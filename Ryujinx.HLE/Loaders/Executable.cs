using ChocolArm64.Memory;
using Ryujinx.HLE.HOS;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.Loaders.Executables;
using Ryujinx.HLE.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Ryujinx.HLE.Loaders
{
    internal class Executable
    {
        private AMemory _memory;

        private List<ElfDyn> _dynamic;

        public ReadOnlyCollection<ElfSym> SymbolTable;

        public string Name { get; private set; }

        public string FilePath { get; private set; }

        public long ImageBase { get; private set; }
        public long ImageEnd  { get; private set; }

        private KMemoryManager _memoryManager;

        public Executable(IExecutable exe, KMemoryManager memoryManager, AMemory memory, long imageBase)
        {
            _dynamic = new List<ElfDyn>();

            FilePath = exe.FilePath;

            if (FilePath != null) Name = Path.GetFileNameWithoutExtension(FilePath.Replace(Homebrew.TemporaryNroSuffix, ""));

            _memory        = memory;
            _memoryManager = memoryManager;
            ImageBase     = imageBase;
            ImageEnd      = imageBase;

            long textPosition = imageBase + (uint)exe.TextOffset;
            long roPosition   = imageBase + (uint)exe.RoOffset;
            long dataPosition = imageBase + (uint)exe.DataOffset;

            long textSize = (uint)IntUtils.AlignUp(exe.Text.Length, KMemoryManager.PageSize);
            long roSize   = (uint)IntUtils.AlignUp(exe.Ro.Length, KMemoryManager.PageSize);
            long dataSize = (uint)IntUtils.AlignUp(exe.Data.Length, KMemoryManager.PageSize);
            long bssSize  = (uint)IntUtils.AlignUp(exe.BssSize, KMemoryManager.PageSize);

            long dataAndBssSize = bssSize + dataSize;

            ImageEnd = dataPosition + dataAndBssSize;

            if (exe.SourceAddress == 0)
            {
                memoryManager.HleMapProcessCode(textPosition, textSize + roSize + dataAndBssSize);

                memoryManager.SetProcessMemoryPermission(roPosition, roSize, MemoryPermission.Read);
                memoryManager.SetProcessMemoryPermission(dataPosition, dataAndBssSize, MemoryPermission.ReadAndWrite);

                memory.WriteBytes(textPosition, exe.Text);
                memory.WriteBytes(roPosition, exe.Ro);
                memory.WriteBytes(dataPosition, exe.Data);
            }
            else
            {
                long result = memoryManager.MapProcessCodeMemory(textPosition, exe.SourceAddress, textSize + roSize + dataSize);

                if (result != 0) throw new InvalidOperationException();

                memoryManager.SetProcessMemoryPermission(roPosition, roSize, MemoryPermission.Read);
                memoryManager.SetProcessMemoryPermission(dataPosition, dataSize, MemoryPermission.ReadAndWrite);

                if (exe.BssAddress != 0 && exe.BssSize != 0)
                {
                    result = memoryManager.MapProcessCodeMemory(dataPosition + dataSize, exe.BssAddress, bssSize);

                    if (result != 0) throw new InvalidOperationException();

                    memoryManager.SetProcessMemoryPermission(dataPosition + dataSize, bssSize, MemoryPermission.ReadAndWrite);
                }
            }

            if (exe.Mod0Offset == 0) return;

            long mod0Offset = imageBase + exe.Mod0Offset;

            int  mod0Magic        = memory.ReadInt32(mod0Offset + 0x0);
            long dynamicOffset    = memory.ReadInt32(mod0Offset + 0x4)  + mod0Offset;
            long bssStartOffset   = memory.ReadInt32(mod0Offset + 0x8)  + mod0Offset;
            long bssEndOffset     = memory.ReadInt32(mod0Offset + 0xc)  + mod0Offset;
            long ehHdrStartOffset = memory.ReadInt32(mod0Offset + 0x10) + mod0Offset;
            long ehHdrEndOffset   = memory.ReadInt32(mod0Offset + 0x14) + mod0Offset;
            long modObjOffset     = memory.ReadInt32(mod0Offset + 0x18) + mod0Offset;

            while (true)
            {
                long tagVal = memory.ReadInt64(dynamicOffset + 0);
                long value  = memory.ReadInt64(dynamicOffset + 8);

                dynamicOffset += 0x10;

                ElfDynTag tag = (ElfDynTag)tagVal;

                if (tag == ElfDynTag.DtNull) break;

                _dynamic.Add(new ElfDyn(tag, value));
            }

            long strTblAddr = imageBase + GetFirstValue(ElfDynTag.DtStrtab);
            long symTblAddr = imageBase + GetFirstValue(ElfDynTag.DtSymtab);

            long symEntSize = GetFirstValue(ElfDynTag.DtSyment);

            List<ElfSym> symbols = new List<ElfSym>();

            while ((ulong)symTblAddr < (ulong)strTblAddr)
            {
                ElfSym sym = GetSymbol(symTblAddr, strTblAddr);

                symbols.Add(sym);

                symTblAddr += symEntSize;
            }

            SymbolTable = Array.AsReadOnly(symbols.OrderBy(x => x.Value).ToArray());
        }

        private ElfRel GetRelocation(long position)
        {
            long offset = _memory.ReadInt64(position + 0);
            long info   = _memory.ReadInt64(position + 8);
            long addend = _memory.ReadInt64(position + 16);

            int relType = (int)(info >> 0);
            int symIdx  = (int)(info >> 32);

            ElfSym symbol = GetSymbol(symIdx);

            return new ElfRel(offset, addend, symbol, (ElfRelType)relType);
        }

        private ElfSym GetSymbol(int index)
        {
            long strTblAddr = ImageBase + GetFirstValue(ElfDynTag.DtStrtab);
            long symTblAddr = ImageBase + GetFirstValue(ElfDynTag.DtSymtab);

            long symEntSize = GetFirstValue(ElfDynTag.DtSyment);

            long position = symTblAddr + index * symEntSize;

            return GetSymbol(position, strTblAddr);
        }

        private ElfSym GetSymbol(long position, long strTblAddr)
        {
            int  nameIndex = _memory.ReadInt32(position + 0);
            int  info      = _memory.ReadByte(position + 4);
            int  other     = _memory.ReadByte(position + 5);
            int  shIdx     = _memory.ReadInt16(position + 6);
            long value     = _memory.ReadInt64(position + 8);
            long size      = _memory.ReadInt64(position + 16);

            string name = string.Empty;

            for (int chr; (chr = _memory.ReadByte(strTblAddr + nameIndex++)) != 0;) name += (char)chr;

            return new ElfSym(name, info, other, shIdx, value, size);
        }

        private long GetFirstValue(ElfDynTag tag)
        {
            foreach (ElfDyn entry in _dynamic)
                if (entry.Tag == tag) return entry.Value;

            return 0;
        }
    }
}