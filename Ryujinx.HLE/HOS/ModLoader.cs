using LibHac.Common;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.FsSystem.RomFs;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.Loaders.Mods;
using Ryujinx.HLE.Loaders.Executables;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.IO;

namespace Ryujinx.HLE.HOS
{
    public class ModLoader
    {
        private const string RomfsDir = "romfs";
        private const string RomfsStorageFile = "romfs.storage";
        private const string ExefsDir = "exefs";
        private const string StubExtension = ".stub";

        private const string NsoPatchesDir = "exefs_patches";
        private const string NroPatchesDir = "nro_patches";

        public enum ModType
        {
            TitleMod,
            NsoMod,
            NroMod
        }

        public class Mod
        {
            public readonly ulong TitleId;
            public readonly ModType Type;
            public readonly DirectoryInfo Dir;
            public readonly DirectoryInfo Exefs;
            public readonly DirectoryInfo Romfs;
            public readonly FileInfo RomfsFile;

            public bool Enabled;

            public static Mod MakeMod(DirectoryInfo dir, ModType type, ulong titleId = ulong.MaxValue, bool enabled = true)
            {
                if (type == ModType.TitleMod && titleId == ulong.MaxValue)
                {
                    Logger.PrintWarning(LogClass.Application, $"Orphaned {type} without TitleId '{dir.Name}'");
                    return null;
                }

                Mod m = new Mod(dir, type, titleId, enabled);

                bool check = type == ModType.TitleMod ? m.Exefs.Exists || m.Romfs.Exists || m.RomfsFile.Exists : m.Exefs.Exists;

                if (!check)
                {
                    Logger.PrintWarning(LogClass.Application, $"Invalid/Empty {type} '{m.Dir.Name}'");
                    return null;
                }

                string status = (m.Exefs.Exists ? "[E" : "[") +
                                ((m.RomfsFile?.Exists ?? false) ? "r" : "") +
                                ((m.Romfs?.Exists ?? false) ? "R] " : "] ") +
                                (type == ModType.TitleMod ? $"[{titleId:X16}]" : "");

                Logger.PrintInfo(LogClass.Application, $"Found {type} '{m.Dir.Name}' {status}");

                return m;
            }

            private Mod(DirectoryInfo dir, ModType type, ulong titleId, bool enabled = true)
            {
                Dir = dir;
                Type = type;
                Enabled = enabled;

                switch (type)
                {
                    case ModType.NroMod:
                    case ModType.NsoMod:
                        Exefs = Dir; // No exefs needed for this type. Mods are directly under <mod-name>.
                        break;
                    default:
                        TitleId = titleId;
                        Exefs = new DirectoryInfo(Path.Combine(dir.FullName, ExefsDir));
                        Romfs = new DirectoryInfo(Path.Combine(dir.FullName, RomfsDir));
                        RomfsFile = new FileInfo(Path.Combine(dir.FullName, RomfsStorageFile));
                        break;
                }
            }

            // Useful when collect and processing are separated
            public bool Check()
            {
                if (!Enabled)
                {
                    return false;
                }

                Dir.Refresh();

                if (Type == ModType.TitleMod)
                {
                    Exefs.Refresh();
                    Romfs.Refresh();
                    RomfsFile.Refresh();
                }

                return Dir.Exists;
            }
        }

        public readonly Dictionary<ulong, List<Mod>> TitleMods;
        public readonly Dictionary<string, Mod> NsoMods;
        public readonly Dictionary<string, Mod> NroMods;

        public ModLoader()
        {
            TitleMods = new Dictionary<ulong, List<Mod>>();
            NsoMods = new Dictionary<string, Mod>();
            NroMods = new Dictionary<string, Mod>();
        }

        public void Clear()
        {
            TitleMods.Clear();
            NsoMods.Clear();
            NroMods.Clear();
        }

        // Check if searchDir is NsoPatchesDir
        // Check if searchDir is NroPatchesDir
        // Check if searchDir is a TitleId
        // Finally, check if searchDir is a RootDir of above
        public void SearchMods(DirectoryInfo searchDir)
        {
            bool TrySearch(DirectoryInfo dir)
            {
                if (NsoPatchesDir.Equals(dir.Name, StringComparison.OrdinalIgnoreCase))
                {
                    AddPatchMods(dir, ModType.NsoMod);
                }
                else if (NroPatchesDir.Equals(dir.Name, StringComparison.OrdinalIgnoreCase))
                {
                    AddPatchMods(dir, ModType.NroMod);
                }
                else if (dir.Name.Length >= 16 && ulong.TryParse(dir.Name.Substring(0, 16), System.Globalization.NumberStyles.HexNumber, null, out ulong titleId))
                {
                    AddTitleMods(dir, titleId);
                }
                else
                {
                    return false;
                }

                return true;
            }

            if (searchDir.Exists && !TrySearch(searchDir))
            {
                foreach (var dir in searchDir.EnumerateDirectories())
                {
                    TrySearch(dir);
                }
            }
        }

        public void AddTitleMods(DirectoryInfo modsDir, ulong titleId)
        {
            foreach (var modDir in modsDir.EnumerateDirectories())
            {
                var mod = Mod.MakeMod(modDir, ModType.TitleMod, titleId);
                if (mod == null) continue;

                if (TitleMods.TryGetValue(titleId, out var mods))
                {
                    mods.Add(mod);
                }
                else
                {
                    TitleMods[titleId] = new List<Mod> { mod };
                }
            }
        }

        public void AddPatchMods(DirectoryInfo patchesDir, ModType type)
        {
            foreach (var modDir in patchesDir.EnumerateDirectories())
            {
                var mod = Mod.MakeMod(modDir, type);
                if (mod == null) continue;

                (type == ModType.NroMod ? NroMods : NsoMods).TryAdd(modDir.Name, mod);
            }
        }

        // Apply helpers
        internal IStorage ApplyRomFsMods(ulong titleId, IStorage baseStorage)
        {
            if (!TitleMods.TryGetValue(titleId, out var titleMods))
            {
                return baseStorage;
            }

            var fileSet = new HashSet<string>();
            var builder = new RomFsBuilder();

            Logger.PrintInfo(LogClass.Loader, "Collecting RomFS mods...");

            int count = GatherRomFsMods(titleMods, fileSet, builder);
            if (count == 0)
            {
                Logger.PrintInfo(LogClass.Loader, "Using base RomFS");
                return baseStorage;
            }

            Logger.PrintInfo(LogClass.Loader, $"Found {fileSet.Count} modded files over {count} mods. Processing base storage...");

            var baseRom = new RomFsFileSystem(baseStorage);
            foreach (var entry in baseRom.EnumerateEntries()
                                         .Where(f => f.Type == DirectoryEntryType.File && !fileSet.Contains(f.FullPath))
                                         .OrderBy(f => f.FullPath, StringComparer.Ordinal))
            {
                baseRom.OpenFile(out IFile file, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();
                builder.AddFile(entry.FullPath, file);
            }

            Logger.PrintInfo(LogClass.Loader, "Building new RomFS...");
            IStorage newStorage = builder.Build();
            Logger.PrintInfo(LogClass.Loader, "Using modded RomFS");

            return newStorage;
        }

        private static int GatherRomFsMods(IEnumerable<Mod> titleMods, HashSet<string> fileSet, RomFsBuilder builder)
        {
            int modCount = 0;

            foreach (var mod in titleMods.Where(mod => mod.Check())) // Filter enabled and existing
            {
                IFileSystem fs;
                if (mod.RomfsFile.Exists) // Prioritize RomFS file
                {
                    fs = new RomFsFileSystem(mod.RomfsFile.OpenRead().AsStorage());
                }
                else if (mod.Romfs.Exists)
                {
                    fs = new LocalFileSystem(mod.Romfs.FullName);
                }
                else
                {
                    continue;
                }

                using (fs)
                {
                    foreach (var entry in fs.EnumerateEntries()
                                        .Where(f => f.Type == DirectoryEntryType.File)
                                        .OrderBy(f => f.FullPath, StringComparer.Ordinal))
                    {
                        fs.OpenFile(out IFile file, entry.FullPath.ToU8Span(), OpenMode.Read).ThrowIfFailure();
                        if (fileSet.Add(entry.FullPath))
                        {
                            builder.AddFile(entry.FullPath, file);
                        }
                        else
                        {
                            Logger.PrintWarning(LogClass.Loader, $"    Skipped duplicate file '{entry.FullPath}' from '{mod.Dir.Name}'");
                        }
                    }
                }

                modCount++;
            }

            return modCount;
        }

        internal void ApplyExefsReplacements(ulong titleId, List<NsoExecutable> nsos)
        {
            if (!TitleMods.TryGetValue(titleId, out var titleMods))
            {
                return;
            }

            if (nsos.Count > 32)
            {
                throw new ArgumentOutOfRangeException("NSO Count is more than 32");
            }

            var exefsDirs = titleMods.Where(mod => mod.Check() && mod.Exefs.Exists)
                            .Select(mod => mod.Exefs);

            BitVector32 stubs = new BitVector32();
            BitVector32 repls = new BitVector32();

            foreach (var exefsDir in exefsDirs)
            {
                for (int i = 0; i < nsos.Count; ++i)
                {
                    var nso = nsos[i];
                    var nsoName = nso.Name;

                    FileInfo nsoFile = new FileInfo(Path.Combine(exefsDir.FullName, nsoName));
                    if (nsoFile.Exists)
                    {
                        if (repls[1 << i])
                        {
                            Logger.PrintWarning(LogClass.Loader, $"Multiple replacements to '{nsoName}'");
                            continue;
                        }

                        repls[1 << i] = true;

                        nsos[i] = new NsoExecutable(nsoFile.OpenRead().AsStorage(), nsoName);
                        Logger.PrintInfo(LogClass.Loader, $"NSO '{nsoName}' replaced");

                        continue;
                    }

                    stubs[1 << i] |= File.Exists(Path.Combine(exefsDir.FullName, nsoName + StubExtension));
                }
            }

            for (int i = nsos.Count - 1; i >= 0; --i)
            {
                if (stubs[1 << i] && !repls[1 << i]) // Prioritizes replacements over stubs
                {
                    Logger.PrintInfo(LogClass.Loader, $"NSO '{nsos[i].Name}' stubbed");
                    nsos.RemoveAt(i);
                }
            }
        }

        internal void ApplyNroPatches(NroExecutable nro)
        {
            var nroPatches = NroMods.Values.Where(mod => mod.Check() && mod.Exefs.Exists);

            // NRO patches aren't offset relative to header unlike NSO
            // according to Atmosphere's ro patcher module
            ApplyProgramPatches(nroPatches, 0, nro);
        }

        internal void ApplyNsoPatches(ulong titleId, params IExecutable[] programs)
        {
            var nsoMods = NsoMods.Values
                          .Concat(TitleMods.TryGetValue(titleId, out var titleMods) ? titleMods : Enumerable.Empty<Mod>())
                          .Where(mod => mod.Check() && mod.Exefs.Exists);

            // NSO patches are created with offset 0 according to Atmosphere's patcher module
            // But `Program` doesn't contain the header which is 0x100 bytes. So, we adjust for that here
            ApplyProgramPatches(nsoMods, 0x100, programs);
        }

        private void ApplyProgramPatches(IEnumerable<Mod> mods, int protectedOffset, params IExecutable[] programs)
        {
            MemPatch[] patches = new MemPatch[programs.Length];

            for (int i = 0; i < patches.Length; ++i)
            {
                patches[i] = new MemPatch();
            }

            var buildIds = programs.Select(p => p switch
            {
                NsoExecutable nso => BitConverter.ToString(nso.BuildId.Bytes.ToArray()).Replace("-", "").TrimEnd('0'),
                NroExecutable nro => BitConverter.ToString(nro.Header.BuildId).Replace("-", "").TrimEnd('0'),
                _ => string.Empty
            }).ToList();

            int GetIndex(string buildId) => buildIds.FindIndex(id => id == buildId); // O(n) but list is small

            // Collect patches
            foreach (var mod in mods)
            {
                var patchDir = mod.Exefs;
                foreach (var patchFile in patchDir.EnumerateFiles())
                {
                    if (string.Equals(".ips", patchFile.Extension, StringComparison.OrdinalIgnoreCase)) // IPS|IPS32
                    {
                        string filename = Path.GetFileNameWithoutExtension(patchFile.FullName).Split('.')[0];
                        string buildId = filename.TrimEnd('0');

                        int index = GetIndex(buildId);
                        if (index == -1)
                        {
                            continue;
                        }

                        Logger.PrintInfo(LogClass.Loader, $"Found IPS patch '{patchFile.Name}' in '{mod.Dir.Name}' bid={buildId}");

                        using var fs = patchFile.OpenRead();
                        using var reader = new BinaryReader(fs);

                        var patcher = new IpsPatcher(reader);
                        patcher.AddPatches(patches[index]);
                    }
                    else if (string.Equals(".pchtxt", patchFile.Extension, StringComparison.OrdinalIgnoreCase)) // IPSwitch
                    {
                        using var fs = patchFile.OpenRead();
                        using var reader = new StreamReader(fs);

                        var patcher = new IPSwitchPatcher(reader);

                        int index = GetIndex(patcher.BuildId);
                        if (index == -1)
                        {
                            continue;
                        }

                        Logger.PrintInfo(LogClass.Loader, $"Found IPSwitch patch '{patchFile.Name}' in '{mod.Dir.Name}' bid={patcher.BuildId}");

                        patcher.AddPatches(patches[index]);
                    }
                }
            }

            // Apply patches
            for (int i = 0; i < programs.Length; ++i)
            {
                patches[i].Patch(programs[i].Program, protectedOffset);
            }
        }
    }
}