using System;
using System.Diagnostics.Contracts;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Ryujinx.Tests.Unicorn
{
    public class UnicornAArch64
    {
        internal readonly IntPtr Uc;

        public IndexedProperty<int, ulong> X
        {
            get
            {
                return new IndexedProperty<int, ulong>(
                    (int i) => GetX(i),
                    (int i, ulong value) => SetX(i, value));
            }
        }

        public IndexedProperty<int, Vector128<float>> Q
        {
            get
            {
                return new IndexedProperty<int, Vector128<float>>(
                    (int i) => GetQ(i),
                    (int i, Vector128<float> value) => SetQ(i, value));
            }
        }

        public ulong Lr
        {
            get => GetRegister(Native.ArmRegister.Lr);
            set => SetRegister(Native.ArmRegister.Lr, value);
        }

        public ulong Sp
        {
            get => GetRegister(Native.ArmRegister.Sp);
            set => SetRegister(Native.ArmRegister.Sp, value);
        }

        public ulong Pc
        {
            get => GetRegister(Native.ArmRegister.Pc);
            set => SetRegister(Native.ArmRegister.Pc, value);
        }

        public uint Pstate
        {
            get => (uint)GetRegister(Native.ArmRegister.Pstate);
            set => SetRegister(Native.ArmRegister.Pstate, (uint)value);
        }

        public int Fpcr
        {
            get => (int)GetRegister(Native.ArmRegister.Fpcr);
            set => SetRegister(Native.ArmRegister.Fpcr, (uint)value);
        }

        public int Fpsr
        {
            get => (int)GetRegister(Native.ArmRegister.Fpsr);
            set => SetRegister(Native.ArmRegister.Fpsr, (uint)value);
        }

        public bool OverflowFlag
        {
            get => (Pstate & 0x10000000u) != 0;
            set => Pstate = (Pstate & ~0x10000000u) | (value ? 0x10000000u : 0u);
        }

        public bool CarryFlag
        {
            get => (Pstate & 0x20000000u) != 0;
            set => Pstate = (Pstate & ~0x20000000u) | (value ? 0x20000000u : 0u);
        }

        public bool ZeroFlag
        {
            get => (Pstate & 0x40000000u) != 0;
            set => Pstate = (Pstate & ~0x40000000u) | (value ? 0x40000000u : 0u);
        }

        public bool NegativeFlag
        {
            get => (Pstate & 0x80000000u) != 0;
            set => Pstate = (Pstate & ~0x80000000u) | (value ? 0x80000000u : 0u);
        }

        public UnicornAArch64()
        {
            Native.Interface.Checked(Native.Interface.uc_open((uint)Native.UnicornArch.UcArchArm64, (uint)Native.UnicornMode.UcModeLittleEndian, out Uc));
            SetRegister(Native.ArmRegister.CpacrEl1, 0x00300000);
        }

        ~UnicornAArch64()
        {
            Native.Interface.Checked(Native.Interface.uc_close(Uc));
        }

        public void RunForCount(ulong count)
        {
            Native.Interface.Checked(Native.Interface.uc_emu_start(Uc, Pc, 0xFFFFFFFFFFFFFFFFu, 0, count));
        }

        public void Step()
        {
            RunForCount(1);
        }

        internal static Native.ArmRegister[] XRegisters = new Native.ArmRegister[31]
        {
            Native.ArmRegister.X0,
            Native.ArmRegister.X1,
            Native.ArmRegister.X2,
            Native.ArmRegister.X3,
            Native.ArmRegister.X4,
            Native.ArmRegister.X5,
            Native.ArmRegister.X6,
            Native.ArmRegister.X7,
            Native.ArmRegister.X8,
            Native.ArmRegister.X9,
            Native.ArmRegister.X10,
            Native.ArmRegister.X11,
            Native.ArmRegister.X12,
            Native.ArmRegister.X13,
            Native.ArmRegister.X14,
            Native.ArmRegister.X15,
            Native.ArmRegister.X16,
            Native.ArmRegister.X17,
            Native.ArmRegister.X18,
            Native.ArmRegister.X19,
            Native.ArmRegister.X20,
            Native.ArmRegister.X21,
            Native.ArmRegister.X22,
            Native.ArmRegister.X23,
            Native.ArmRegister.X24,
            Native.ArmRegister.X25,
            Native.ArmRegister.X26,
            Native.ArmRegister.X27,
            Native.ArmRegister.X28,
            Native.ArmRegister.X29,
            Native.ArmRegister.X30,
        };

        internal static Native.ArmRegister[] QRegisters = new Native.ArmRegister[32]
        {
            Native.ArmRegister.Q0,
            Native.ArmRegister.Q1,
            Native.ArmRegister.Q2,
            Native.ArmRegister.Q3,
            Native.ArmRegister.Q4,
            Native.ArmRegister.Q5,
            Native.ArmRegister.Q6,
            Native.ArmRegister.Q7,
            Native.ArmRegister.Q8,
            Native.ArmRegister.Q9,
            Native.ArmRegister.Q10,
            Native.ArmRegister.Q11,
            Native.ArmRegister.Q12,
            Native.ArmRegister.Q13,
            Native.ArmRegister.Q14,
            Native.ArmRegister.Q15,
            Native.ArmRegister.Q16,
            Native.ArmRegister.Q17,
            Native.ArmRegister.Q18,
            Native.ArmRegister.Q19,
            Native.ArmRegister.Q20,
            Native.ArmRegister.Q21,
            Native.ArmRegister.Q22,
            Native.ArmRegister.Q23,
            Native.ArmRegister.Q24,
            Native.ArmRegister.Q25,
            Native.ArmRegister.Q26,
            Native.ArmRegister.Q27,
            Native.ArmRegister.Q28,
            Native.ArmRegister.Q29,
            Native.ArmRegister.Q30,
            Native.ArmRegister.Q31,
        };

        internal ulong GetRegister(Native.ArmRegister register)
        {
            byte[] valueBytes = new byte[8];
            Native.Interface.Checked(Native.Interface.uc_reg_read(Uc, (int)register, valueBytes));
            return (ulong)BitConverter.ToInt64(valueBytes, 0);
        }

        internal void SetRegister(Native.ArmRegister register, ulong value)
        {
            byte[] valueBytes = BitConverter.GetBytes(value);
            Native.Interface.Checked(Native.Interface.uc_reg_write(Uc, (int)register, valueBytes));
        }

        internal Vector128<float> GetVector(Native.ArmRegister register)
        {
            byte[] valueBytes = new byte[16];
            Native.Interface.Checked(Native.Interface.uc_reg_read(Uc, (int)register, valueBytes));
            unsafe
            {
                fixed (byte* p = &valueBytes[0])
                {
                    return Sse.LoadVector128((float*)p);
                }
            }
        }

        internal void SetVector(Native.ArmRegister register, Vector128<float> value)
        {
            byte[] valueBytes = new byte[16];
            unsafe
            {
                fixed (byte* p = &valueBytes[0])
                {
                    Sse.Store((float*)p, value);
                }
            }
            Native.Interface.Checked(Native.Interface.uc_reg_write(Uc, (int)register, valueBytes));
        }

        public ulong GetX(int index)
        {
            Contract.Requires(index <= 30, "invalid register");

            return GetRegister(XRegisters[index]);
        }

        public void SetX(int index, ulong value)
        {
            Contract.Requires(index <= 30, "invalid register");

            SetRegister(XRegisters[index], value);
        }

        public Vector128<float> GetQ(int index)
        {
            Contract.Requires(index <= 31, "invalid vector");

            return GetVector(QRegisters[index]);
        }

        public void SetQ(int index, Vector128<float> value)
        {
            Contract.Requires(index <= 31, "invalid vector");

            SetVector(QRegisters[index], value);
        }

        public byte[] MemoryRead(ulong address, ulong size)
        {
            byte[] value = new byte[size];
            Native.Interface.Checked(Native.Interface.uc_mem_read(Uc, address, value, size));
            return value;
        }

        public byte   MemoryRead8 (ulong address) { return MemoryRead(address, 1)[0]; }
        public UInt16 MemoryRead16(ulong address) { return (UInt16)BitConverter.ToInt16(MemoryRead(address, 2), 0); }
        public UInt32 MemoryRead32(ulong address) { return (UInt32)BitConverter.ToInt32(MemoryRead(address, 4), 0); }
        public UInt64 MemoryRead64(ulong address) { return (UInt64)BitConverter.ToInt64(MemoryRead(address, 8), 0); }

        public void MemoryWrite(ulong address, byte[] value)
        {
            Native.Interface.Checked(Native.Interface.uc_mem_write(Uc, address, value, (ulong)value.Length));
        }

        public void MemoryWrite8 (ulong address, byte value)   { MemoryWrite(address, new byte[]{value}); }
        public void MemoryWrite16(ulong address, Int16 value)  { MemoryWrite(address, BitConverter.GetBytes(value)); }
        public void MemoryWrite16(ulong address, UInt16 value) { MemoryWrite(address, BitConverter.GetBytes(value)); }
        public void MemoryWrite32(ulong address, Int32 value)  { MemoryWrite(address, BitConverter.GetBytes(value)); }
        public void MemoryWrite32(ulong address, UInt32 value) { MemoryWrite(address, BitConverter.GetBytes(value)); }
        public void MemoryWrite64(ulong address, Int64 value)  { MemoryWrite(address, BitConverter.GetBytes(value)); }
        public void MemoryWrite64(ulong address, UInt64 value) { MemoryWrite(address, BitConverter.GetBytes(value)); }

        public void MemoryMap(ulong address, ulong size, MemoryPermission permissions)
        {
            Native.Interface.Checked(Native.Interface.uc_mem_map(Uc, address, size, (uint)permissions));
        }

        public void MemoryUnmap(ulong address, ulong size)
        {
            Native.Interface.Checked(Native.Interface.uc_mem_unmap(Uc, address, size));
        }

        public void MemoryProtect(ulong address, ulong size, MemoryPermission permissions)
        {
            Native.Interface.Checked(Native.Interface.uc_mem_protect(Uc, address, size, (uint)permissions));
        }

        public void DumpMemoryInformation()
        {
            Native.Interface.Checked(Native.Interface.uc_mem_regions(Uc, out IntPtr regionsRaw, out uint length));
            Native.Interface.MarshalArrayOf<Native.UnicornMemoryRegion>(regionsRaw, (int)length, out var regions);
            foreach (var region in regions) Console.WriteLine("region: begin {0:X16} end {1:X16} perms {2:X8}", region.begin, region.end, region.perms);
        }

        public static bool IsAvailable()
        {
            try
            {
                Native.Interface.uc_version(out uint major, out uint minor);
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }
    }
}