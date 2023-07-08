#define AluImm

using System;
using Xunit;

namespace Ryujinx.Tests.Cpu
{
    [Collection("AluImm")]
    public sealed class CpuTestAluImm : CpuTest
    {
#if AluImm

        private static readonly uint[] _testData_rd =
        {
            0u, 31u,
        };
        private static readonly uint[] _testData_rn =
        {
            1u, 31u,
        };
        private static readonly ulong[] _testData_xnSp =
        {
            0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
            0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul,
        };
        private static readonly uint[] _testData_imm =
        {
            0u, 4095u,
        };
        private static readonly uint[] _testData_shift =
        {
            0b00u, 0b01u, // <LSL #0, LSL #12>
        };
        private static readonly uint[] _testData_wnWsp =
        {
            0x00000000u, 0x7FFFFFFFu,
            0x80000000u, 0xFFFFFFFFu,
        };

        public static readonly MatrixTheoryData<uint, uint, ulong, uint, uint> TestData_64 = new(_testData_rd, _testData_rn, _testData_xnSp, _testData_imm, _testData_shift);
        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint> TestData_32 = new(_testData_rd, _testData_rn, _testData_wnWsp, _testData_imm, _testData_shift);

        [Theory(DisplayName = "ADD <Xd|SP>, <Xn|SP>, #<imm>{, <shift>}")]
        [MemberData(nameof(TestData_64))]
        public void Add_64bit(uint rd, uint rn, ulong xnSp, uint imm, uint shift)
        {
            uint opcode = 0x91000000; // ADD X0, X0, #0, LSL #0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((imm & 4095) << 10);

            if (rn != 31)
            {
                SingleOpcode(opcode, x1: xnSp);
            }
            else
            {
                SingleOpcode(opcode, x31: xnSp);
            }

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ADD <Wd|WSP>, <Wn|WSP>, #<imm>{, <shift>}")]
        [MemberData(nameof(TestData_32))]
        public void Add_32bit(uint rd, uint rn, uint wnWsp, uint imm, uint shift)
        {
            uint opcode = 0x11000000; // ADD W0, W0, #0, LSL #0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((imm & 4095) << 10);

            if (rn != 31)
            {
                SingleOpcode(opcode, x1: wnWsp);
            }
            else
            {
                SingleOpcode(opcode, x31: wnWsp);
            }

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ADDS <Xd>, <Xn|SP>, #<imm>{, <shift>}")]
        [MemberData(nameof(TestData_64))]
        public void Adds_64bit(uint rd, uint rn, ulong xnSp, uint imm, uint shift)
        {
            uint opcode = 0xB1000000; // ADDS X0, X0, #0, LSL #0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((imm & 4095) << 10);

            if (rn != 31)
            {
                SingleOpcode(opcode, x1: xnSp);
            }
            else
            {
                SingleOpcode(opcode, x31: xnSp);
            }

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ADDS <Wd>, <Wn|WSP>, #<imm>{, <shift>}")]
        [MemberData(nameof(TestData_32))]
        public void Adds_32bit(uint rd, uint rn, uint wnWsp, uint imm, uint shift)
        {
            uint opcode = 0x31000000; // ADDS W0, W0, #0, LSL #0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((imm & 4095) << 10);

            if (rn != 31)
            {
                SingleOpcode(opcode, x1: wnWsp);
            }
            else
            {
                SingleOpcode(opcode, x31: wnWsp);
            }

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_N1_imms =
        {
            0u, 31u, 32u, 62u, // <imm>
        };
        private static readonly uint[] _testData_N1_immr =
        {
            0u, 31u, 32u, 63u, // <imm>
        };

        public static readonly MatrixTheoryData<uint, uint, ulong, uint, uint> TestData_N1 = new(_testData_rd, _testData_rn, _testData_xnSp, _testData_N1_imms, _testData_N1_immr);

        [Theory(DisplayName = "AND <Xd|SP>, <Xn>, #<imm>")]
        [MemberData(nameof(TestData_N1))]
        public void And_N1_64bit(uint rd, uint rn, ulong xn, uint imms, uint immr)
        {
            uint opcode = 0x92400000; // AND X0, X0, #0x1
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        private static uint[] _testData_N0_imms =
        {
            0u, 15u, 16u, 30u, // <imm>
        };
        private static uint[] _testData_N0_immr =
        {
            0u, 15u, 16u, 31u, // <imm>
        };

        public static readonly MatrixTheoryData<uint, uint, ulong, uint, uint> TestData_N0 = new(_testData_rd, _testData_rn, _testData_xnSp, _testData_N0_imms, _testData_N0_immr);

        [Theory(DisplayName = "AND <Xd|SP>, <Xn>, #<imm>")]
        [MemberData(nameof(TestData_N0))]
        public void And_N0_64bit(uint rd, uint rn, ulong xn, uint imms, uint immr)
        {
            uint opcode = 0x92000000; // AND X0, X0, #0x100000001
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint> TestData_Add32 = new(_testData_rd, _testData_rd, _testData_wnWsp, _testData_N0_imms, _testData_N0_immr);

        [Theory(DisplayName = "AND <Wd|WSP>, <Wn>, #<imm>")]
        [MemberData(nameof(TestData_Add32))]
        public void And_32bit(uint rd, uint rn, uint wn, uint imms, uint immr)
        {
            uint opcode = 0x12000000; // AND W0, W0, #0x1
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ANDS <Xd>, <Xn>, #<imm>")]
        [MemberData(nameof(TestData_N1))]
        public void Ands_N1_64bit(uint rd, uint rn, ulong xn, uint imms, uint immr)
        {
            uint opcode = 0xF2400000; // ANDS X0, X0, #0x1
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ANDS <Xd>, <Xn>, #<imm>")]
        [MemberData(nameof(TestData_N1))]
        public void Ands_N0_64bit(uint rd, uint rn, ulong xn, uint imms, uint immr)
        {
            uint opcode = 0xF2000000; // ANDS X0, X0, #0x100000001
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ANDS <Wd>, <Wn>, #<imm>")]
        [MemberData(nameof(TestData_Add32))]
        public void Ands_32bit(uint rd, uint rn, uint wn, uint imms, uint immr)
        {
            uint opcode = 0x72000000; // ANDS W0, W0, #0x1
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "EOR <Xd|SP>, <Xn>, #<imm>")]
        [MemberData(nameof(TestData_N1))]
        public void Eor_N1_64bit(uint rd, uint rn, ulong xn, uint imms, uint immr)
        {
            uint opcode = 0xD2400000; // EOR X0, X0, #0x1
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "EOR <Xd|SP>, <Xn>, #<imm>")]
        [MemberData(nameof(TestData_N0))]
        public void Eor_N0_64bit(uint rd, uint rn, ulong xn, uint imms, uint immr)
        {
            uint opcode = 0xD2000000; // EOR X0, X0, #0x100000001
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "EOR <Wd>, <Wn>, #<imm>")]
        [MemberData(nameof(TestData_Add32))]
        public void Eor_32bit(uint rd, uint rn, uint wn, uint imms, uint immr)
        {
            uint opcode = 0x52000000; // EOR W0, W0, #0x1
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ORR <Xd|SP>, <Xn>, #<imm>")]
        [MemberData(nameof(TestData_N1))]
        public void Orr_N1_64bit(uint rd, uint rn, ulong xn, uint imms, uint immr)
        {
            uint opcode = 0xB2400000; // ORR X0, X0, #0x1
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ORR <Xd|SP>, <Xn>, #<imm>")]
        [MemberData(nameof(TestData_N0))]
        public void Orr_N0_64bit(uint rd, uint rn, ulong xn, uint imms, uint immr)
        {
            uint opcode = 0xB2000000; // ORR X0, X0, #0x100000001
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ORR <Wd|WSP>, <Wn>, #<imm>")]
        [MemberData(nameof(TestData_Add32))]
        public void Orr_32bit(uint rd, uint rn, uint wn, uint imms, uint immr)
        {
            uint opcode = 0x32000000; // ORR W0, W0, #0x1
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((immr & 63) << 16) | ((imms & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x31: w31);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_Sub_imm =
        {
            0u, 4095u,
        };
        private static readonly uint[] _testData_Sub_shift =
        {
            0b00u, 0b01u, // <LSL #0, LSL #12>
        };

        public static readonly MatrixTheoryData<uint, uint, ulong, uint, uint> TestData_Sub64 = new(_testData_rd, _testData_rn, _testData_xnSp, _testData_Sub_imm, _testData_Sub_shift);

        [Theory(DisplayName = "SUB <Xd|SP>, <Xn|SP>, #<imm>{, <shift>}")]
        [MemberData(nameof(TestData_Sub64))]
        public void Sub_64bit(uint rd, uint rn, ulong xnSp, uint imm, uint shift)
        {
            uint opcode = 0xD1000000; // SUB X0, X0, #0, LSL #0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((imm & 4095) << 10);

            if (rn != 31)
            {
                SingleOpcode(opcode, x1: xnSp);
            }
            else
            {
                SingleOpcode(opcode, x31: xnSp);
            }

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint> TestData_Sub32 = new(_testData_rd, _testData_rn, _testData_wnWsp, _testData_Sub_imm, _testData_Sub_shift);

        [Theory(DisplayName = "SUB <Wd|WSP>, <Wn|WSP>, #<imm>{, <shift>}")]
        [MemberData(nameof(TestData_Sub32))]
        public void Sub_32bit(uint rd, uint rn, uint wnWsp, uint imm, uint shift)
        {
            uint opcode = 0x51000000; // SUB W0, W0, #0, LSL #0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((imm & 4095) << 10);

            if (rn != 31)
            {
                SingleOpcode(opcode, x1: wnWsp);
            }
            else
            {
                SingleOpcode(opcode, x31: wnWsp);
            }

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "SUBS <Xd>, <Xn|SP>, #<imm>{, <shift>}")]
        [MemberData(nameof(TestData_Sub64))]
        public void Subs_64bit(uint rd, uint rn, ulong xnSp, uint imm, uint shift)
        {
            uint opcode = 0xF1000000; // SUBS X0, X0, #0, LSL #0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((imm & 4095) << 10);

            if (rn != 31)
            {
                SingleOpcode(opcode, x1: xnSp);
            }
            else
            {
                SingleOpcode(opcode, x31: xnSp);
            }

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "SUBS <Wd>, <Wn|WSP>, #<imm>{, <shift>}")]
        [MemberData(nameof(TestData_Sub32))]
        public void Subs_32bit(uint rd, uint rn, uint wnWsp, uint imm, uint shift)
        {
            uint opcode = 0x71000000; // SUBS W0, W0, #0, LSL #0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((imm & 4095) << 10);

            if (rn != 31)
            {
                SingleOpcode(opcode, x1: wnWsp);
            }
            else
            {
                SingleOpcode(opcode, x31: wnWsp);
            }

            CompareAgainstUnicorn();
        }
#endif
    }
}
