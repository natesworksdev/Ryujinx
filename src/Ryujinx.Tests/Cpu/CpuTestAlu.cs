#define Alu

using System;
using System.Collections.Generic;
using Xunit;

namespace Ryujinx.Tests.Cpu
{
    [Collection("Alu")]
    public sealed class CpuTestAlu : CpuTest
    {
#if Alu

        #region "Helper methods"
        private static uint GenLeadingSignsMinus32(int cnt) // 0 <= cnt <= 31
        {
            return ~GenLeadingZeros32(cnt + 1);
        }

        private static ulong GenLeadingSignsMinus64(int cnt) // 0 <= cnt <= 63
        {
            return ~GenLeadingZeros64(cnt + 1);
        }

        private static uint GenLeadingSignsPlus32(int cnt) // 0 <= cnt <= 31
        {
            return GenLeadingZeros32(cnt + 1);
        }

        private static ulong GenLeadingSignsPlus64(int cnt) // 0 <= cnt <= 63
        {
            return GenLeadingZeros64(cnt + 1);
        }

        private static uint GenLeadingZeros32(int cnt) // 0 <= cnt <= 32
        {
            if (cnt == 32)
            {
                return 0u;
            }

            if (cnt == 31)
            {
                return 1u;
            }

            uint rnd = Random.Shared.NextUInt();
            int mask = int.MinValue;

            return (rnd >> (cnt + 1)) | ((uint)mask >> cnt);
        }

        private static ulong GenLeadingZeros64(int cnt) // 0 <= cnt <= 64
        {
            if (cnt == 64)
            {
                return 0ul;
            }

            if (cnt == 63)
            {
                return 1ul;
            }

            ulong rnd = Random.Shared.NextULong();
            long mask = long.MinValue;

            return (rnd >> (cnt + 1)) | ((ulong)mask >> cnt);
        }
        #endregion

        #region "ValueSource (Types)"
        private static IEnumerable<ulong> GenLeadingSignsX()
        {
            for (int cnt = 0; cnt <= 63; cnt++)
            {
                yield return GenLeadingSignsMinus64(cnt);
                yield return GenLeadingSignsPlus64(cnt);
            }
        }

        private static IEnumerable<uint> GenLeadingSignsW()
        {
            for (int cnt = 0; cnt <= 31; cnt++)
            {
                yield return GenLeadingSignsMinus32(cnt);
                yield return GenLeadingSignsPlus32(cnt);
            }
        }

        private static IEnumerable<ulong> GenLeadingZerosX()
        {
            for (int cnt = 0; cnt <= 64; cnt++)
            {
                yield return GenLeadingZeros64(cnt);
            }
        }

        private static IEnumerable<uint> GenLeadingZerosW()
        {
            for (int cnt = 0; cnt <= 32; cnt++)
            {
                yield return GenLeadingZeros32(cnt);
            }
        }
        #endregion

        private static readonly uint[] _testData_rd = { 0u, 31u };
        private static readonly uint[] _testData_rn = { 1u, 31u };
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

        public static readonly MatrixTheoryData<uint, uint, ulong> TestData_CLS_x = new(_testData_rd, _testData_rn, GenLeadingSignsX());
        public static readonly MatrixTheoryData<uint, uint, uint> TestData_CLS_w = new(_testData_rd, _testData_rn, GenLeadingSignsW());
        public static readonly MatrixTheoryData<uint, uint, ulong> TestData_CLZ_x = new(_testData_rd, _testData_rn, GenLeadingZerosX());
        public static readonly MatrixTheoryData<uint, uint, uint> TestData_CLZ_w = new(_testData_rd, _testData_rn, GenLeadingZerosW());

        public static readonly MatrixTheoryData<uint, uint, ulong> TestData_64bit = new(_testData_rd, _testData_rn, _testData_xn);
        public static readonly MatrixTheoryData<uint, uint, uint> TestData_32bit = new(_testData_rd, _testData_rn, _testData_wn);

        [Theory(DisplayName = "CLS <Xd>, <Xn>")]
        [MemberData(nameof(TestData_CLS_x))]
        public void Cls_64bit(uint rd, uint rn, ulong xn)
        {
            uint opcode = 0xDAC01400; // CLS X0, X0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "CLS <Wd>, <Wn>")]
        [MemberData(nameof(TestData_CLS_w))]
        public void Cls_32bit(uint rd, uint rn, uint wn)
        {
            uint opcode = 0x5AC01400; // CLS W0, W0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "CLZ <Xd>, <Xn>")]
        [MemberData(nameof(TestData_CLZ_x))]
        public void Clz_64bit(uint rd, uint rn, ulong xn)
        {
            uint opcode = 0xDAC01000; // CLZ X0, X0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "CLZ <Wd>, <Wn>")]
        [MemberData(nameof(TestData_CLZ_w))]
        public void Clz_32bit(uint rd, uint rn, uint wn)
        {
            uint opcode = 0x5AC01000; // CLZ W0, W0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "RBIT <Xd>, <Xn>")]
        [MemberData(nameof(TestData_64bit))]
        public void Rbit_64bit(uint rd, uint rn, ulong xn)
        {
            uint opcode = 0xDAC00000; // RBIT X0, X0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "RBIT <Wd>, <Wn>")]
        [MemberData(nameof(TestData_32bit))]
        public void Rbit_32bit(uint rd, uint rn, uint wn)
        {
            uint opcode = 0x5AC00000; // RBIT W0, W0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "REV16 <Xd>, <Xn>")]
        [MemberData(nameof(TestData_64bit))]
        public void Rev16_64bit(uint rd, uint rn, ulong xn)
        {
            uint opcode = 0xDAC00400; // REV16 X0, X0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "REV16 <Wd>, <Wn>")]
        [MemberData(nameof(TestData_32bit))]
        public void Rev16_32bit(uint rd, uint rn, uint wn)
        {
            uint opcode = 0x5AC00400; // REV16 W0, W0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "REV32 <Xd>, <Xn>")]
        [MemberData(nameof(TestData_64bit))]
        public void Rev32_64bit(uint rd, uint rn, ulong xn)
        {
            uint opcode = 0xDAC00800; // REV32 X0, X0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "REV <Wd>, <Wn>")]
        [MemberData(nameof(TestData_32bit))]
        public void Rev32_32bit(uint rd, uint rn, uint wn)
        {
            uint opcode = 0x5AC00800; // REV W0, W0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, x1: wn, x31: w31);

            CompareAgainstUnicorn();
        }

        [Theory(DisplayName = "REV64 <Xd>, <Xn>")]
        [MemberData(nameof(TestData_64bit))]
        public void Rev64_64bit(uint rd, uint rn, ulong xn)
        {
            uint opcode = 0xDAC00C00; // REV64 X0, X0
            opcode |= ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = Random.Shared.NextULong();

            SingleOpcode(opcode, x1: xn, x31: x31);

            CompareAgainstUnicorn();
        }
#endif
    }
}
