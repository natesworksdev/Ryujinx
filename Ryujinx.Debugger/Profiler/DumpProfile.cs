using Ryujinx.Common;
using System.IO;
using System.Linq;
using System.Text;

namespace Ryujinx.Debugger.Profiler
{
    public static class DumpProfile
    {
        private const string InitialFileData = "Category,Session Group,Session Item,Count,Average(ms),Total(ms)\r\n";

        public static void ToFile(string path, InternalProfile profile)
        {
            // Ensure file directory exists before write
            var filePath = Path.GetFullPath(path);
            var directoryPath = Path.GetFullPath(Path.GetDirectoryName(path));

            Directory.CreateDirectory(directoryPath);

            var fileData = new StringBuilder(InitialFileData);

            foreach (var time in profile.Timers.OrderBy(key => key.Key.Tag))
            {
                fileData.Append(
                    $"{time.Key.Category}," +
                    $"{time.Key.SessionGroup}," +
                    $"{time.Key.SessionItem}," +
                    $"{time.Value.Count}," +
                    $"{time.Value.AverageTime / PerformanceCounter.TicksPerMillisecond}," +
                    $"{time.Value.TotalTime / PerformanceCounter.TicksPerMillisecond}\r\n"
                );
            }

            File.WriteAllText(filePath, fileData.ToString());
        }
    }
}
