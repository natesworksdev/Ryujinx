using ChocolArm64.Exceptions;
using ChocolArm64.State;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ChocolArm64.Memory
{
    public unsafe class AMemory
    {
        private const long ErgMask = (4 << AThreadState.ErgSizeLog2) - 1;

        public AMemoryMgr Manager { get; private set; }

        private struct ExMonitor
        {
            public long Position { get; private set; }

            private bool ExState;

            public ExMonitor(long Position, bool ExState)
            {
                this.Position = Position;
                this.ExState  = ExState;
            }

            public bool HasExclusiveAccess(long Position)
            {
                return this.Position == Position && ExState;
            }

            public void Reset()
            {
                ExState = false;
            }
        }

        private Dictionary<int, ExMonitor> Monitors;

        private HashSet<long> ExAddrs;

        private byte* RamPtr;

        public AMemory(IntPtr Ram, long RamSize, int AddressSpaceBits)
        {
            Manager = new AMemoryMgr(RamSize, AddressSpaceBits);

            Monitors = new Dictionary<int, ExMonitor>();

            ExAddrs = new HashSet<long>();

            RamPtr = (byte*)Ram;
        }

        public void RemoveMonitor(int ThreadId)
        {
            lock (Monitors)
            {
                if (Monitors.TryGetValue(ThreadId, out ExMonitor Monitor))
                {
                    ExAddrs.Remove(Monitor.Position);
                }

                Monitors.Remove(ThreadId);
            }
        }

        public void SetExclusive(AThreadState ThreadState, long Position)
        {
            Position &= ~ErgMask;

            lock (Monitors)
            {
                if (Monitors.TryGetValue(ThreadState.ThreadId, out ExMonitor Monitor))
                {
                    ExAddrs.Remove(Monitor.Position);
                }

                bool ExState = ExAddrs.Add(Position);

                Monitor = new ExMonitor(Position, ExState);

                if (!Monitors.TryAdd(ThreadState.ThreadId, Monitor))
                {
                    Monitors[ThreadState.ThreadId] = Monitor;
                }
            }
        }

        public bool TestExclusive(AThreadState ThreadState, long Position)
        {
            Position &= ~ErgMask;

            lock (Monitors)
            {
                if (!Monitors.TryGetValue(ThreadState.ThreadId, out ExMonitor Monitor))
                {
                    return false;
                }

                return Monitor.HasExclusiveAccess(Position);
            }
        }

        public void ClearExclusive(AThreadState ThreadState)
        {
            lock (Monitors)
            {
                if (Monitors.TryGetValue(ThreadState.ThreadId, out ExMonitor Monitor))
                {
                    Monitor.Reset();
                    ExAddrs.Remove(Monitor.Position);
                }
            }
        }

        public bool AcquireAddress(long Position)
        {
            Position &= ~ErgMask;

            lock (Monitors)
            {
                return ExAddrs.Add(Position);
            }
        }

        public void ReleaseAddress(long Position)
        {
            Position &= ~ErgMask;

            lock (Monitors)
            {
                ExAddrs.Remove(Position);
            }
        }

        public sbyte ReadSByte(long Position) => (sbyte)ReadByte  (Position);
        public short ReadInt16(long Position) => (short)ReadUInt16(Position);
        public int   ReadInt32(long Position) =>   (int)ReadUInt32(Position);
        public long  ReadInt64(long Position) =>  (long)ReadUInt64(Position);

        public byte ReadByte(long Position)
        {
            byte* Ptr = RamPtr + Position;

            if (Manager.IsDirectRead(Position))
            {
                return *Ptr;
            }

            return ReadByteSlow(Position);
        }

        private byte ReadByteSlow(long Position)
        {
            EnsureAccessIsValid(Position, AMemoryPerm.Read);

            long PA = Manager.TranslatePosition(Position);

            return *(RamPtr + PA);
        }

        public ushort ReadUInt16(long Position)
        {
            ushort* Ptr = (ushort*)(RamPtr + Position);

            if (Manager.IsDirectRead(Position) &&
                Manager.IsDirectRead(Position + 1))
            {
                return *Ptr;
            }

            return ReadUInt16Slow(Position);
        }

        private ushort ReadUInt16Slow(long Position)
        {
            EnsureAccessIsValid(Position,     AMemoryPerm.Read);
            EnsureAccessIsValid(Position + 1, AMemoryPerm.Read);

            long PA = Manager.TranslatePosition(Position);

            return *((ushort*)(RamPtr + PA));
        }

        public uint ReadUInt32(long Position)
        {
            uint* Ptr = (uint*)(RamPtr + Position);

            if (Manager.IsDirectRead(Position) &&
                Manager.IsDirectRead(Position + 3))
            {
                return *Ptr;
            }

            return ReadUInt32Slow(Position);
        }

        private uint ReadUInt32Slow(long Position)
        {
            EnsureAccessIsValid(Position,     AMemoryPerm.Read);
            EnsureAccessIsValid(Position + 3, AMemoryPerm.Read);

            long PA = Manager.TranslatePosition(Position);

            return *((uint*)(RamPtr + PA));
        }

        public ulong ReadUInt64(long Position)
        {
            ulong* Ptr = (ulong*)(RamPtr + Position);

            if (Manager.IsDirectRead(Position) &&
                Manager.IsDirectRead(Position + 7))
            {
                return *Ptr;
            }

            return ReadUInt64Slow(Position);
        }

        private ulong ReadUInt64Slow(long Position)
        {
            EnsureAccessIsValid(Position,     AMemoryPerm.Read);
            EnsureAccessIsValid(Position + 7, AMemoryPerm.Read);

            long PA = Manager.TranslatePosition(Position);

            return *((ulong*)(RamPtr + PA));
        }

        public AVec ReadVector8(long Position)
        {
            return new AVec() { B0 = ReadByte(Position) };
        }

        public AVec ReadVector16(long Position)
        {
            return new AVec() { H0 = ReadUInt16(Position) };
        }

        public AVec ReadVector32(long Position)
        {
            return new AVec() { W0 = ReadUInt32(Position) };
        }

        public AVec ReadVector64(long Position)
        {
            return new AVec() { X0 = ReadUInt64(Position) };
        }

        public AVec ReadVector128(long Position)
        {
            return new AVec()
            {
                X0 = ReadUInt64(Position + 0),
                X1 = ReadUInt64(Position + 8)
            };
        }

        public void WriteSByte(long Position, sbyte Value) => WriteByte  (Position,   (byte)Value);
        public void WriteInt16(long Position, short Value) => WriteUInt16(Position, (ushort)Value);
        public void WriteInt32(long Position, int   Value) => WriteUInt32(Position,   (uint)Value);
        public void WriteInt64(long Position, long  Value) => WriteUInt64(Position,  (ulong)Value);

        public void WriteByte(long Position, byte Value)
        {
            byte* Ptr = RamPtr + Position;

            if (Manager.IsDirectWrite(Position))
            {
                *Ptr = Value;

                return;
            }

            WriteByteSlow(Position, Value);
        }

        private void WriteByteSlow(long Position, byte Value)
        {
            EnsureAccessIsValid(Position, AMemoryPerm.Write);

            long PA = Manager.TranslatePosition(Position);

            *(RamPtr + PA) = Value;
        }

        public void WriteUInt16(long Position, ushort Value)
        {
            ushort* Ptr = (ushort*)(RamPtr + Position);

            if (Manager.IsDirectWrite(Position) &&
                Manager.IsDirectWrite(Position + 1))
            {
                *Ptr = Value;

                return;
            }

            WriteUInt16Slow(Position, Value);
        }

        private void WriteUInt16Slow(long Position, ushort Value)
        {
            EnsureAccessIsValid(Position,     AMemoryPerm.Write);
            EnsureAccessIsValid(Position + 1, AMemoryPerm.Write);

            long PA = Manager.TranslatePosition(Position);

            *((ushort*)(RamPtr + PA)) = Value;
        }

        public void WriteUInt32(long Position, uint Value)
        {
            uint* Ptr = (uint*)(RamPtr + Position);

            if (Manager.IsDirectWrite(Position) &&
                Manager.IsDirectWrite(Position + 3))
            {
                *Ptr = Value;

                return;
            }

            WriteUInt32Slow(Position, Value);
        }

        private void WriteUInt32Slow(long Position, uint Value)
        {
            EnsureAccessIsValid(Position,     AMemoryPerm.Write);
            EnsureAccessIsValid(Position + 3, AMemoryPerm.Write);

            long PA = Manager.TranslatePosition(Position);

            *((uint*)(RamPtr + PA)) = Value;
        }

        public void WriteUInt64(long Position, ulong Value)
        {
            ulong* Ptr = (ulong*)(RamPtr + Position);

            if (Manager.IsDirectWrite(Position) &&
                Manager.IsDirectWrite(Position + 7))
            {
                *Ptr = Value;

                return;
            }

            WriteUInt64Slow(Position, Value);
        }

        private void WriteUInt64Slow(long Position, ulong Value)
        {
            EnsureAccessIsValid(Position,     AMemoryPerm.Write);
            EnsureAccessIsValid(Position + 7, AMemoryPerm.Write);

            long PA = Manager.TranslatePosition(Position);

            *((ulong*)(RamPtr + PA)) = Value;
        }

        public void WriteVector8(long Position, AVec Value)
        {
            WriteByte(Position, Value.B0);
        }

        public void WriteVector16(long Position, AVec Value)
        {
            WriteUInt16(Position, Value.H0);
        }

        public void WriteVector32(long Position, AVec Value)
        {
            WriteUInt32(Position, Value.W0);
        }

        public void WriteVector64(long Position, AVec Value)
        {
            WriteUInt64(Position, Value.X0);
        }

        public void WriteVector128(long Position, AVec Value)
        {
            WriteUInt64(Position + 0, Value.X0);
            WriteUInt64(Position + 8, Value.X1);
        }

        public void WriteDirectUnchecked(long Position, byte[] Data)
        {
            byte* Ptr = (byte*)(RamPtr + Position);

            int Pages = (Data.Length + AMemoryMgr.PageMask) / AMemoryMgr.PageSize;

            int RemSize = Data.Length;

            for (int Page = 0; Page < Pages; Page++)
            {
                int Offset = Page * AMemoryMgr.PageSize;

                if (!Manager.IsMapped(Position + Offset))
                {
                    throw new VmmPageFaultException(Position + Offset);
                }

                int ToCopy = Math.Min(AMemoryMgr.PageSize, RemSize);

                for (; Offset < Page * AMemoryMgr.PageSize + ToCopy; Offset++)
                {
                    *Ptr++ = Data[Offset];
                }

                RemSize -= AMemoryMgr.PageSize;
            }
        }

        private void EnsureAccessIsValid(long Position, AMemoryPerm Perm)
        {
            if (!Manager.IsMapped(Position))
            {
                throw new VmmPageFaultException(Position);
            }

            if (!Manager.HasPermission(Position, Perm))
            {
                throw new VmmAccessViolationException(Position, Perm);
            }
        }
    }
}