using System;
using System.IO;
using System.Linq;

namespace Ryujinx.Profiler
{
    public static class DumpProfile
    {
        public static void ToFile(string path, InternalProfile profile)
        {
            String fileData = "Name,Session Group,Session Item,Count,Average(ms),Total(ms)\r\n";

            foreach (var time in profile.Timers.OrderBy(key => key.Key.Tag))
            {
                fileData += $"{time.Key.Name}," +
                            $"{time.Key.SessionGroup}," +
                            $"{time.Key.SessionItem}," +
                            $"{time.Value.Count}," +
                            $"{profile.ConvertTicksToMS(time.Value.AverageTime)}," +
                            $"{profile.ConvertTicksToMS(time.Value.TotalTime)}\r\n";
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
