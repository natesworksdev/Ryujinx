#define AluRs32

using System;
using Xunit;

namespace Ryujinx.Tests.Cpu
{
    [Collection("AluRs32")]
    public sealed class CpuTestAluRs32 : CpuTest32
    {
#if AluRs32

        #region "ValueSource (Opcodes)"
        private static uint[] _Add_Adds_Rsb_Rsbs_()
        {
            return new[]
            {
                0xe0800000u, // ADD R0, R0, R0, LSL #0
                0xe0900000u, // ADDS R0, R0, R0, LSL #0
                0xe0600000u, // RSB R0, R0, R0, LSL #0
                0xe0700000u, // RSBS R0, R0, R0, LSL #0
            };
        }

        private static uint[] _Adc_Adcs_Rsc_Rscs_Sbc_Sbcs_()
        {
            return new[]
            {
                0xe0a00000u, // ADC R0, R0, R0
                0xe0b00000u, // ADCS R0, R0, R0
                0xe0e00000u, // RSC R0, R0, R0
                0xe0f00000u, // RSCS R0, R0, R0
                0xe0c00000u, // SBC R0, R0, R0
                0xe0d00000u, // SBCS R0, R0, R0
            };
        }
        #endregion

        private static readonly uint[] _testData_rd =
        {
            0u, 13u,
        };
        private static readonly uint[] _testData_rn =
        {
            1u, 13u,
        };
        private static readonly uint[] _testData_rm =
        {
            2u, 13u,
        };
        private static readonly uint[] _testData_wn =
        {
            0x00000000u, 0x7FFFFFFFu,
            0x80000000u, 0xFFFFFFFFu,
        };
        private static readonly bool[] _testData_carry =
        {
            false,
            true,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint, bool> TestData = new(_Adc_Adcs_Rsc_Rscs_Sbc_Sbcs_(), _testData_rd, _testData_rn, _testData_rm, _testData_wn, _testData_wn, _testData_carry);

        [Theory]
        [MemberData(nameof(TestData))]
        public void Adc_Adcs_Rsc_Rscs_Sbc_Sbcs(uint opcode, uint rd, uint rn, uint rm, uint wn, uint wm, bool carryIn)
        {
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);

            uint sp = Random.Shared.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp, carry: carryIn);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_shift =
        {
            0b00u, 0b01u, 0b10u, 0b11u, // <LSL, LSR, ASR, ROR>
        };
        private static readonly uint[] _testData_amount =
        {
            0u, 15u, 16u, 31u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, uint, uint, uint> TestData_Add = new(_Add_Adds_Rsb_Rsbs_(), _testData_rd, _testData_rn, _testData_rm, _testData_wn, _testData_wn, _testData_shift, _testData_amount);

        [Theory]
        [MemberData(nameof(TestData_Add))]
        public void Add_Adds_Rsb_Rsbs(uint opcode, uint rd, uint rn, uint rm, uint wn, uint wm, uint shift, uint amount)
        {
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);
            opcode |= ((shift & 3) << 5) | ((amount & 31) << 7);

            uint sp = Random.Shared.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp);

            CompareAgainstUnicorn();
        }
#endif
    }
}
