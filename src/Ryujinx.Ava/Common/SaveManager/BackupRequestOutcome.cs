using System;

namespace Ryujinx.Ava.Common.SaveManager
{
    public readonly record struct BackupRequestOutcome
    {
        public bool DidFail { get; init; }
        public string Message { get; init; }
    }
}
