using LibHac;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Font;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.HLE.Loaders.Executables;
using Ryujinx.HLE.Loaders.Npdm;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ryujinx.HLE.HOS
{
    public class Horizon : IDisposable
    {
        internal const int HidSize  = 0x40000;
        internal const int FontSize = 0x1100000;

        private Switch _device;

        private ConcurrentDictionary<int, Process> _processes;

        public SystemStateMgr State { get; private set; }

        internal KRecursiveLock CriticalSectionLock { get; private set; }

        internal KScheduler Scheduler { get; private set; }

        internal KTimeManager TimeManager { get; private set; }

        internal KAddressArbiter AddressArbiter { get; private set; }

        internal KSynchronization Synchronization { get; private set; }

        internal LinkedList<KThread> Withholders { get; private set; }

        internal KSharedMemory HidSharedMem  { get; private set; }
        internal KSharedMemory FontSharedMem { get; private set; }

        internal SharedFontManager Font { get; private set; }

        internal KEvent VsyncEvent { get; private set; }

        internal Keyset KeySet { get; private set; }

        private bool _hasStarted;

        public Nacp ControlData { get; set; }

        public string CurrentTitle { get; private set; }

        public bool EnableFsIntegrityChecks { get; set; }

        public Horizon(Switch device)
        {
            _device = device;

            _processes = new ConcurrentDictionary<int, Process>();

            State = new SystemStateMgr();

            CriticalSectionLock = new KRecursiveLock(this);

            Scheduler = new KScheduler(this);

            TimeManager = new KTimeManager();

            AddressArbiter = new KAddressArbiter(this);

            Synchronization = new KSynchronization(this);

            Withholders = new LinkedList<KThread>();

            Scheduler.StartAutoPreemptionThread();

            if (!device.Memory.Allocator.TryAllocate(HidSize,  out long hidPa) ||
                !device.Memory.Allocator.TryAllocate(FontSize, out long fontPa))
            {
                throw new InvalidOperationException();
            }

            HidSharedMem  = new KSharedMemory(hidPa, HidSize);
            FontSharedMem = new KSharedMemory(fontPa, FontSize);

            Font = new SharedFontManager(device, FontSharedMem.Pa);

            VsyncEvent = new KEvent(this);

            LoadKeySet();
        }

        public void LoadCart(string exeFsDir, string romFsFile = null)
        {
            if (romFsFile != null)
            {
                _device.FileSystem.LoadRomFs(romFsFile);
            }

            string npdmFileName = Path.Combine(exeFsDir, "main.npdm");

            Npdm metaData = null;

            if (File.Exists(npdmFileName))
            {
                Logger.PrintInfo(LogClass.Loader, $"Loading main.npdm...");

                using (FileStream input = new FileStream(npdmFileName, FileMode.Open))
                {
                    metaData = new Npdm(input);
                }
            }
            else
            {
                Logger.PrintWarning(LogClass.Loader, $"NPDM file not found, using default values!");
            }

            Process mainProcess = MakeProcess(metaData);

            void LoadNso(string fileName)
            {
                foreach (string file in Directory.GetFiles(exeFsDir, fileName))
                {
                    if (Path.GetExtension(file) != string.Empty)
                    {
                        continue;
                    }

                    Logger.PrintInfo(LogClass.Loader, $"Loading {Path.GetFileNameWithoutExtension(file)}...");

                    using (FileStream input = new FileStream(file, FileMode.Open))
                    {
                        string name = Path.GetFileNameWithoutExtension(file);

                        Nso program = new Nso(input, name);

                        mainProcess.LoadProgram(program);
                    }
                }
            }

            if (!(mainProcess.MetaData?.Is64Bits ?? true))
            {
                throw new NotImplementedException("32-bit titles are unsupported!");
            }

            CurrentTitle = mainProcess.MetaData.Aci0.TitleId.ToString("x16");

            LoadNso("rtld");

            mainProcess.SetEmptyArgs();

            LoadNso("main");
            LoadNso("subsdk*");
            LoadNso("sdk");

            mainProcess.Run();
        }

        public void LoadXci(string xciFile)
        {
            FileStream file = new FileStream(xciFile, FileMode.Open, FileAccess.Read);

            Xci xci = new Xci(KeySet, file);

            (Nca mainNca, Nca controlNca) = GetXciGameData(xci);

            if (mainNca == null)
            {
                Logger.PrintError(LogClass.Loader, "Unable to load XCI");

                return;
            }

            LoadNca(mainNca, controlNca);
        }

        private (Nca Main, Nca Control) GetXciGameData(Xci xci)
        {
            if (xci.SecurePartition == null)
            {
                throw new InvalidDataException("Could not find XCI secure partition");
            }

            Nca mainNca    = null;
            Nca patchNca   = null;
            Nca controlNca = null;

            foreach (PfsFileEntry fileEntry in xci.SecurePartition.Files.Where(x => x.Name.EndsWith(".nca")))
            {
                Stream ncaStream = xci.SecurePartition.OpenFile(fileEntry);

                Nca nca = new Nca(KeySet, ncaStream, true);

                if (nca.Header.ContentType == ContentType.Program)
                {
                    if (nca.Sections.Any(x => x?.Type == SectionType.Romfs))
                    {
                        mainNca = nca;
                    }
                    else if (nca.Sections.Any(x => x?.Type == SectionType.Bktr))
                    {
                        patchNca = nca;
                    }
                }
                else if (nca.Header.ContentType == ContentType.Control)
                {
                    controlNca = nca;
                }
            }

            if (mainNca == null)
            {
                Logger.PrintError(LogClass.Loader, "Could not find an Application NCA in the provided XCI file");
            }

            mainNca.SetBaseNca(patchNca);

            if (controlNca != null)
            {
                ReadControlData(controlNca);
            }

            if (patchNca != null)
            {
                patchNca.SetBaseNca(mainNca);

                return (patchNca, controlNca);
            }

            return (mainNca, controlNca);
        }

        public void ReadControlData(Nca controlNca)
        {
            Romfs controlRomfs = new Romfs(controlNca.OpenSection(0, false, EnableFsIntegrityChecks));

            byte[] controlFile = controlRomfs.GetFile("/control.nacp");

            BinaryReader reader = new BinaryReader(new MemoryStream(controlFile));

            ControlData = new Nacp(reader);
        }

        public void LoadNca(string ncaFile)
        {
            FileStream file = new FileStream(ncaFile, FileMode.Open, FileAccess.Read);

            Nca nca = new Nca(KeySet, file, true);

            LoadNca(nca, null);
        }

        public void LoadNsp(string nspFile)
        {
            FileStream file = new FileStream(nspFile, FileMode.Open, FileAccess.Read);

            Pfs nsp = new Pfs(file);

            PfsFileEntry ticketFile = nsp.Files.FirstOrDefault(x => x.Name.EndsWith(".tik"));

            // Load title key from the NSP's ticket in case the user doesn't have a title key file
            if (ticketFile != null)
            {
                Ticket ticket = new Ticket(nsp.OpenFile(ticketFile));

                KeySet.TitleKeys[ticket.RightsId] = ticket.GetTitleKey(KeySet);
            }

            Nca mainNca    = null;
            Nca controlNca = null;

            foreach (PfsFileEntry ncaFile in nsp.Files.Where(x => x.Name.EndsWith(".nca")))
            {
                Nca nca = new Nca(KeySet, nsp.OpenFile(ncaFile), true);

                if (nca.Header.ContentType == ContentType.Program)
                {
                    mainNca = nca;
                }
                else if (nca.Header.ContentType == ContentType.Control)
                {
                    controlNca = nca;
                }
            }

            if (mainNca != null)
            {
                LoadNca(mainNca, controlNca);

                return;
            }

            Logger.PrintError(LogClass.Loader, "Could not find an Application NCA in the provided NSP file");
        }

        public void LoadNca(Nca mainNca, Nca controlNca)
        {
            NcaSection romfsSection = mainNca.Sections.FirstOrDefault(x => x?.Type == SectionType.Romfs || x?.Type == SectionType.Bktr);
            NcaSection exefsSection = mainNca.Sections.FirstOrDefault(x => x?.IsExefs == true);

            if (exefsSection == null)
            {
                Logger.PrintError(LogClass.Loader, "No ExeFS found in NCA");

                return;
            }

            if (romfsSection == null)
            {
                Logger.PrintWarning(LogClass.Loader, "No RomFS found in NCA");
            }
            else
            {
                Stream romfsStream = mainNca.OpenSection(romfsSection.SectionNum, false, EnableFsIntegrityChecks);

                _device.FileSystem.SetRomFs(romfsStream);
            }

            Stream exefsStream = mainNca.OpenSection(exefsSection.SectionNum, false, EnableFsIntegrityChecks);

            Pfs exefs = new Pfs(exefsStream);

            Npdm metaData = null;

            if (exefs.FileExists("main.npdm"))
            {
                Logger.PrintInfo(LogClass.Loader, "Loading main.npdm...");

                metaData = new Npdm(exefs.OpenFile("main.npdm"));
            }
            else
            {
                Logger.PrintWarning(LogClass.Loader, $"NPDM file not found, using default values!");
            }

            Process mainProcess = MakeProcess(metaData);

            void LoadNso(string filename)
            {
                foreach (PfsFileEntry file in exefs.Files.Where(x => x.Name.StartsWith(filename)))
                {
                    if (Path.GetExtension(file.Name) != string.Empty)
                    {
                        continue;
                    }

                    Logger.PrintInfo(LogClass.Loader, $"Loading {filename}...");

                    string name = Path.GetFileNameWithoutExtension(file.Name);

                    Nso program = new Nso(exefs.OpenFile(file), name);

                    mainProcess.LoadProgram(program);
                }
            }

            Nacp ReadControlData()
            {
                Romfs controlRomfs = new Romfs(controlNca.OpenSection(0, false, EnableFsIntegrityChecks));

                byte[] controlFile = controlRomfs.GetFile("/control.nacp");

                BinaryReader reader = new BinaryReader(new MemoryStream(controlFile));

                Nacp controlData = new Nacp(reader);

                CurrentTitle = controlData.Languages[(int)State.DesiredTitleLanguage].Title;

                if (string.IsNullOrWhiteSpace(CurrentTitle))
                {
                    CurrentTitle = controlData.Languages.ToList().Find(x => !string.IsNullOrWhiteSpace(x.Title)).Title;
                }

                return controlData;
            }

            if (controlNca != null)
            {
                mainProcess.ControlData = ReadControlData();
            }
            else
            {
                CurrentTitle = mainProcess.MetaData.Aci0.TitleId.ToString("x16");
            }

            if (!mainProcess.MetaData.Is64Bits)
            {
                throw new NotImplementedException("32-bit titles are unsupported!");
            }

            LoadNso("rtld");

            mainProcess.SetEmptyArgs();

            LoadNso("main");
            LoadNso("subsdk");
            LoadNso("sdk");

            mainProcess.Run();
        }

        public void LoadProgram(string filePath)
        {
            bool isNro = Path.GetExtension(filePath).ToLower() == ".nro";

            string name = Path.GetFileNameWithoutExtension(filePath);
            string switchFilePath = _device.FileSystem.SystemPathToSwitchPath(filePath);

            if (isNro && (switchFilePath == null || !switchFilePath.StartsWith("sdmc:/")))
            {
                string switchPath = $"sdmc:/switch/{name}{Homebrew.TemporaryNroSuffix}";
                string tempPath = _device.FileSystem.SwitchPathToSystemPath(switchPath);

                string switchDir = Path.GetDirectoryName(tempPath);

                if (!Directory.Exists(switchDir))
                {
                    Directory.CreateDirectory(switchDir);
                }

                File.Copy(filePath, tempPath, true);

                filePath = tempPath;
            }

            Process mainProcess = MakeProcess();

            using (FileStream input = new FileStream(filePath, FileMode.Open))
            {
                mainProcess.LoadProgram(isNro
                    ? (IExecutable)new Nro(input, filePath)
                    : (IExecutable)new Nso(input, filePath));
            }

            mainProcess.SetEmptyArgs();
            mainProcess.Run(isNro);
        }

        public void LoadKeySet()
        {
            string keyFile        = null;
            string titleKeyFile   = null;
            string consoleKeyFile = null;

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            LoadSetAtPath(Path.Combine(home, ".switch"));
            LoadSetAtPath(_device.FileSystem.GetSystemPath());

            KeySet = ExternalKeys.ReadKeyFile(keyFile, titleKeyFile, consoleKeyFile);

            void LoadSetAtPath(string basePath)
            {
                string localKeyFile        = Path.Combine(basePath,    "prod.keys");
                string localTitleKeyFile   = Path.Combine(basePath,   "title.keys");
                string localConsoleKeyFile = Path.Combine(basePath, "console.keys");

                if (File.Exists(localKeyFile))
                {
                    keyFile = localKeyFile;
                }

                if (File.Exists(localTitleKeyFile))
                {
                    titleKeyFile = localTitleKeyFile;
                }

                if (File.Exists(localConsoleKeyFile))
                {
                    consoleKeyFile = localConsoleKeyFile;
                }
            }
        }

        public void SignalVsync()
        {
            VsyncEvent.ReadableEvent.Signal();
        }

        private Process MakeProcess(Npdm metaData = null)
        {
            _hasStarted = true;

            Process process;

            lock (_processes)
            {
                int processId = 0;

                while (_processes.ContainsKey(processId))
                {
                    processId++;
                }

                process = new Process(_device, processId, metaData);

                _processes.TryAdd(processId, process);
            }

            InitializeProcess(process);

            return process;
        }

        private void InitializeProcess(Process process)
        {
            process.AppletState.SetFocus(true);
        }

        internal void ExitProcess(int processId)
        {
            if (_processes.TryRemove(processId, out Process process))
            {
                process.Dispose();

                if (_processes.Count == 0)
                {
                    Scheduler.Dispose();

                    TimeManager.Dispose();

                    _device.Unload();
                }
            }
        }

        public void EnableMultiCoreScheduling()
        {
            if (!_hasStarted)
            {
                Scheduler.MultiCoreScheduling = true;
            }
        }

        public void DisableMultiCoreScheduling()
        {
            if (!_hasStarted)
            {
                Scheduler.MultiCoreScheduling = false;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (Process process in _processes.Values)
                {
                    process.Dispose();
                }
            }
        }
    }
}
