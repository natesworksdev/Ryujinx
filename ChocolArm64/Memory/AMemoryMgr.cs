using System;

namespace ChocolArm64.Memory
{
    public class AMemoryMgr
    {
        private ulong RamSize;

        private ulong AddressSpaceSize;

        private int PTLvl0Bits;
        private int PTLvl1Bits;

        private int PTLvl0Size;
        private int PTLvl1Size;

        private int PTLvl0Mask;
        private int PTLvl1Mask;

        private int PTLvl0Bit;

        private const int PTPageBits = 12;

        public  const int PageSize = 1 << PTPageBits;

        public  const int PageMask = PageSize - 1;

        private const int PTLvl1Bit = PTPageBits;

        private enum PTMap : byte
        {
            Unmapped  = 0,
            Mapped    = 1,

            Direct      = 1 << 5,
            DirectRead  = Direct | 1 << 6,
            DirectWrite = Direct | 1 << 7,

            DirectRW =
                DirectRead |
                DirectWrite
        }

        private struct PTEntry
        {
            public PTMap       Map;
            public AMemoryPerm Perm;

            public long Position;
            public int Type;
            public int Attr;

            public PTEntry(PTMap Map, long Position, AMemoryPerm Perm, int Type, int Attr)
            {
                this.Map  = Map;
                this.Position = Position;
                this.Perm = Perm;
                this.Type = Type;
                this.Attr = Attr;
            }
        }

        private PTMap[] MapTypes;

        private PTEntry[][] PageTable;

        public AMemoryMgr(long RamSize, int AddressSpaceBits)
        {
            this.RamSize = (ulong)RamSize;

            AddressSpaceSize = 1UL << AddressSpaceBits;

            int RemBits = AddressSpaceBits - PTPageBits;

            PTLvl0Bits = RemBits >> 1;
            PTLvl1Bits = RemBits - PTLvl0Bits;

            PTLvl0Size = 1 << PTLvl0Bits;
            PTLvl1Size = 1 << PTLvl1Bits;

            PTLvl0Mask = PTLvl0Size - 1;
            PTLvl1Mask = PTLvl1Size - 1;

            PTLvl0Bit = PTPageBits + PTLvl1Bits;

            MapTypes = new PTMap[1 << RemBits];

            PageTable = new PTEntry[PTLvl0Size][];
        }

        public void MapDirectRX(long Position, long Size, int Type)
        {
            if ((ulong)Position >= RamSize)
            {
                throw new ArgumentOutOfRangeException(nameof(Position));
            }

            if ((ulong)(Position + Size) > RamSize)
            {
                throw new ArgumentOutOfRangeException(nameof(Size));
            }

            SetPTEntry(Position, Size, new PTEntry(PTMap.DirectRead, 0, AMemoryPerm.RX, Type, 0));
        }

        public void MapDirectRO(long Position, long Size, int Type)
        {
            if ((ulong)Position >= RamSize)
            {
                throw new ArgumentOutOfRangeException(nameof(Position));
            }

            if ((ulong)(Position + Size) > RamSize)
            {
                throw new ArgumentOutOfRangeException(nameof(Size));
            }

            SetPTEntry(Position, Size, new PTEntry(PTMap.DirectRead, 0, AMemoryPerm.Read, Type, 0));
        }

        public void MapDirectRW(long Position, long Size, int Type)
        {
            if ((ulong)Position >= RamSize)
            {
                throw new ArgumentOutOfRangeException(nameof(Position));
            }

            if ((ulong)(Position + Size) > RamSize)
            {
                throw new ArgumentOutOfRangeException(nameof(Size));
            }

            SetPTEntry(Position, Size, new PTEntry(PTMap.DirectRW, 0, AMemoryPerm.RW, Type, 0));
        }

        public void Map(long VA, long PA, long Size, int Type, AMemoryPerm Perm)
        {
            if ((ulong)VA >= AddressSpaceSize)
            {
                throw new ArgumentOutOfRangeException(nameof(VA));
            }

            if ((ulong)PA >= RamSize)
            {
                throw new ArgumentOutOfRangeException(nameof(PA));
            }

            if ((ulong)(PA + Size) > RamSize || (ulong)(VA + Size) > AddressSpaceSize)
            {
                throw new ArgumentOutOfRangeException(nameof(Size));
            }

            SetPTEntry(VA, Size, new PTEntry(PTMap.Mapped, PA, Perm, Type, 0));
        }

        public void Unmap(long Position, long Size)
        {
            SetPTEntry(Position, Size, new PTEntry(PTMap.Unmapped, 0, 0, 0, 0));
        }

        public void Unmap(long Position, long Size, int Type)
        {
            SetPTEntry(Position, Size, Type, new PTEntry(PTMap.Unmapped, 0, 0, 0, 0));
        }

        public void Reprotect(long Position, long Size, AMemoryPerm Perm)
        {
            Position = AMemoryHelper.PageRoundDown(Position);

            Size = AMemoryHelper.PageRoundUp(Size);

            long PagesCount = Size / PageSize;

            while (PagesCount-- > 0)
            {
                PTEntry Entry = GetPTEntry(Position);

                Entry.Perm = Perm;

                SetPTEntry(Position, Entry);

                Position += PageSize;
            }
        }

        public AMemoryMapInfo GetMapInfo(long Position)
        {
            if (!IsValidPosition(Position))
            {
                return null;
            }

            Position = AMemoryHelper.PageRoundDown(Position);

            PTEntry BaseEntry = GetPTEntry(Position);

            bool IsSameSegment(long Pos)
            {
                if (!IsValidPosition(Pos))
                {
                    return false;
                }

                PTEntry Entry = GetPTEntry(Pos);

                return Entry.Map  == BaseEntry.Map  &&
                       Entry.Perm == BaseEntry.Perm &&
                       Entry.Type == BaseEntry.Type &&
                       Entry.Attr == BaseEntry.Attr;
            }

            long Start = Position;
            long End   = Position + PageSize;

            while (Start > 0 && IsSameSegment(Start - PageSize))
            {
                Start -= PageSize;
            }

            while ((ulong)End < AddressSpaceSize && IsSameSegment(End))
            {
                End += PageSize;
            }

            long Size = End - Start;

            return new AMemoryMapInfo(
                Start,
                Size,
                BaseEntry.Type,
                BaseEntry.Attr,
                BaseEntry.Perm);
        }

        public void ClearAttrBit(long Position, long Size, int Bit)
        {
            while (Size > 0)
            {
                PTEntry Entry = GetPTEntry(Position);

                Entry.Attr &= ~(1 << Bit);

                SetPTEntry(Position, Entry);

                Position += PageSize;
                Size     -= PageSize;
            }
        }

        public void SetAttrBit(long Position, long Size, int Bit)
        {
            while (Size > 0)
            {
                PTEntry Entry = GetPTEntry(Position);

                Entry.Attr |= (1 << Bit);

                SetPTEntry(Position, Entry);

                Position += PageSize;
                Size     -= PageSize;
            }
        }

        public bool HasPermission(long Position, AMemoryPerm Perm)
        {
            return GetPTEntry(Position).Perm.HasFlag(Perm);
        }

        public bool IsValidPosition(long Position)
        {
            if (Position >> PTLvl0Bits + PTLvl1Bits + PTPageBits != 0)
            {
                return false;
            }

            return true;
        }

        public bool IsMapped(long Position)
        {
            if (Position >> PTLvl0Bits + PTLvl1Bits + PTPageBits != 0)
            {
                return false;
            }

            long L0 = (Position >> PTLvl0Bit) & PTLvl0Mask;
            long L1 = (Position >> PTLvl1Bit) & PTLvl1Mask;

            if (PageTable[L0] == null)
            {
                return false;
            }

            return PageTable[L0][L1].Map != PTMap.Unmapped;
        }

        public long TranslatePosition(long Position)
        {
            if (IsDirect(Position))
            {
                return Position;
            }
            else
            {
                long BasePosition = GetPTEntry(Position).Position;

                return BasePosition + (Position & AMemoryMgr.PageMask);
            }
        }

        public bool IsDirect(long Position)
        {
            return (MapTypes[(ulong)Position >> PTPageBits] & PTMap.Direct) != 0;
        }

        public bool IsDirectRead(long Position)
        {
            return (MapTypes[(ulong)Position >> PTPageBits] & PTMap.DirectRead) != 0;
        }

        public bool IsDirectWrite(long Position)
        {
            return (MapTypes[(ulong)Position >> PTPageBits] & PTMap.DirectWrite) != 0;
        }

        private PTEntry GetPTEntry(long Position)
        {
            long L0 = (Position >> PTLvl0Bit) & PTLvl0Mask;
            long L1 = (Position >> PTLvl1Bit) & PTLvl1Mask;

            if (PageTable[L0] == null)
            {
                return default(PTEntry);
            }

            return PageTable[L0][L1];
        }

        private void SetPTEntry(long Position, long Size, PTEntry Entry)
        {
            Entry.Position &= ~PageMask;

            while (Size > 0)
            {
                SetPTEntry(Position, Entry);

                Entry.Position += PageSize;

                Position += PageSize;
                Size     -= PageSize;
            }
        }

        private void SetPTEntry(long Position, long Size, int Type, PTEntry Entry)
        {
            Entry.Position &= ~PageMask;

            while (Size > 0 && GetPTEntry(Position).Type == Type)
            {
                SetPTEntry(Position, Entry);

                Entry.Position += PageSize;

                Position += PageSize;
                Size     -= PageSize;
            }
        }

        private void SetPTEntry(long Position, PTEntry Entry)
        {
            if (!IsValidPosition(Position))
            {
                throw new ArgumentOutOfRangeException(nameof(Position));
            }

            long L0 = (Position >> PTLvl0Bit) & PTLvl0Mask;
            long L1 = (Position >> PTLvl1Bit) & PTLvl1Mask;

            if (PageTable[L0] == null)
            {
                PageTable[L0] = new PTEntry[PTLvl1Size];
            }

            PageTable[L0][L1] = Entry;

            MapTypes[Position >> PTPageBits] = Entry.Map;
        }
    }
}