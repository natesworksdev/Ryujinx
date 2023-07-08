#define AluImm32

using System;
using Xunit;

namespace Ryujinx.Tests.Cpu
{
    [Collection("AluImm32")]
    public sealed class CpuTestAluImm32 : CpuTest32
    {
#if AluImm32

        #region "ValueSource (Opcodes)"
        private static uint[] Opcodes()
        {
            return new[]
            {
                0xe2a00000u, // ADC R0, R0, #0
                0xe2b00000u, // ADCS R0, R0, #0
                0xe2800000u, // ADD R0, R0, #0
                0xe2900000u, // ADDS R0, R0, #0
                0xe3c00000u, // BIC R0, R0, #0
                0xe3d00000u, // BICS R0, R0, #0
                0xe2600000u, // RSB R0, R0, #0
                0xe2700000u, // RSBS R0, R0, #0
                0xe2e00000u, // RSC R0, R0, #0
                0xe2f00000u, // RSCS R0, R0, #0
                0xe2c00000u, // SBC R0, R0, #0
                0xe2d00000u, // SBCS R0, R0, #0
                0xe2400000u, // SUB R0, R0, #0
                0xe2500000u, // SUBS R0, R0, #0
            };
        }
        #endregion

        private const int RndCnt = 2;

        private static readonly uint[] _testData_rd =
        {
            0u, 13u,
        };
        private static readonly uint[] _testData_rn =
        {
            1u, 13u,
        };
        private static readonly bool[] _testData_carry =
        {
            false,
            true,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, uint, bool> TestData = new(Opcodes(), _testData_rd, _testData_rn, Random.Shared.NextUIntEnumerable(RndCnt), Random.Shared.NextUIntEnumerable(RndCnt), _testData_carry);

        [Theory]
        [MemberData(nameof(TestData))]
        public void TestCpuTestAluImm32(uint opcode, uint rd, uint rn, uint imm, uint wn, bool carryIn)
        {
            opcode |= ((imm & 0xfff) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);

            uint sp = Random.Shared.NextUInt();

            SingleOpcode(opcode, r1: wn, sp: sp, carry: carryIn);

            CompareAgainstUnicorn();
        }
#endif
    }
}
