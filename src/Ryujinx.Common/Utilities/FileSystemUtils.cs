using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Ryujinx.Common.Utilities
{
    public static class FileSystemUtils
    {
        public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            // Get information about the source directory
            var dir = new DirectoryInfo(sourceDir);

            // Check if the source directory exists
            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
            }

            // Cache directories before we start copying
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Create the destination directory
            Directory.CreateDirectory(destinationDir);

            // Get the files in the source directory and copy to the destination directory
            foreach (FileInfo file in dir.GetFiles())
            {
                string targetFilePath = Path.Combine(destinationDir, file.Name);
                file.CopyTo(targetFilePath);
            }

            // If recursive and copying subdirectories, recursively call this method
            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        public static void MoveDirectory(string sourceDir, string destinationDir)
        {
            CopyDirectory(sourceDir, destinationDir, true);
            Directory.Delete(sourceDir, true);
        }

        public static string SanitizeFileName(string fileName)
        {
            var reservedChars = new HashSet<char>(Path.GetInvalidFileNameChars());
            return string.Concat(fileName.Select(c => reservedChars.Contains(c) ? '_' : c));
        }

        public static string ResolveFullPath(string path, bool isDirectory)
        {
            FileSystemInfo pathInfo = isDirectory ? new DirectoryInfo(path) : new FileInfo(path);

            if (pathInfo.Exists)
            {
                return pathInfo.ResolveLinkTarget(true)?.FullName ?? pathInfo.FullName;
            }

            return pathInfo.FullName;
        }

        public static FileInfo GetActualFileInfo(this FileInfo fileInfo)
        {
            if (fileInfo.Exists)
            {
                return (FileInfo)(fileInfo.ResolveLinkTarget(true) ?? fileInfo);
            }

            return fileInfo;
        }

        public static FileInfo GetActualFileInfo(string filePath)
        {
            FileInfo fileInfo = new(filePath);

            return fileInfo.GetActualFileInfo();
        }

        public static DirectoryInfo GetActualDirectoryInfo(this DirectoryInfo directoryInfo)
        {
            if (directoryInfo.Exists)
            {
                return (DirectoryInfo)(directoryInfo.ResolveLinkTarget(true) ?? directoryInfo);
            }

            return directoryInfo;
        }

        public static DirectoryInfo GetActualDirectoryInfo(string directoryPath)
        {
            DirectoryInfo directoryInfo = new(directoryPath);

            return directoryInfo.GetActualDirectoryInfo();
        }
    }
}
