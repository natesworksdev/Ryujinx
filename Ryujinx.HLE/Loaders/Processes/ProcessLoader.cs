using LibHac.Common;
using LibHac.Fs;
using LibHac.Fs.Fsa;
using LibHac.FsSystem;
using LibHac.Ns;
using LibHac.Tools.Fs;
using LibHac.Tools.FsSystem;
using LibHac.Tools.FsSystem.NcaUtils;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.Loaders.Executables;
using Ryujinx.HLE.Loaders.Processes.Extensions;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace Ryujinx.HLE.Loaders.Processes
{
    public class ProcessLoader
    {
        private readonly Switch _device;

        private readonly ConcurrentDictionary<ulong, ProcessResult> _processesByPid;

        private ulong _latestPid;

        public ProcessResult ActiveProcess => _processesByPid[_latestPid];

        public ProcessLoader(Switch device)
        {
            _device         = device;
            _processesByPid = new ConcurrentDictionary<ulong, ProcessResult>();
        }

        public void LoadXci(string path)
        {
            FileStream stream = new(path, FileMode.Open, FileAccess.Read);
            Xci        xci    = new(_device.Configuration.VirtualFileSystem.KeySet, stream.AsStorage());

            if (!xci.HasPartition(XciPartitionType.Secure))
            {
                Logger.Error?.Print(LogClass.Loader, "Unable to load XCI: Could not find XCI Secure partition");

                return;
            }

            ProcessResult processResult = xci.OpenPartition(XciPartitionType.Secure).Load(_device, path);

            if (_processesByPid.TryAdd(processResult.ProcessId, processResult))
            {
                if (processResult.Start(_device))
                {
                    _latestPid = processResult.ProcessId;
                }
            }
        }

        public void LoadNsp(string path)
        {
            FileStream          file                = new(path, FileMode.Open, FileAccess.Read);
            PartitionFileSystem partitionFileSystem = new(file.AsStorage());
            ProcessResult       processResult       = partitionFileSystem.Load(_device, path);

            if (processResult.ProcessId != 0 && _processesByPid.TryAdd(processResult.ProcessId, processResult))
            {
                if (processResult.Start(_device))
                {
                    _latestPid = processResult.ProcessId;
                }

                return;
            }

            // This is not a normal NSP, it's actually a ExeFS as a NSP
            partitionFileSystem.Load(_device, new BlitStruct<ApplicationControlProperty>(1), partitionFileSystem.GetNpdm(), true);
        }

        public void LoadNca(string path)
        {
            FileStream file = new(path, FileMode.Open, FileAccess.Read);
            Nca        nca  = new(_device.Configuration.VirtualFileSystem.KeySet, file.AsStorage(false));

            ProcessResult processResult = nca.Load(_device, null, null);

            if (_processesByPid.TryAdd(processResult.ProcessId, processResult))
            {
                if (processResult.Start(_device))
                {
                    // NOTE: Check if process is SystemApplicationId or ApplicationId
                    if (processResult.ProgramId > 0x0100000000000FFF)
                    {
                        _latestPid = processResult.ProcessId;
                    }
                }
            }
        }

        public void LoadUnpackedNca(string exeFsDirPath, string romFsPath = null)
        {
            ProcessResult processResult = new LocalFileSystem(exeFsDirPath).Load(_device, romFsPath);

            if (_processesByPid.TryAdd(processResult.ProcessId, processResult))
            {
                if (processResult.Start(_device))
                {
                    _latestPid = processResult.ProcessId;
                }
            }
        }

        public void LoadNxo(string path)
        {
            var         nacpData    = new BlitStruct<ApplicationControlProperty>(1);
            IFileSystem dummyExeFs  = null;
            Stream      romfsStream = null;

            string programName = "";
            ulong  programId   = 0000000000000000;

            // Load executable.
            IExecutable executable;

            if (System.IO.Path.GetExtension(path).ToLower() == ".nro")
            {
                FileStream    input = new(path, FileMode.Open);
                NroExecutable nro   = new(input.AsStorage());

                executable = nro;

                // Open RomFS if exists.
                IStorage romFsStorage = nro.OpenNroAssetSection(LibHac.Tools.Ro.NroAssetType.RomFs, false);
                romFsStorage.GetSize(out long romFsSize).ThrowIfFailure();
                if (romFsSize != 0)
                {
                    romfsStream = romFsStorage.AsStream();
                }

                // Load Nacp if exists.
                IStorage nacpStorage = nro.OpenNroAssetSection(LibHac.Tools.Ro.NroAssetType.Nacp, false);
                nacpStorage.GetSize(out long nacpSize).ThrowIfFailure();
                if (nacpSize != 0)
                {
                    nacpStorage.Read(0, nacpData.ByteSpan);

                    programName = nacpData.Value.Title[(int)_device.System.State.DesiredTitleLanguage].NameString.ToString();

                    if (string.IsNullOrWhiteSpace(programName))
                    {
                        programName = nacpData.Value.Title.ItemsRo.ToArray().FirstOrDefault(x => x.Name[0] != 0).NameString.ToString();
                    }

                    if (nacpData.Value.PresenceGroupId != 0)
                    {
                        programId = nacpData.Value.PresenceGroupId;
                    }
                    else if (nacpData.Value.SaveDataOwnerId != 0)
                    {
                        programId = nacpData.Value.SaveDataOwnerId;
                    }
                    else if (nacpData.Value.AddOnContentBaseId != 0)
                    {
                        programId = nacpData.Value.AddOnContentBaseId - 0x1000;
                    }
                }

                // TODO: Add icon maybe ?
            }
            else
            {
                programName = System.IO.Path.GetFileNameWithoutExtension(path);

                executable = new NsoExecutable(new LocalStorage(path, FileAccess.Read), programName);
            }

            // Explicitly null TitleId to disable the shader cache.
            Graphics.Gpu.GraphicsConfig.TitleId = null;
            _device.Gpu.HostInitalized.Set();

            ProcessResult processResult = ProcessLoaderHelper.LoadNsos(_device, 
                                                                       _device.System.KernelContext,
                                                                       dummyExeFs.GetNpdm(),
                                                                       nacpData.Value,
                                                                       diskCacheEnabled: false,
                                                                       allowCodeMemoryForJit: true,
                                                                       programName,
                                                                       programId,
                                                                       null,
                                                                       executable);

            // Load RomFS.
            if (romfsStream != null)
            {
                _device.Configuration.VirtualFileSystem.SetRomFs(processResult.ProcessId, romfsStream);
            }

            // Start process.
            if (_processesByPid.TryAdd(processResult.ProcessId, processResult))
            {
                if (processResult.Start(_device))
                {
                    _latestPid = processResult.ProcessId;
                }
            }
        }
    }
}