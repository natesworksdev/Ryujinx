#define Mul32

using System;
using Xunit;

namespace Ryujinx.Tests.Cpu
{
    [Collection("Mul32")]
    public sealed class CpuTestMul32 : CpuTest32
    {
#if Mul32

        #region "ValueSource (Opcodes)"
        private static uint[] _Smlabb_Smlabt_Smlatb_Smlatt_()
        {
            return new[]
            {
                0xe1000080u, // SMLABB R0, R0, R0, R0
                0xe10000C0u, // SMLABT R0, R0, R0, R0
                0xe10000A0u, // SMLATB R0, R0, R0, R0
                0xe10000E0u, // SMLATT R0, R0, R0, R0
            };
        }

        private static uint[] _Smlawb_Smlawt_()
        {
            return new[]
            {
                0xe1200080u, // SMLAWB R0, R0, R0, R0
                0xe12000C0u, // SMLAWT R0, R0, R0, R0
            };
        }

        private static uint[] _Smulbb_Smulbt_Smultb_Smultt_()
        {
            return new[]
            {
                0xe1600080u, // SMULBB R0, R0, R0
                0xe16000C0u, // SMULBT R0, R0, R0
                0xe16000A0u, // SMULTB R0, R0, R0
                0xe16000E0u, // SMULTT R0, R0, R0
            };
        }

        private static uint[] _Smulwb_Smulwt_()
        {
            return new[]
            {
                0xe12000a0u, // SMULWB R0, R0, R0
                0xe12000e0u, // SMULWT R0, R0, R0
            };
        }
        #endregion

        private static readonly uint[] _testData_rn =
        {
            0u, 0xdu,
        };
        private static readonly uint[] _testData_rm =
        {
            1u, 0xdu,
        };
        private static readonly uint[] _testData_ra =
        {
            2u, 0xdu,
        };
        private static readonly uint[] _testData_rd =
        {
            3u, 0xdu,
        };
        private static readonly uint[] _testData_wn =
        {
            0x00000000u, 0x7FFFFFFFu,
            0x80000000u, 0xFFFFFFFFu,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint, uint, uint> TestData_Smla = new(_Smlabb_Smlabt_Smlatb_Smlatt_(), _testData_rn, _testData_rm, _testData_ra, _testData_rd, _testData_wn, _testData_wn, _testData_wn);

        [Theory(DisplayName = "SMLA<x><y> <Rd>, <Rn>, <Rm>, <Ra>")]
        [MemberData(nameof(TestData_Smla))]
        public void Smla___32bit(uint opcode, uint rn, uint rm, uint ra, uint rd, uint wn, uint wm, uint wa)
        {
            opcode |= ((rn & 15) << 0) | ((rm & 15) << 8) | ((ra & 15) << 12) | ((rd & 15) << 16);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, r0: wn, r1: wm, r2: wa, sp: w31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint, uint, uint> TestData_Smlaw = new(_Smlawb_Smlawt_(), _testData_rn, _testData_rm, _testData_ra, _testData_rd, _testData_wn, _testData_wn, _testData_wn);

        [Theory(DisplayName = "SMLAW<x> <Rd>, <Rn>, <Rm>, <Ra>")]
        [MemberData(nameof(TestData_Smlaw))]
        public void Smlaw__32bit(uint opcode, uint rn, uint rm, uint ra, uint rd, uint wn, uint wm, uint wa)
        {
            opcode |= ((rn & 15) << 0) | ((rm & 15) << 8) | ((ra & 15) << 12) | ((rd & 15) << 16);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, r0: wn, r1: wm, r2: wa, sp: w31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint> TestData_Smul = new(_Smulbb_Smulbt_Smultb_Smultt_(), _testData_rn, _testData_rm, _testData_rd, _testData_wn, _testData_wn);

        [Theory(DisplayName = "SMUL<x><y> <Rd>, <Rn>, <Rm>")]
        [MemberData(nameof(TestData_Smul))]
        public void Smul___32bit(uint opcode, uint rn, uint rm, uint rd, uint wn, uint wm)
        {
            opcode |= ((rn & 15) << 0) | ((rm & 15) << 8) | ((rd & 15) << 16);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, r0: wn, r1: wm, sp: w31);

            CompareAgainstUnicorn();
        }

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint> TestData_Smulw = new(_Smulwb_Smulwt_(), _testData_rn, _testData_rm, _testData_rd, _testData_wn, _testData_wn);

        [Theory(DisplayName = "SMULW<x> <Rd>, <Rn>, <Rm>")]
        [MemberData(nameof(TestData_Smulw))]
        public void Smulw__32bit(uint opcode, uint rn, uint rm, uint rd, uint wn, uint wm)
        {
            opcode |= ((rn & 15) << 0) | ((rm & 15) << 8) | ((rd & 15) << 16);

            uint w31 = Random.Shared.NextUInt();

            SingleOpcode(opcode, r0: wn, r1: wm, sp: w31);

            CompareAgainstUnicorn();
        }
#endif
    }
}
