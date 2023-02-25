using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.Horizon.Common;

namespace Ryujinx.HLE.Loaders.Processes
{
    public struct ProcessResult
    {
        public static ProcessResult Failed => new(new ProcessInfo(), null, 0, 0, 0);

        private readonly byte _mainThreadPriority;
        private readonly uint _mainThreadStackSize;

        public readonly ProcessInfo         Informations;
        public readonly IDiskCacheLoadState DiskCacheLoadState;
        public readonly ulong               ProcessId;

        public ProcessResult(ProcessInfo processInfo, IDiskCacheLoadState diskCacheLoadState, ulong pid, byte mainThreadPriority, uint mainThreadStackSize)
        {
            _mainThreadPriority  = mainThreadPriority;
            _mainThreadStackSize = mainThreadStackSize;

            Informations       = processInfo;
            DiskCacheLoadState = diskCacheLoadState;
            ProcessId          = pid;
        }

        public bool Start(Switch device)
        {
            device.Configuration.ContentManager.LoadEntries(device);

            Result result = device.System.KernelContext.Processes[ProcessId].Start(_mainThreadPriority, _mainThreadStackSize);
            if (result != Result.Success)
            {
                Logger.Error?.Print(LogClass.Loader, $"Process start returned error \"{result}\".");

                return false;
            }

            // TODO: LibHac npdm currently doesn't support version field.
            string version;

            if (Informations.ProgramId > 0x010000000000FFFF)
            {
                version = Informations.ApplicationControlProperties.DisplayVersionString.ToString();
            }
            else
            {
                version = device.System.ContentManager.GetCurrentFirmwareVersion().VersionString;
            }

            Logger.Info?.Print(LogClass.Loader, $"Application Loaded: {Informations.Name} v{version} [{Informations.ProgramIdText}] [{(Informations.Is64Bit ? "64-bit" : "32-bit")}]");

            return true;
        }
    }
}