using System;
using System.Collections.Generic;

namespace Ryujinx.Ava.Common.SaveManager
{
    internal readonly record struct UserFriendlyAppData
    {
        public ulong TitleId { get; init; }
        public string Title { get; init; }
        public string TitleIdHex { get; init; }
    }

    internal readonly record struct UserFriendlySaveMetadata
    {
        public string UserId { get; init; }
        public string ProfileName { get; init; }
        public DateTime CreationTimeUtc { get; init; }
        public IEnumerable<UserFriendlyAppData> ApplicationMap { get; init; }
    }
}
