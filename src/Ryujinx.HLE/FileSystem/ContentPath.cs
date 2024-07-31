using LibHac.Fs;
using LibHac.Ncm;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Utilities;
using System;
using static Ryujinx.HLE.FileSystem.VirtualFileSystem;

namespace Ryujinx.HLE.FileSystem
{
    internal static class ContentPath
    {
        public const string SystemContent = "@SystemContent";
        public const string UserContent = "@UserContent";
        public const string SdCardContent = "@SdCardContent";
        public const string SdCard = "@Sdcard";
        public const string CalibFile = "@CalibFile";
        public const string Safe = "@Safe";
        public const string User = "@User";
        public const string System = "@System";
        public const string Host = "@Host";
        public const string GamecardApp = "@GcApp";
        public const string GamecardContents = "@GcS00000001";
        public const string GamecardUpdate = "@upp";
        public const string RegisteredUpdate = "@RegUpdate";

        public const string Nintendo = "Nintendo";
        public const string Contents = "Contents";

        public static bool TryGetRealPath(string switchContentPath, out string realPath)
        {
            realPath = switchContentPath switch
            {
                SystemContent => FileSystemUtils.CombineAndResolveFullPath(true, AppDataManager.BaseDirPath, SystemNandPath, Contents),
                UserContent => FileSystemUtils.CombineAndResolveFullPath(true, AppDataManager.BaseDirPath, UserNandPath, Contents),
                SdCardContent => FileSystemUtils.CombineAndResolveFullPath(true, GetSdCardPath(), Nintendo, Contents),
                System => FileSystemUtils.CombineAndResolveFullPath(true, AppDataManager.BaseDirPath, SystemNandPath),
                User => FileSystemUtils.CombineAndResolveFullPath(true, AppDataManager.BaseDirPath, UserNandPath),
                _ => null,
            };

            return realPath != null;
        }

        public static string GetContentPath(ContentStorageId contentStorageId)
        {
            return contentStorageId switch
            {
                ContentStorageId.System => SystemContent,
                ContentStorageId.User => UserContent,
                ContentStorageId.SdCard => SdCardContent,
                _ => throw new NotSupportedException($"Content Storage Id \"`{contentStorageId}`\" is not supported."),
            };
        }

        public static bool TryGetContentPath(StorageId storageId, out string contentPath)
        {
            contentPath = storageId switch
            {
                StorageId.BuiltInSystem => SystemContent,
                StorageId.BuiltInUser => UserContent,
                StorageId.SdCard => SdCardContent,
                _ => null,
            };

            return contentPath != null;
        }

        public static StorageId GetStorageId(string contentPathString)
        {
            return contentPathString.Split(':')[0] switch
            {
                SystemContent or
                System => StorageId.BuiltInSystem,
                UserContent or
                User => StorageId.BuiltInUser,
                SdCardContent => StorageId.SdCard,
                Host => StorageId.Host,
                GamecardApp or
                GamecardContents or
                GamecardUpdate => StorageId.GameCard,
                _ => StorageId.None,
            };
        }
    }
}
