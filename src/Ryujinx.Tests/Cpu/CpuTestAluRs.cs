#define AluRs

using System;
using Xunit;

namespace Ryujinx.Tests.Cpu
{
    [Collection("AluRs")]
    public sealed class CpuTestAluRs : CpuTest
    {
#if AluRs

        private static readonly uint[] _testData_rd =
        {
            0u, 31u,
        };
        private static readonly uint[] _testData_rn =
        {
            1u, 31u,
        };
        private static readonly uint[] _testData_rm =
        {
            2u, 31u,
        };
        private static readonly ulong[] _testData_xn =
        {
            0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
            0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul,
        };
        private static readonly bool[] _testData_bools =
        {
            false,
            true,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, ulong, ulong, bool> TestData_Adc = new(_testData_rd, _testData_rn, _testData_rm, _testData_xn, _testData_xn, _testData_bools);

        [Theory(DisplayName = "ADC <Xd>, <Xn>, <Xm>")]
        [MemberData(nameof(TestData_Adc))]
        public void Adc_64bit(uint rd, uint rn, uint rm, ulong xn, ulong xm, bool carryIn)
        {
            uint opcode = 0x9A000000; // ADC X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31, carry: carryIn);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_wn = {
            0x00000000u, 0x7FFFFFFFu,
            0x80000000u, 0xFFFFFFFFu,
         };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, bool> TestData_Adc32 = new(_testData_rd, _testData_rn, _testData_rm, _testData_wn, _testData_wn, _testData_bools);

        [Theory(DisplayName = "ADC <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Adc32))]
        public void Adc_32bit(uint rd, uint rn, uint rm, uint wn, uint wm, bool carryIn)
        {
            uint opcode = 0x1A000000; // ADC W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31, carry: carryIn);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ADCS <Xd>, <Xn>, <Xm>")]
        [MemberData(nameof(TestData_Adc))]
        public void Adcs_64bit(uint rd, uint rn, uint rm, ulong xn, ulong xm, bool carryIn)
        {
            uint opcode = 0xBA000000; // ADCS X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31, carry: carryIn);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ADCS <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Adc32))]
        public void Adcs_32bit(uint rd, uint rn, uint rm, uint wn, uint wm, bool carryIn)
        {
            uint opcode = 0x3A000000; // ADCS W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31, carry: carryIn);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_shift =
        {
            0b00u, 0b01u, 0b10u, // <LSL, LSR, ASR>
        };
        private static readonly uint[] _testData_amount =
        {
            0u, 31u, 32u, 63u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, ulong, ulong, uint, uint> TestData_Add = new(_testData_rd, _testData_rn, _testData_rm, _testData_xn, _testData_xn, _testData_shift, _testData_amount);

        [Theory(DisplayName = "ADD <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_Add))]
        public void Add_64bit(uint rd, uint rn, uint rm, ulong xn, ulong xm, uint shift, uint amount)
        {
            uint opcode = 0x8B000000; // ADD X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint, uint> TestData_Add32 = new(_testData_rd, _testData_rn, _testData_rm, _testData_wn, _testData_wn, _testData_shift, _testData_amount);

        [Theory(DisplayName = "ADD <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_Add32))]
        public void Add_32bit(uint rd, uint rn, uint rm, uint wn, uint wm, uint shift, uint amount)
        {
            uint opcode = 0x0B000000; // ADD W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ADDS <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_Add))]
        public void Adds_64bit(uint rd, uint rn, uint rm, ulong xn, ulong xm, uint shift, uint amount)
        {
            uint opcode = 0xAB000000; // ADDS X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ADDS <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_Add32))]
        public void Adds_32bit(uint rd, uint rn, uint rm, uint wn, uint wm, uint shift, uint amount)
        {
            uint opcode = 0x2B000000; // ADDS W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_And_shift = {
            0b00u, 0b01u, 0b10u, 0b11u, // <LSL, LSR, ASR, ROR>
         };

        public static readonly MatrixTheoryData<uint, uint, uint, ulong, ulong, uint, uint> TestData_And = new(_testData_rd, _testData_rn, _testData_rm, _testData_xn, _testData_xn, _testData_And_shift, _testData_amount);

        [Theory(DisplayName = "AND <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And))]
        public void And_64bit(uint rd, uint rn, uint rm, ulong xn, ulong xm, uint shift, uint amount)
        {
            uint opcode = 0x8A000000; // AND X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint, uint> TestData_And32 = new(_testData_rd, _testData_rn, _testData_rm, _testData_wn, _testData_wn, _testData_And_shift, _testData_amount);

        [Theory(DisplayName = "AND <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And32))]
        public void And_32bit(uint rd, uint rn, uint rm, uint wn, uint wm, uint shift, uint amount)
        {
            uint opcode = 0x0A000000; // AND W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ANDS <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And))]
        public void Ands_64bit(uint rd, uint rn, uint rm, ulong xn, ulong xm, uint shift, uint amount)
        {
            uint opcode = 0xEA000000; // ANDS X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ANDS <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And32))]
        public void Ands_32bit(uint rd, uint rn, uint rm, uint wn, uint wm, uint shift, uint amount)
        {
            uint opcode = 0x6A000000; // ANDS W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        private static readonly ulong[] _testData_xm = {
            0ul, 31ul, 32ul, 63ul, 0x7FFFFFFFFFFFFFFFul,
            0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul,
         };

        public static readonly MatrixTheoryData<uint, uint, uint, ulong, ulong> TestData_Asrv = new(_testData_rd, _testData_rn, _testData_rm, _testData_xn, _testData_xm);

        [Theory(DisplayName = "ASRV <Xd>, <Xn>, <Xm>")]
        [MemberData(nameof(TestData_Asrv))]
        public void Asrv_64bit(uint rd, uint rn, uint rm, ulong xn, ulong xm)
        {
            uint opcode = 0x9AC02800; // ASRV X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_wm = {
            0u, 15u, 16u, 31u, 0x7FFFFFFFu,
            0x80000000u, 0xFFFFFFFFu,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint> TestData_Asrv32 = new(_testData_rd, _testData_rn, _testData_rm, _testData_wn, _testData_wm);

        [Theory(DisplayName = "ASRV <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Asrv32))]
        public void Asrv_32bit(uint rd, uint rn, uint rm, uint wn, uint wm)
        {
            uint opcode = 0x1AC02800; // ASRV W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "BIC <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And))]
        public void Bic_64bit(uint rd, uint rn, uint rm, ulong xn, ulong xm, uint shift, uint amount)
        {
            uint opcode = 0x8A200000; // BIC X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "BIC <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And32))]
        public void Bic_32bit(uint rd, uint rn, uint rm, uint wn, uint wm, uint shift, uint amount)
        {
            uint opcode = 0x0A200000; // BIC W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "BICS <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And))]
        public void Bics_64bit(uint rd, uint rn, uint rm, ulong xn, ulong xm, uint shift, uint amount)
        {
            uint opcode = 0xEA200000; // BICS X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "BICS <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And32))]
        public void Bics_32bit(uint rd,
                               uint rn,
                               uint rm,
                               uint wn,
                               uint wm,
                               uint shift, // <LSL, LSR, ASR, ROR>
                               uint amount)
        {
            uint opcode = 0x6A200000; // BICS W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "EON <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And))]
        public void Eon_64bit(uint rd,
                              uint rn,
                              uint rm,
                              ulong xn,
                              ulong xm,
                              uint shift, // <LSL, LSR, ASR, ROR>
                              uint amount)
        {
            uint opcode = 0xCA200000; // EON X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "EON <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And32))]
        public void Eon_32bit(uint rd,
                              uint rn,
                              uint rm,
                              uint wn,
                              uint wm,
                              uint shift, // <LSL, LSR, ASR, ROR>
                              uint amount)
        {
            uint opcode = 0x4A200000; // EON W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "EOR <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And))]
        public void Eor_64bit(uint rd,
                              uint rn,
                              uint rm,
                              ulong xn,
                              ulong xm,
                              uint shift, // <LSL, LSR, ASR, ROR>
                              uint amount)
        {
            uint opcode = 0xCA000000; // EOR X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "EOR <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And32))]
        public void Eor_32bit(uint rd,
                              uint rn,
                              uint rm,
                              uint wn,
                              uint wm,
                              uint shift, // <LSL, LSR, ASR, ROR>
                              uint amount)
        {
            uint opcode = 0x4A000000; // EOR W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, ulong, ulong, uint> TestData_Extr = new(_testData_rd, _testData_rn, _testData_rm, _testData_xn, _testData_xn, _testData_amount);

        [Theory(DisplayName = "EXTR <Xd>, <Xn>, <Xm>, #<lsb>")]
        [MemberData(nameof(TestData_Extr))]
        public void Extr_64bit(uint rd,
                               uint rn,
                               uint rm,
                               ulong xn,
                               ulong xm,
                               uint lsb)
        {
            uint opcode = 0x93C00000; // EXTR X0, X0, X0, #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((lsb & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint> TestData_Extr32 = new(_testData_rd, _testData_rn, _testData_rm, _testData_wn, _testData_wn, _testData_amount);

        [Theory(DisplayName = "EXTR <Wd>, <Wn>, <Wm>, #<lsb>")]
        [MemberData(nameof(TestData_Extr32))]
        public void Extr_32bit(uint rd,
                               uint rn,
                               uint rm,
                               uint wn,
                               uint wm,
                               uint lsb)
        {
            uint opcode = 0x13800000; // EXTR W0, W0, W0, #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((lsb & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "LSLV <Xd>, <Xn>, <Xm>")]
        [MemberData(nameof(TestData_Asrv))]
        public void Lslv_64bit(uint rd,
                               uint rn,
                               uint rm,
                               ulong xn,
                               ulong xm)
        {
            uint opcode = 0x9AC02000; // LSLV X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "LSLV <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Asrv32))]
        public void Lslv_32bit(uint rd,
                               uint rn,
                               uint rm,
                               uint wn,
                               uint wm)
        {
            uint opcode = 0x1AC02000; // LSLV W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "LSRV <Xd>, <Xn>, <Xm>")]
        [MemberData(nameof(TestData_Asrv))]
        public void Lsrv_64bit(uint rd,
                               uint rn,
                               uint rm,
                               ulong xn,
                               ulong xm)
        {
            uint opcode = 0x9AC02400; // LSRV X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "LSRV <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Asrv32))]
        public void Lsrv_32bit(uint rd,
                               uint rn,
                               uint rm,
                               uint wn,
                               uint wm)
        {
            uint opcode = 0x1AC02400; // LSRV W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ORN <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And))]
        public void Orn_64bit(uint rd,
                              uint rn,
                              uint rm,
                              ulong xn,
                              ulong xm,
                              uint shift, // <LSL, LSR, ASR, ROR>
                              uint amount)
        {
            uint opcode = 0xAA200000; // ORN X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ORN <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And32))]
        public void Orn_32bit(uint rd,
                              uint rn,
                              uint rm,
                              uint wn,
                              uint wm,
                              uint shift, // <LSL, LSR, ASR, ROR>
                              uint amount)
        {
            uint opcode = 0x2A200000; // ORN W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ORR <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And))]
        public void Orr_64bit(uint rd,
                              uint rn,
                              uint rm,
                              ulong xn,
                              ulong xm,
                              uint shift, // <LSL, LSR, ASR, ROR>
                              uint amount)
        {
            uint opcode = 0xAA000000; // ORR X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "ORR <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_And32))]
        public void Orr_32bit(uint rd,
                              uint rn,
                              uint rm,
                              uint wn,
                              uint wm,
                              uint shift, // <LSL, LSR, ASR, ROR>
                              uint amount)
        {
            uint opcode = 0x2A000000; // ORR W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "RORV <Xd>, <Xn>, <Xm>")]
        [MemberData(nameof(TestData_Asrv))]
        public void Rorv_64bit(uint rd,
                               uint rn,
                               uint rm,
                               ulong xn,
                               ulong xm)
        {
            uint opcode = 0x9AC02C00; // RORV X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "RORV <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Asrv32))]
        public void Rorv_32bit(uint rd,
                               uint rn,
                               uint rm,
                               uint wn,
                               uint wm)
        {
            uint opcode = 0x1AC02C00; // RORV W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "SBC <Xd>, <Xn>, <Xm>")]
        [MemberData(nameof(TestData_Adc))]
        public void Sbc_64bit(uint rd,
                              uint rn,
                              uint rm,
                              ulong xn,
                              ulong xm,
                              bool carryIn)
        {
            uint opcode = 0xDA000000; // SBC X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31, carry: carryIn);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "SBC <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Adc32))]
        public void Sbc_32bit(uint rd,
                              uint rn,
                              uint rm,
                              uint wn,
                              uint wm,
                              bool carryIn)
        {
            uint opcode = 0x5A000000; // SBC W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31, carry: carryIn);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "SBCS <Xd>, <Xn>, <Xm>")]
        [MemberData(nameof(TestData_Adc))]
        public void Sbcs_64bit(uint rd,
                               uint rn,
                               uint rm,
                               ulong xn,
                               ulong xm,
                               bool carryIn)
        {
            uint opcode = 0xFA000000; // SBCS X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31, carry: carryIn);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "SBCS <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Adc32))]
        public void Sbcs_32bit(uint rd,
                               uint rn,
                               uint rm,
                               uint wn,
                               uint wm,
                               bool carryIn)
        {
            uint opcode = 0x7A000000; // SBCS W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31, carry: carryIn);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "SUB <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_Add))]
        public void Sub_64bit(uint rd,
                              uint rn,
                              uint rm,
                              ulong xn,
                              ulong xm,
                              uint shift, // <LSL, LSR, ASR>
                              uint amount)
        {
            uint opcode = 0xCB000000; // SUB X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "SUB <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_Add32))]
        public void Sub_32bit(uint rd,
                              uint rn,
                              uint rm,
                              uint wn,
                              uint wm,
                              uint shift, // <LSL, LSR, ASR>
                              uint amount)
        {
            uint opcode = 0x4B000000; // SUB W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "SUBS <Xd>, <Xn>, <Xm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_Add))]
        public void Subs_64bit(uint rd,
                               uint rn,
                               uint rm,
                               ulong xn,
                               ulong xm,
                               uint shift, // <LSL, LSR, ASR>
                               uint amount)
        {
            uint opcode = 0xEB000000; // SUBS X0, X0, X0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "SUBS <Wd>, <Wn>, <Wm>{, <shift> #<amount>}")]
        [MemberData(nameof(TestData_Add32))]
        public void Subs_32bit(uint rd,
                               uint rn,
                               uint rm,
                               uint wn,
                               uint wm,
                               uint shift, // <LSL, LSR, ASR>
                               uint amount)
        {
            uint opcode = 0x6B000000; // SUBS W0, W0, W0, LSL #0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcode |= ((shift & 3) << 22) | ((amount & 63) << 10);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }
#endif
    }
}
