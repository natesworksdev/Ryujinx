using LibHac.Loader;
using LibHac.Ns;
using Ryujinx.HLE.Loaders.Processes.Extensions;

namespace Ryujinx.HLE.Loaders.Processes
{
    public struct ProcessInfo
    {
        public readonly MetaLoader                 MetaLoader;
        public readonly ApplicationControlProperty ApplicationControlProperties;

        public string Name;
        public ulong  ProgramId;

        public readonly string ProgramIdText;
        public readonly bool   Is64Bit;

        public readonly bool DiskCacheEnabled;
        public readonly bool AllowCodeMemoryForJit;


        public ProcessInfo(MetaLoader metaLoader, ApplicationControlProperty applicationControlProperties, bool diskCacheEnabled, bool allowCodeMemoryForJit)
        {
            MetaLoader                   = metaLoader;
            ApplicationControlProperties = applicationControlProperties;

            ulong programId = metaLoader.GetProgramId();

            Name          = MetaLoader.GetProgramName();
            ProgramId     = programId;
            ProgramIdText = $"{programId:x16}";
            Is64Bit       = metaLoader.IsProgram64Bit();

            DiskCacheEnabled      = diskCacheEnabled;
            AllowCodeMemoryForJit = allowCodeMemoryForJit;
        }
    }
}
