using LibHac.Fs;
using LibHac.Ncm;

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
        public string ImportPath { get; init; }
    }
}