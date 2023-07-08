#define Bf32

using System;
using Xunit;

namespace Ryujinx.Tests.Cpu
{
    [Collection("Bf32")]
    public sealed class CpuTestBf32 : CpuTest32
    {
#if Bf32
        private const int RndCnt = 2;

        private static readonly uint[] _testData_rd =
        {
            0u, 0xdu,
        };
        private static readonly uint[] _testData_wd =
        {
            0x00000000u, 0x7FFFFFFFu,
            0x80000000u, 0xFFFFFFFFu,
        };
        private static readonly uint[] _testData_lsb =
        {
            0u, 15u, 16u, 31u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint> TestData_Bfc = new(_testData_rd, _testData_wd, _testData_lsb, _testData_lsb);

        [Theory(DisplayName = "BFC <Rd>, #<lsb>, #<width>")]
        [MemberData(nameof(TestData_Bfc))]
        public void Bfc(uint rd, uint wd, uint lsb, uint msb)
        {
            msb = Math.Max(lsb, msb); // Don't test unpredictable for now.
            uint opcode = 0xe7c0001fu; // BFC R0, #0, #1
            opcode |= ((rd & 0xf) << 12);
            opcode |= ((msb & 31) << 16) | ((lsb & 31) << 7);

            uint sp = Random.Shared.NextUInt();

            SingleOpcode(opcode, r0: wd, sp: sp);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_rn =
        {
            1u, 0xdu,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint> TestData_Bfi = new(_testData_rd, _testData_rn, Random.Shared.NextUIntEnumerable(RndCnt), _testData_wd, _testData_lsb, _testData_lsb);

        [Theory(DisplayName = "BFI <Rd>, <Rn>, #<lsb>, #<width>")]
        [MemberData(nameof(TestData_Bfi))]
        public void Bfi(uint rd, uint rn, uint wd, uint wn, uint lsb, uint msb)
        {
            msb = Math.Max(lsb, msb); // Don't test unpredictable for now.
            uint opcode = 0xe7c00010u; // BFI R0, R0, #0, #1
            opcode |= ((rd & 0xf) << 12);
            opcode |= ((rn & 0xf) << 0);
            opcode |= ((msb & 31) << 16) | ((lsb & 31) << 7);

            uint sp = Random.Shared.NextUInt();

            SingleOpcode(opcode, r0: wd, r1: wn, sp: sp);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "UBFX <Rd>, <Rn>, #<lsb>, #<width>")]
        [MemberData(nameof(TestData_Bfi))]
        public void Ubfx(uint rd, uint rn, uint wd, uint wn, uint lsb, uint widthm1)
        {
            if (lsb + widthm1 > 31)
            {
                widthm1 -= (lsb + widthm1) - 31;
            }
            uint opcode = 0xe7e00050u; // UBFX R0, R0, #0, #1
            opcode |= ((rd & 0xf) << 12);
            opcode |= ((rn & 0xf) << 0);
            opcode |= ((widthm1 & 31) << 16) | ((lsb & 31) << 7);

            uint sp = Random.Shared.NextUInt();

            SingleOpcode(opcode, r0: wd, r1: wn, sp: sp);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "SBFX <Rd>, <Rn>, #<lsb>, #<width>")]
        [MemberData(nameof(TestData_Bfi))]
        public void Sbfx(uint rd, uint rn, uint wd, uint wn, uint lsb, uint widthm1)
        {
            if (lsb + widthm1 > 31)
            {
                widthm1 -= (lsb + widthm1) - 31;
            }
            uint opcode = 0xe7a00050u; // SBFX R0, R0, #0, #1
            opcode |= ((rd & 0xf) << 12);
            opcode |= ((rn & 0xf) << 0);
            opcode |= ((widthm1 & 31) << 16) | ((lsb & 31) << 7);

            uint sp = Random.Shared.NextUInt();

            SingleOpcode(opcode, r0: wd, r1: wn, sp: sp);

            CompareAgainstUnicorn();
        }
#endif
    }
}
