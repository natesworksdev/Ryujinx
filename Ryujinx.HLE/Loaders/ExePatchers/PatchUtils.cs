using System;
using System.IO;
using System.Collections.Generic;

using Ryujinx.Common.Logging;

namespace Ryujinx.HLE.Loaders.ExePatchers
{
    class PatchUtils
    {
        public static MemPatch[] CollectNsoPatches(string dirPath, List<string> buildIds)
        {
            if (!Directory.Exists(dirPath))
            {
                throw new ArgumentException("dirPath must be a valid directory");
            }

            var patches = new MemPatch[buildIds.Count];
            for (int i = 0; i < patches.Length; ++i)
            {
                patches[i] = new MemPatch();
            }

            int GetIndex(string buildId) => buildIds.FindIndex(id => id == buildId); // O(n) but list is small

            foreach (var file in Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories))
            {
                switch (Path.GetExtension(file))
                {
                    case ".ips":
                        {
                            string filename = Path.GetFileNameWithoutExtension(file).Split('.')[0];
                            string buildId = filename.TrimEnd('0');

                            int index = GetIndex(buildId);
                            if (index == -1)
                            {
                                continue;
                            }

                            Logger.PrintInfo(LogClass.Loader, $"Found matching IPS patch for bid={buildId} - {file}");

                            using var fs = File.OpenRead(file);
                            using var reader = new BinaryReader(fs);

                            var patcher = new IpsPatcher(reader);
                            patcher.AddPatches(patches[index]);

                        }
                        break;

                    case ".pchtxt":
                        using (var fs = File.OpenRead(file))
                        using (var reader = new StreamReader(fs))
                        {
                            var patcher = new IPSwitchPatcher(reader);

                            int index = GetIndex(patcher.BuildId);
                            if (index == -1)
                            {
                                continue;
                            }

                            Logger.PrintInfo(LogClass.Loader, $"Found matching IPSwitch patch for bid={patcher.BuildId} - {file}");

                            patcher.AddPatches(patches[index]);
                        }
                        break;
                }
            }

            return patches;
        }
    }
}