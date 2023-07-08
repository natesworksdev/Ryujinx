#define AluBinary

using ARMeilleure.State;
using System;
using Xunit;

namespace Ryujinx.Tests.Cpu
{
    [Collection("AluBinary")]
    public sealed class CpuTestAluBinary : CpuTest
    {
#if AluBinary
        public struct CrcTest
        {
            public uint Crc;
            public ulong Value;
            public bool C;

            public uint[] Results; // One result for each CRC variant (8, 16, 32)

            public CrcTest(uint crc, ulong value, bool c, params uint[] results)
            {
                Crc = crc;
                Value = value;
                C = c;
                Results = results;
            }
        }

        #region "ValueSource (CRC32)"
        private static CrcTest[] _CRC32_Test_Values_()
        {
            // Created with http://www.sunshine2k.de/coding/javascript/crc/crc_js.html, with:
            //  - non-reflected polynomials
            //  - input reflected, result reflected
            //  - bytes in order of increasing significance
            //  - xor 0
            // Only includes non-C variant, as the other can be tested with unicorn.

            return new[]
            {
                new CrcTest(0x00000000u, 0x00_00_00_00_00_00_00_00u, false, 0x00000000, 0x00000000, 0x00000000, 0x00000000),
                new CrcTest(0x00000000u, 0x7f_ff_ff_ff_ff_ff_ff_ffu, false, 0x2d02ef8d, 0xbe2612ff, 0xdebb20e3, 0xa9de8355),
                new CrcTest(0x00000000u, 0x80_00_00_00_00_00_00_00u, false, 0x00000000, 0x00000000, 0x00000000, 0xedb88320),
                new CrcTest(0x00000000u, 0xff_ff_ff_ff_ff_ff_ff_ffu, false, 0x2d02ef8d, 0xbe2612ff, 0xdebb20e3, 0x44660075),
                new CrcTest(0x00000000u, 0xa0_02_f1_ca_52_78_8c_1cu, false, 0x14015c4f, 0x02799256, 0x9063c9e5, 0x8816610a),

                new CrcTest(0xffffffffu, 0x00_00_00_00_00_00_00_00u, false, 0x2dfd1072, 0xbe26ed00, 0xdebb20e3, 0x9add2096),
                new CrcTest(0xffffffffu, 0x7f_ff_ff_ff_ff_ff_ff_ffu, false, 0x00ffffff, 0x0000ffff, 0x00000000, 0x3303a3c3),
                new CrcTest(0xffffffffu, 0x80_00_00_00_00_00_00_00u, false, 0x2dfd1072, 0xbe26ed00, 0xdebb20e3, 0x7765a3b6),
                new CrcTest(0xffffffffu, 0xff_ff_ff_ff_ff_ff_ff_ffu, false, 0x00ffffff, 0x0000ffff, 0x00000000, 0xdebb20e3),
                new CrcTest(0xffffffffu, 0xa0_02_f1_ca_52_78_8c_1cu, false, 0x39fc4c3d, 0xbc5f7f56, 0x4ed8e906, 0x12cb419c),
            };
        }
        #endregion

        private static readonly ulong[] _testData_xn =
        {
            0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
            0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul,
        };
        private static readonly uint[] _testData_wn =
        {
            0x00000000u, 0x7FFFFFFFu,
            0x80000000u, 0xFFFFFFFFu,
        };

        private static readonly uint[] _testData_Crc32_rd =
        {
            0u,
        };
        private static readonly uint[] _testData_Crc32_rn =
        {
            1u,
        };
        private static readonly uint[] _testData_Crc32_rm =
        {
            2u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, CrcTest> TestData_Crc32 = new(_testData_Crc32_rd, _testData_Crc32_rn, _testData_Crc32_rm, RangeUtils.RangeData(0u, 3u, 1u), _CRC32_Test_Values_());

        [Theory]
        [MemberData(nameof(TestData_Crc32))]
        public void Crc32_b_h_w_x(uint rd, uint rn, uint rm, uint size, CrcTest test)
        {
            uint opcode = 0x1AC04000; // CRC32B W0, W0, W0

            opcode |= size << 10;
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            if (size == 3)
            {
                opcode |= 0x80000000;
            }

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: test.Crc, x2: test.Value, x31: w31, runUnicorn: false);

            ExecutionContext context = GetContext();
            ulong result = context.GetX((int)rd);
            Assert.True(result == test.Results[size]);
        }

        private static readonly uint[] _testData_Crc32x_rd =
        {
            0u, 31u,
        };
        private static readonly uint[] _testData_Crc32x_rn =
        {
            1u, 31u,
        };
        private static readonly uint[] _testData_Crc32x_rm =
        {
            2u, 31u,
        };

        private static readonly uint[] _testData_Crc32x_wn =
        {
            0x00000000u, 0xFFFFFFFFu,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, ulong> TestData_Crc32x = new(_testData_Crc32x_rd, _testData_Crc32x_rn, _testData_Crc32x_rm, _testData_Crc32x_wn, _testData_xn);

        [Theory(DisplayName = "CRC32X <Wd>, <Wn>, <Xm>", Skip = "Unicorn fails.")]
        [MemberData(nameof(TestData_Crc32x))]
        public void Crc32x(uint rd, uint rn, uint rm, uint wn, ulong xm)
        {
            uint opcode = 0x9AC04C00; // CRC32X W0, W0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: xm, x31: w31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint> TestData_Crc32w = new(_testData_Crc32x_rd, _testData_Crc32x_rn, _testData_Crc32x_rm, _testData_Crc32x_wn, _testData_wn);

        [Theory(DisplayName = "CRC32W <Wd>, <Wn>, <Wm>", Skip = "Unicorn fails.")]
        [MemberData(nameof(TestData_Crc32w))]
        public void Crc32w(uint rd, uint rn, uint rm, uint wn, uint wm)
        {
            uint opcode = 0x1AC04800; // CRC32W W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        private static readonly ushort[] _testData_Crc32h_wm =
        {
            0x00_00, 0x7F_FF,
            0x80_00, 0xFF_FF,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, ushort> TestData_Crc32h = new(_testData_Crc32x_rd, _testData_Crc32x_rn, _testData_Crc32x_rm, _testData_Crc32x_wn, _testData_Crc32h_wm);

        [Theory(DisplayName = "CRC32H <Wd>, <Wn>, <Wm>", Skip = "Unicorn fails.")]
        [MemberData(nameof(TestData_Crc32h))]
        public void Crc32h(uint rd, uint rn, uint rm, uint wn, ushort wm)
        {
            uint opcode = 0x1AC04400; // CRC32H W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        private static readonly byte[] _testData_Crc32b_wm =
        {
            0x00, 0x7F,
            0x80, 0xFF,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, byte> TestData_Crc32b = new(_testData_Crc32x_rd, _testData_Crc32x_rn, _testData_Crc32x_rm, _testData_Crc32x_wn, _testData_Crc32b_wm);

        [Theory(DisplayName = "CRC32B <Wd>, <Wn>, <Wm>", Skip = "Unicorn fails.")]
        [MemberData(nameof(TestData_Crc32b))]
        public void Crc32b(uint rd, uint rn, uint rm, uint wn, byte wm)
        {
            uint opcode = 0x1AC04000; // CRC32B W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "CRC32CX <Wd>, <Wn>, <Xm>")]
        [MemberData(nameof(TestData_Crc32x))]
        public void Crc32cx(uint rd, uint rn, uint rm, uint wn, ulong xm)
        {
            uint opcode = 0x9AC05C00; // CRC32CX W0, W0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: xm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "CRC32CW <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Crc32w))]
        public void Crc32cw(uint rd, uint rn, uint rm, uint wn, uint wm)
        {
            uint opcode = 0x1AC05800; // CRC32CW W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "CRC32CH <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Crc32h))]
        public void Crc32ch(uint rd, uint rn, uint rm, uint wn, ushort wm)
        {
            uint opcode = 0x1AC05400; // CRC32CH W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "CRC32CB <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Crc32b))]
        public void Crc32cb(uint rd, uint rn, uint rm, uint wn, byte wm)
        {
            uint opcode = 0x1AC05000; // CRC32CB W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, ulong, ulong> TestData_Sdiv64 = new(_testData_Crc32x_rd, _testData_Crc32x_rn, _testData_Crc32x_rm, _testData_xn, _testData_xn);

        [Theory(DisplayName = "SDIV <Xd>, <Xn>, <Xm>")]
        [MemberData(nameof(TestData_Sdiv64))]
        public void Sdiv_64bit(uint rd, uint rn, uint rm, ulong xn, ulong xm)
        {
            uint opcode = 0x9AC00C00; // SDIV X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint> TestData_Sdiv32 = new(_testData_Crc32x_rd, _testData_Crc32x_rn, _testData_Crc32x_rm, _testData_wn, _testData_wn);

        [Theory(DisplayName = "SDIV <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Sdiv32))]
        public void Sdiv_32bit(uint rd, uint rn, uint rm, uint wn, uint wm)
        {
            uint opcode = 0x1AC00C00; // SDIV W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "UDIV <Xd>, <Xn>, <Xm>")]
        [MemberData(nameof(TestData_Sdiv64))]
        public void Udiv_64bit(uint rd, uint rn, uint rm, ulong xn, ulong xm)
        {
            uint opcode = 0x9AC00800; // UDIV X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "UDIV <Wd>, <Wn>, <Wm>")]
        [MemberData(nameof(TestData_Sdiv32))]
        public void Udiv_32bit(uint rd, uint rn, uint rm, uint wn, uint wm)
        {
            uint opcode = 0x1AC00800; // UDIV W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }
#endif
    }
}
