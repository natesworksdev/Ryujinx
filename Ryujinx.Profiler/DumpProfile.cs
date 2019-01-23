using System;
using System.IO;

namespace Ryujinx.Profiler
{
    public static class DumpProfile
    {
        public static void ToFile(string path, InternalProfile profile)
        {
            String fileData = "";
            foreach (var time in profile.Timers)
            {
                fileData += $"{time.Key.Tag} - " +
                            $"Total: {profile.ConvertTicksToMS(time.Value.TotalTime)}ms, " +
                            $"Average: {profile.ConvertTicksToMS(time.Value.AverageTime)}ms, " +
                            $"Count: {time.Value.Count}\r\n";
            }

            // Ensure file directory exists before write
            FileInfo fileInfo = new FileInfo(path);
            if (fileInfo == null)
                throw new Exception("Unknown logging error, probably a bad file path");
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
                Directory.CreateDirectory(fileInfo.Directory.FullName);

            File.WriteAllText(fileInfo.FullName, fileData);
        }
    }
}
