using ARMeilleure;
using ARMeilleure.State;
using ARMeilleure.Translation;
using NUnit.Framework;
using Ryujinx.Cpu;
using Ryujinx.Memory;
using System;

namespace Ryujinx.Tests.Cpu
{
    [TestFixture]
    public class CpuTest32
    {
        protected const uint Size = 0x1000;
        protected const uint CodeBaseAddress = 0x1000;
        protected const uint DataBaseAddress = CodeBaseAddress + Size;

        private uint _currAddress;

        private MemoryBlock _ram;

        private MemoryManager _memory;

        private ExecutionContext _context;

        private CpuContext _cpuContext;

        private static byte[] _dynarmicMemory;
        private static Dynarmic.Net.A32TestEnv _dynarmicEnv;
        private static Dynarmic.Net.A32Jit _dynarmic;

        private bool _usingMemory;

        static CpuTest32()
        {
            _dynarmicMemory = new byte[Size * 3];
            _dynarmicEnv = new Dynarmic.Net.A32TestEnv(_dynarmicMemory);
            _dynarmic = new Dynarmic.Net.A32Jit(_dynarmicEnv, new Dynarmic.Net.A32Config{ EnableOptimizations = false });
        }

        [SetUp]
        public void Setup()
        {
            _currAddress = CodeBaseAddress;

            _ram = new MemoryBlock(Size * 2);
            _memory = new MemoryManager(1ul << 16);
            _memory.IncrementReferenceCount();
            _memory.Map(CodeBaseAddress, _ram.GetPointer(0, Size * 2), Size * 2);

            _context = CpuContext.CreateExecutionContext();
            _context.IsAarch32 = true;
            Translator.IsReadyForTranslation.Set();

            _cpuContext = new CpuContext(_memory, for64Bit: false);

            // Prevent registering LCQ functions in the FunctionTable to avoid initializing and populating the table,
            // which improves test durations.
            Optimizations.AllowLcqInFunctionTable = false;
            Optimizations.UseUnmanagedDispatchLoop = false;

            _dynarmic.Reset();
        }

        [TearDown]
        public void Teardown()
        {
            _memory.DecrementReferenceCount();
            _context.Dispose();
            _ram.Dispose();

            _memory     = null;
            _context    = null;
            _cpuContext = null;

            _dynarmic.ClearCache();

            _usingMemory = false;
        }

        protected void Reset()
        {
            Teardown();
            Setup();
        }

        protected void Opcode(uint opcode)
        {
            _memory.Write(_currAddress, opcode);

            _dynarmicEnv.MemoryWrite32(_currAddress, opcode);

            _currAddress += 4;
        }

        protected void ThumbOpcode(ushort opcode)
        {
            _memory.Write(_currAddress, opcode);

            _dynarmicEnv.MemoryWrite16(_currAddress, opcode);

            _currAddress += 2;
        }

        protected ExecutionContext GetContext() => _context;
        protected Dynarmic.Net.A32Jit GetDynarmic() => _dynarmic;

        protected void SetContext(uint r0 = 0,
                                  uint r1 = 0,
                                  uint r2 = 0,
                                  uint r3 = 0,
                                  uint sp = 0,
                                  V128 v0 = default,
                                  V128 v1 = default,
                                  V128 v2 = default,
                                  V128 v3 = default,
                                  V128 v4 = default,
                                  V128 v5 = default,
                                  V128 v14 = default,
                                  V128 v15 = default,
                                  bool saturation = false,
                                  bool overflow = false,
                                  bool carry = false,
                                  bool zero = false,
                                  bool negative = false,
                                  int fpscr = 0,
                                  bool thumb = false)
        {
            _context.SetX(0, r0);
            _context.SetX(1, r1);
            _context.SetX(2, r2);
            _context.SetX(3, r3);
            _context.SetX(13, sp);

            _context.SetV(0, v0);
            _context.SetV(1, v1);
            _context.SetV(2, v2);
            _context.SetV(3, v3);
            _context.SetV(4, v4);
            _context.SetV(5, v5);
            _context.SetV(14, v14);
            _context.SetV(15, v15);

            _context.SetPstateFlag(PState.QFlag, saturation);
            _context.SetPstateFlag(PState.VFlag, overflow);
            _context.SetPstateFlag(PState.CFlag, carry);
            _context.SetPstateFlag(PState.ZFlag, zero);
            _context.SetPstateFlag(PState.NFlag, negative);
            _context.SetPstateFlag(PState.TFlag, thumb);

            SetFpscr((uint)fpscr);

            _dynarmic.Reg[0] = r0;
            _dynarmic.Reg[1] = r1;
            _dynarmic.Reg[2] = r2;
            _dynarmic.Reg[3] = r3;
            _dynarmic.Reg[13] = sp;

            Action<int, V128> setV = (int index, V128 value) =>
            {
                _dynarmic.ExtReg[index * 4 + 0] = value.Extract<uint>(0);
                _dynarmic.ExtReg[index * 4 + 1] = value.Extract<uint>(1);
                _dynarmic.ExtReg[index * 4 + 2] = value.Extract<uint>(2);
                _dynarmic.ExtReg[index * 4 + 3] = value.Extract<uint>(3);
            };
            setV(0, v0);
            setV(1, v1);
            setV(2, v2);
            setV(3, v3);
            setV(4, v4);
            setV(5, v5);
            setV(14, v14);
            setV(15, v15);

            _dynarmic.Cpsr = 0;
            _dynarmic.Cpsr |= saturation ? 1u << (int)PState.QFlag : 0;
            _dynarmic.Cpsr |= overflow ? 1u << (int)PState.VFlag : 0;
            _dynarmic.Cpsr |= carry ? 1u << (int)PState.CFlag : 0;
            _dynarmic.Cpsr |= zero ? 1u << (int)PState.ZFlag : 0;
            _dynarmic.Cpsr |= negative ? 1u << (int)PState.NFlag : 0;
            _dynarmic.Cpsr |= thumb ? 1u << (int)PState.TFlag : 0;

            _dynarmic.Fpscr = (uint)fpscr;
        }

        protected void ExecuteOpcodes()
        {
            _cpuContext.Execute(_context, CodeBaseAddress);

            _dynarmic.Reg[15] = CodeBaseAddress;
            _dynarmicEnv.TicksRemaining = (_currAddress - CodeBaseAddress - 4) / 4;
            _dynarmic.Run();
        }

        protected ExecutionContext SingleOpcode(uint opcode,
                                                uint r0 = 0,
                                                uint r1 = 0,
                                                uint r2 = 0,
                                                uint r3 = 0,
                                                uint sp = 0,
                                                V128 v0 = default,
                                                V128 v1 = default,
                                                V128 v2 = default,
                                                V128 v3 = default,
                                                V128 v4 = default,
                                                V128 v5 = default,
                                                V128 v14 = default,
                                                V128 v15 = default,
                                                bool saturation = false,
                                                bool overflow = false,
                                                bool carry = false,
                                                bool zero = false,
                                                bool negative = false,
                                                int fpscr = 0)
        {
            Opcode(opcode);
            Opcode(0xE12FFF1E); // BX LR
            SetContext(r0, r1, r2, r3, sp, v0, v1, v2, v3, v4, v5, v14, v15, saturation, overflow, carry, zero, negative, fpscr);
            ExecuteOpcodes();

            return GetContext();
        }

        protected ExecutionContext SingleThumbOpcode(ushort opcode,
                                                     uint r0 = 0,
                                                     uint r1 = 0,
                                                     uint r2 = 0,
                                                     uint r3 = 0,
                                                     uint sp = 0,
                                                     bool saturation = false,
                                                     bool overflow = false,
                                                     bool carry = false,
                                                     bool zero = false,
                                                     bool negative = false,
                                                     int fpscr = 0)
        {
            ThumbOpcode(opcode);
            ThumbOpcode(0x4770); // BX LR
            SetContext(r0, r1, r2, r3, sp, default, default, default, default, default, default, default, default, saturation, overflow, carry, zero, negative, fpscr, thumb: true);
            ExecuteOpcodes();

            return GetContext();
        }

        protected void SetWorkingMemory(uint offset, byte[] data)
        {
            _memory.Write(DataBaseAddress + offset, data);

            data.CopyTo(_dynarmicMemory, DataBaseAddress + offset);

            _usingMemory = true; // When true, CompareAgainstDynarmic checks the working memory for equality too.
        }

        /// <summary>Rounding Mode control field.</summary>
        public enum RMode
        {
            /// <summary>Round to Nearest mode.</summary>
            Rn,
            /// <summary>Round towards Plus Infinity mode.</summary>
            Rp,
            /// <summary>Round towards Minus Infinity mode.</summary>
            Rm,
            /// <summary>Round towards Zero mode.</summary>
            Rz
        };

        /// <summary>Floating-point Control Register.</summary>
        protected enum Fpcr
        {
            /// <summary>Rounding Mode control field.</summary>
            RMode = 22,
            /// <summary>Flush-to-zero mode control bit.</summary>
            Fz = 24,
            /// <summary>Default NaN mode control bit.</summary>
            Dn = 25,
            /// <summary>Alternative half-precision control bit.</summary>
            Ahp = 26
        }

        /// <summary>Floating-point Status Register.</summary>
        [Flags]
        protected enum Fpsr
        {
            None = 0,

            /// <summary>Invalid Operation cumulative floating-point exception bit.</summary>
            Ioc = 1 << 0,
            /// <summary>Divide by Zero cumulative floating-point exception bit.</summary>
            Dzc = 1 << 1,
            /// <summary>Overflow cumulative floating-point exception bit.</summary>
            Ofc = 1 << 2,
            /// <summary>Underflow cumulative floating-point exception bit.</summary>
            Ufc = 1 << 3,
            /// <summary>Inexact cumulative floating-point exception bit.</summary>
            Ixc = 1 << 4,
            /// <summary>Input Denormal cumulative floating-point exception bit.</summary>
            Idc = 1 << 7,

            /// <summary>Cumulative saturation bit.</summary>
            Qc = 1 << 27,

            /// <summary>NZCV flags.</summary>
            Nzcv = (1 << 31) | (1 << 30) | (1 << 29) | (1 << 28)
        }

        [Flags]
        protected enum FpSkips
        {
            None = 0,

            IfNaNS = 1,
            IfNaND = 2,

            IfUnderflow = 4,
            IfOverflow = 8
        }

        protected enum FpTolerances
        {
            None,

            UpToOneUlpsS,
            UpToOneUlpsD
        }

        protected void CompareAgainstDynarmic(
            Fpsr fpsrMask = Fpsr.None,
            FpSkips fpSkips = FpSkips.None,
            FpTolerances fpTolerances = FpTolerances.None)
        {
            if (fpSkips != FpSkips.None)
            {
                ManageFpSkips(fpSkips);
            }

            Assert.That(_context.GetX(0), Is.EqualTo(_dynarmic.Reg[0]), "R0");
            Assert.That(_context.GetX(1), Is.EqualTo(_dynarmic.Reg[1]), "R1");
            Assert.That(_context.GetX(2), Is.EqualTo(_dynarmic.Reg[2]), "R2");
            Assert.That(_context.GetX(3), Is.EqualTo(_dynarmic.Reg[3]), "R3");
            Assert.That(_context.GetX(4), Is.EqualTo(_dynarmic.Reg[4]));
            Assert.That(_context.GetX(5), Is.EqualTo(_dynarmic.Reg[5]));
            Assert.That(_context.GetX(6), Is.EqualTo(_dynarmic.Reg[6]));
            Assert.That(_context.GetX(7), Is.EqualTo(_dynarmic.Reg[7]));
            Assert.That(_context.GetX(8), Is.EqualTo(_dynarmic.Reg[8]));
            Assert.That(_context.GetX(9), Is.EqualTo(_dynarmic.Reg[9]));
            Assert.That(_context.GetX(10), Is.EqualTo(_dynarmic.Reg[10]));
            Assert.That(_context.GetX(11), Is.EqualTo(_dynarmic.Reg[11]));
            Assert.That(_context.GetX(12), Is.EqualTo(_dynarmic.Reg[12]));
            Assert.That(_context.GetX(13), Is.EqualTo(_dynarmic.Reg[13  ]), "SP");
            Assert.That(_context.GetX(14), Is.EqualTo(_dynarmic.Reg[14]));

            if (fpTolerances == FpTolerances.None)
            {
                Assert.That(_context.GetV(0), Is.EqualTo(GetDynarmicQ(0)), "V0");
            }
            else
            {
                ManageFpTolerances(fpTolerances);
            }

            Assert.That(_context.GetV(1), Is.EqualTo(GetDynarmicQ(1)), "V1");
            Assert.That(_context.GetV(2), Is.EqualTo(GetDynarmicQ(2)), "V2");
            Assert.That(_context.GetV(3), Is.EqualTo(GetDynarmicQ(3)), "V3");
            Assert.That(_context.GetV(4), Is.EqualTo(GetDynarmicQ(4)), "V4");
            Assert.That(_context.GetV(5), Is.EqualTo(GetDynarmicQ(5)), "V5");
            Assert.That(_context.GetV(6), Is.EqualTo(GetDynarmicQ(6)));
            Assert.That(_context.GetV(7), Is.EqualTo(GetDynarmicQ(7)));
            Assert.That(_context.GetV(8), Is.EqualTo(GetDynarmicQ(8)));
            Assert.That(_context.GetV(9), Is.EqualTo(GetDynarmicQ(9)));
            Assert.That(_context.GetV(10), Is.EqualTo(GetDynarmicQ(10)));
            Assert.That(_context.GetV(11), Is.EqualTo(GetDynarmicQ(11)));
            Assert.That(_context.GetV(12), Is.EqualTo(GetDynarmicQ(12)));
            Assert.That(_context.GetV(13), Is.EqualTo(GetDynarmicQ(13)));
            Assert.That(_context.GetV(14), Is.EqualTo(GetDynarmicQ(14)), "V14");
            Assert.That(_context.GetV(15), Is.EqualTo(GetDynarmicQ(15)), "V15");

            Assert.Multiple(() =>
            {
                Assert.That(_context.GetPstateFlag(PState.QFlag), Is.EqualTo((_dynarmic.Cpsr & (1 << (int)PState.QFlag)) != 0), "QFlag");
                Assert.That(_context.GetPstateFlag(PState.VFlag), Is.EqualTo((_dynarmic.Cpsr & (1 << (int)PState.VFlag)) != 0), "VFlag");
                Assert.That(_context.GetPstateFlag(PState.CFlag), Is.EqualTo((_dynarmic.Cpsr & (1 << (int)PState.CFlag)) != 0), "CFlag");
                Assert.That(_context.GetPstateFlag(PState.ZFlag), Is.EqualTo((_dynarmic.Cpsr & (1 << (int)PState.ZFlag)) != 0), "ZFlag");
                Assert.That(_context.GetPstateFlag(PState.NFlag), Is.EqualTo((_dynarmic.Cpsr & (1 << (int)PState.NFlag)) != 0), "NFlag");
            });

            Assert.That((int)GetFpscr() & (int)fpsrMask, Is.EqualTo((int)_dynarmic.Fpscr & (int)fpsrMask), "Fpscr");

            if (_usingMemory)
            {
                byte[] mem = _memory.GetSpan(DataBaseAddress, (int)Size).ToArray();
                byte[] dynarmicMem = new Span<byte>(_dynarmicMemory, (int)DataBaseAddress, (int)Size).ToArray();

                Assert.That(mem, Is.EqualTo(dynarmicMem), "Data");
            }
        }

        private void ManageFpSkips(FpSkips fpSkips)
        {
            if (fpSkips.HasFlag(FpSkips.IfNaNS))
            {
                if (float.IsNaN(GetDynarmicQ(0).As<float>()))
                {
                    Assert.Ignore("NaN test.");
                }
            }
            else if (fpSkips.HasFlag(FpSkips.IfNaND))
            {
                if (double.IsNaN(GetDynarmicQ(0).As<double>()))
                {
                    Assert.Ignore("NaN test.");
                }
            }

            if (fpSkips.HasFlag(FpSkips.IfUnderflow))
            {
                if ((_dynarmic.Fpscr & (int)Fpsr.Ufc) != 0)
                {
                    Assert.Ignore("Underflow test.");
                }
            }

            if (fpSkips.HasFlag(FpSkips.IfOverflow))
            {
                if ((_dynarmic.Fpscr & (int)Fpsr.Ofc) != 0)
                {
                    Assert.Ignore("Overflow test.");
                }
            }
        }

        private void ManageFpTolerances(FpTolerances fpTolerances)
        {
            bool IsNormalOrSubnormalS(float f) => float.IsNormal(f) || float.IsSubnormal(f);
            bool IsNormalOrSubnormalD(double d) => double.IsNormal(d) || double.IsSubnormal(d);

            if (!Is.EqualTo(GetDynarmicQ(0)).ApplyTo(_context.GetV(0)).IsSuccess)
            {
                if (fpTolerances == FpTolerances.UpToOneUlpsS)
                {
                    if (IsNormalOrSubnormalS(GetDynarmicQ(0).As<float>()) &&
                        IsNormalOrSubnormalS(_context.GetV(0).As<float>()))
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(_context.GetV(0).Extract<float>(0),
                                Is.EqualTo(GetDynarmicQ(0).Extract<float>(0)).Within(1).Ulps, "V0[0]");
                            Assert.That(_context.GetV(0).Extract<float>(1),
                                Is.EqualTo(GetDynarmicQ(0).Extract<float>(1)).Within(1).Ulps, "V0[1]");
                            Assert.That(_context.GetV(0).Extract<float>(2),
                                Is.EqualTo(GetDynarmicQ(0).Extract<float>(2)).Within(1).Ulps, "V0[2]");
                            Assert.That(_context.GetV(0).Extract<float>(3),
                                Is.EqualTo(GetDynarmicQ(0).Extract<float>(3)).Within(1).Ulps, "V0[3]");
                        });

                        Console.WriteLine(fpTolerances);
                    }
                    else
                    {
                        Assert.That(_context.GetV(0), Is.EqualTo(GetDynarmicQ(0)));
                    }
                }

                if (fpTolerances == FpTolerances.UpToOneUlpsD)
                {
                    if (IsNormalOrSubnormalD(GetDynarmicQ(0).As<double>()) &&
                        IsNormalOrSubnormalD(_context.GetV(0).As<double>()))
                    {
                        Assert.Multiple(() =>
                        {
                            Assert.That(_context.GetV(0).Extract<double>(0),
                                Is.EqualTo(GetDynarmicQ(0).Extract<double>(0)).Within(1).Ulps, "V0[0]");
                            Assert.That(_context.GetV(0).Extract<double>(1),
                                Is.EqualTo(GetDynarmicQ(0).Extract<double>(1)).Within(1).Ulps, "V0[1]");
                        });

                        Console.WriteLine(fpTolerances);
                    }
                    else
                    {
                        Assert.That(_context.GetV(0), Is.EqualTo(GetDynarmicQ(0)));
                    }
                }
            }
        }

        protected static V128 MakeVectorScalar(float value) => new V128(value);
        protected static V128 MakeVectorScalar(double value) => new V128(value);

        protected static V128 MakeVectorE0(ulong e0) => new V128(e0, 0);
        protected static V128 MakeVectorE1(ulong e1) => new V128(0, e1);

        protected static V128 MakeVectorE0E1(ulong e0, ulong e1) => new V128(e0, e1);

        protected static V128 MakeVectorE0E1E2E3(uint e0, uint e1, uint e2, uint e3)
        {
            return new V128(e0, e1, e2, e3);
        }

        protected static ulong GetVectorE0(V128 vector) => vector.Extract<ulong>(0);
        protected static ulong GetVectorE1(V128 vector) => vector.Extract<ulong>(1);

        protected static ushort GenNormalH()
        {
            uint rnd;

            do rnd = TestContext.CurrentContext.Random.NextUShort();
            while ((rnd & 0x7C00u) == 0u ||
                   (~rnd & 0x7C00u) == 0u);

            return (ushort)rnd;
        }

        protected static ushort GenSubnormalH()
        {
            uint rnd;

            do rnd = TestContext.CurrentContext.Random.NextUShort();
            while ((rnd & 0x03FFu) == 0u);

            return (ushort)(rnd & 0x83FFu);
        }

        protected static uint GenNormalS()
        {
            uint rnd;

            do rnd = TestContext.CurrentContext.Random.NextUInt();
            while ((rnd & 0x7F800000u) == 0u ||
                   (~rnd & 0x7F800000u) == 0u);

            return rnd;
        }

        protected static uint GenSubnormalS()
        {
            uint rnd;

            do rnd = TestContext.CurrentContext.Random.NextUInt();
            while ((rnd & 0x007FFFFFu) == 0u);

            return rnd & 0x807FFFFFu;
        }

        protected static ulong GenNormalD()
        {
            ulong rnd;

            do rnd = TestContext.CurrentContext.Random.NextULong();
            while ((rnd & 0x7FF0000000000000ul) == 0ul ||
                   (~rnd & 0x7FF0000000000000ul) == 0ul);

            return rnd;
        }

        protected static ulong GenSubnormalD()
        {
            ulong rnd;

            do rnd = TestContext.CurrentContext.Random.NextULong();
            while ((rnd & 0x000FFFFFFFFFFFFFul) == 0ul);

            return rnd & 0x800FFFFFFFFFFFFFul;
        }

        private V128 GetDynarmicQ(int index)
        {
            return new V128(
                _dynarmic.ExtReg[index * 4 + 0],
                _dynarmic.ExtReg[index * 4 + 1],
                _dynarmic.ExtReg[index * 4 + 2],
                _dynarmic.ExtReg[index * 4 + 3]);
        }

        private uint GetFpscr()
        {
            uint fpscr = (uint)(_context.Fpsr & FPSR.A32Mask & ~FPSR.Nzcv) | (uint)(_context.Fpcr & FPCR.A32Mask);

            fpscr |= _context.GetFPstateFlag(FPState.NFlag) ? (1u << (int)FPState.NFlag) : 0;
            fpscr |= _context.GetFPstateFlag(FPState.ZFlag) ? (1u << (int)FPState.ZFlag) : 0;
            fpscr |= _context.GetFPstateFlag(FPState.CFlag) ? (1u << (int)FPState.CFlag) : 0;
            fpscr |= _context.GetFPstateFlag(FPState.VFlag) ? (1u << (int)FPState.VFlag) : 0;

            return fpscr;
        }

        private void SetFpscr(uint fpscr)
        {
            _context.Fpsr = FPSR.A32Mask & (FPSR)fpscr;
            _context.Fpcr = FPCR.A32Mask & (FPCR)fpscr;

            _context.SetFPstateFlag(FPState.NFlag, (fpscr & (1u << (int)FPState.NFlag)) != 0);
            _context.SetFPstateFlag(FPState.ZFlag, (fpscr & (1u << (int)FPState.ZFlag)) != 0);
            _context.SetFPstateFlag(FPState.CFlag, (fpscr & (1u << (int)FPState.CFlag)) != 0);
            _context.SetFPstateFlag(FPState.VFlag, (fpscr & (1u << (int)FPState.VFlag)) != 0);
        }
    }
}
