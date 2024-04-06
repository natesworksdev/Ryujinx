using LibHac.Fs;
using LibHac.Ncm;
using Ryujinx.Common.Configuration;
using System;
using static Ryujinx.HLE.FileSystem.VirtualFileSystem;
using Path = System.IO.Path;

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
            switch (switchContentPath)
            {
                case SystemContent:
                    realPath = Path.Combine(AppDataManager.BaseDirPath, SystemNandPath, Contents);
                    return true;
                case UserContent:
                    realPath = Path.Combine(AppDataManager.BaseDirPath, UserNandPath, Contents);
                    return true;
                case SdCardContent:
                    realPath = Path.Combine(GetSdCardPath(), Nintendo, Contents);
                    return true;
                case System:
                    realPath = Path.Combine(AppDataManager.BaseDirPath, SystemNandPath);
                    return true;
                case User:
                    realPath = Path.Combine(AppDataManager.BaseDirPath, UserNandPath);
                    return true;
                default:
                    realPath = null;
                    return false;
            }
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
            switch (storageId)
            {
                case StorageId.BuiltInSystem:
                    contentPath = SystemContent;
                    return true;
                case StorageId.BuiltInUser:
                    contentPath = UserContent;
                    return true;
                case StorageId.SdCard:
                    contentPath = SdCardContent;
                    return true;
                default:
                    contentPath = null;
                    return false;
            }
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
