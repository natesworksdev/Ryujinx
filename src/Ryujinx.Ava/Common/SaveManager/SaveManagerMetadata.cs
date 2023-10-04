using LibHac.Fs;
using LibHac.Ncm;
using System.IO.Compression;

namespace Ryujinx.Ava.Common.SaveManager
{
    internal readonly record struct BackupSaveMeta
    {
        public ulong SaveDataId { get; init; }
        public SaveDataType Type { get; init; }
        public ProgramId TitleId { get; init; }
    }

    internal readonly record struct RestoreSaveMeta
    {
        public ulong TitleId { get; init; }
        public SaveDataType SaveType { get; init; }
    }

    internal readonly record struct MetaToLocalMap
    {
        public string RelativeDir { get; init; }
        public string LocalDir { get; init; }
    }
}
