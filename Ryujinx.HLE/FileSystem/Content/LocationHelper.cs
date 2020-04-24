using System;
using System.IO;

using static Ryujinx.HLE.FileSystem.VirtualFileSystem;

namespace Ryujinx.HLE.FileSystem.Content
{
    static class LocationHelper
    {
        public static string GetRealPath(VirtualFileSystem fileSystem, string switchContentPath)
        {
            string basePath = fileSystem.GetBasePath();

            return switchContentPath switch
            {
                ContentPath.SystemContent => Path.Combine(basePath, SystemNandPath, "Contents"),
                ContentPath.UserContent   => Path.Combine(basePath, UserNandPath, "Contents"),
                ContentPath.SdCardContent => Path.Combine(fileSystem.GetSdCardPath(), "Nintendo", "Contents"),
                ContentPath.System        => Path.Combine(basePath, SystemNandPath),
                ContentPath.User          => Path.Combine(basePath, UserNandPath),
                _ => throw new NotSupportedException($"Content Path `{switchContentPath}` is not supported."),
            };
        }

        public static string GetContentPath(ContentStorageId contentStorageId)
        {
            return contentStorageId switch
            {
                ContentStorageId.NandSystem => ContentPath.SystemContent,
                ContentStorageId.NandUser   => ContentPath.UserContent,
                ContentStorageId.SdCard     => ContentPath.SdCardContent,
                _ => throw new NotSupportedException($"Content Storage `{contentStorageId}` is not supported."),
            };
        }

        public static string GetContentRoot(StorageId storageId)
        {
            return storageId switch
            {
                StorageId.NandSystem => ContentPath.SystemContent,
                StorageId.NandUser   => ContentPath.UserContent,
                StorageId.SdCard     => ContentPath.SdCardContent,
                _ => throw new NotSupportedException($"Storage Id `{storageId}` is not supported."),
            };
        }

        public static StorageId GetStorageId(string contentPathString)
        {
            string cleanedPath = contentPathString.Split(':')[0];

            switch (cleanedPath)
            {
                case ContentPath.SystemContent:
                case ContentPath.System:
                    return StorageId.NandSystem;

                case ContentPath.UserContent:
                case ContentPath.User:
                    return StorageId.NandUser;

                case ContentPath.SdCardContent:
                    return StorageId.SdCard;

                case ContentPath.Host:
                    return StorageId.Host;

                case ContentPath.GamecardApp:
                case ContentPath.GamecardContents:
                case ContentPath.GamecardUpdate:
                    return StorageId.GameCard;

                default:
                    return StorageId.None;
            }
        }
    }
}
