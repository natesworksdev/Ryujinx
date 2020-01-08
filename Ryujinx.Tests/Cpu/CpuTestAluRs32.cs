//#define AluRs32

using NUnit.Framework;

namespace Ryujinx.Tests.Cpu
{
    [Category("AluRs")]
    public sealed class CpuTestAluRs32 : CpuTest32
    {
#if AluRs32
        private const int RndCnt = 50;
        private const int RndCntAmount = 50;
        private const int RndCntLsb = 2;

        [Test, Pairwise, Description("ADC <Rd>, <Rn>, <Rm>")]
        public void Adc([Values(0u, 13u)] uint rd,
                              [Values(1u, 13u)] uint rn,
                              [Values(2u, 13u)] uint rm,
                              [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
                              [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm,
                              [Values] bool carryIn)
        {
            uint opcode = 0xe0a00000; // ADC R0, R0, R0
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp, carry: carryIn);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("ADCS <Rd>, <Rn>, <Rm>")]
        public void Adcs([Values(0u, 13u)] uint rd,
                      [Values(1u, 13u)] uint rn,
                      [Values(2u, 13u)] uint rm,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm,
                      [Values] bool carryIn)
        {
            uint opcode = 0xe0b00000; // ADCS R0, R0, R0
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp, carry: carryIn);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("ADD <Rd>, <Rn>, <Rm>{, <shift> #<amount>}")]
        public void Add([Values(0u, 13u)] uint rd,
                      [Values(1u, 13u)] uint rn,
                      [Values(2u, 13u)] uint rm,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm,
                      [Values(0b00u, 0b01u, 0b10u, 0b11u)] uint shift, // <LSL, LSR, ASR, ROR>
                      [Values(0u, 15u, 16u, 31u)] [Random(0u, 31u, RndCntAmount)] uint amount)
        {
            uint opcode = 0xe0800000; // ADD R0, R0, R0, LSL #0
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);
            opcode |= ((shift & 3) << 5) | ((amount & 31) << 7);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("ADDS <Rd>, <Rn>, <Rm>{, <shift> #<amount>}")]
        public void Adds([Values(0u, 13u)] uint rd,
                      [Values(1u, 13u)] uint rn,
                      [Values(2u, 13u)] uint rm,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm,
                      [Values(0b00u, 0b01u, 0b10u, 0b11u)] uint shift, // <LSL, LSR, ASR, ROR>
                      [Values(0u, 15u, 16u, 31u)] [Random(0u, 31u, RndCntAmount)] uint amount)
        {
            uint opcode = 0xe0900000; // ADDS R0, R0, R0, LSL #0
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);
            opcode |= ((shift & 3) << 5) | ((amount & 31) << 7);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("RSB <Rd>, <Rn>, <Rm>{, <shift> #<amount>}")]
        public void Rsb([Values(0u, 13u)] uint rd,
                      [Values(1u, 13u)] uint rn,
                      [Values(2u, 13u)] uint rm,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm,
                      [Values(0b00u, 0b01u, 0b10u, 0b11u)] uint shift, // <LSL, LSR, ASR, ROR>
                      [Values(0u, 15u, 16u, 31u)] [Random(0u, 31u, RndCntAmount)] uint amount)
        {
            uint opcode = 0xe0600000; // RSB R0, R0, R0, LSL #0
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);
            opcode |= ((shift & 3) << 5) | ((amount & 31) << 7);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("RSBS <Rd>, <Rn>, <Rm>{, <shift> #<amount>}")]
        public void Rsbs([Values(0u, 13u)] uint rd,
                      [Values(1u, 13u)] uint rn,
                      [Values(2u, 13u)] uint rm,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm,
                      [Values(0b00u, 0b01u, 0b10u, 0b11u)] uint shift, // <LSL, LSR, ASR, ROR>
                      [Values(0u, 15u, 16u, 31u)] [Random(0u, 31u, RndCntAmount)] uint amount)
        {
            uint opcode = 0xe0700000; // RSBS R0, R0, R0, LSL #0
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);
            opcode |= ((shift & 3) << 5) | ((amount & 31) << 7);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("RSB <Rd>, <Rn>, <Rm>")]
        public void Rsc([Values(0u, 13u)] uint rd,
              [Values(1u, 13u)] uint rn,
              [Values(2u, 13u)] uint rm,
              [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
              [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm,
              [Values] bool carryIn)
        {
            uint opcode = 0xe0e00000; // RSC R0, R0, R0
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp, carry: carryIn);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("RSCS <Rd>, <Rn>, <Rm>")]
        public void Rscs([Values(0u, 13u)] uint rd,
              [Values(1u, 13u)] uint rn,
              [Values(2u, 13u)] uint rm,
              [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
              [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm,
              [Values] bool carryIn)
        {
            uint opcode = 0xe0f00000; // RSCS R0, R0, R0
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp, carry: carryIn);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SBC <Rd>, <Rn>, <Rm>")]
        public void Sbc([Values(0u, 13u)] uint rd,
                      [Values(1u, 13u)] uint rn,
                      [Values(2u, 13u)] uint rm,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm,
                      [Values] bool carryIn)
        {
            uint opcode = 0xe0c00000; // SBC R0, R0, R0
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp, carry: carryIn);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SBCS <Rd>, <Rn>, <Rm>")]
        public void Sbcs([Values(0u, 13u)] uint rd,
                      [Values(1u, 13u)] uint rn,
                      [Values(2u, 13u)] uint rm,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
                      [Values(0x00000000u, 0x7FFFFFFFu,
                                      0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm,
                      [Values] bool carryIn)
        {
            uint opcode = 0xe0d00000; // SBCS R0, R0, R0
            opcode |= ((rm & 15) << 0) | ((rn & 15) << 16) | ((rd & 15) << 12);

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp, carry: carryIn);

            CompareAgainstUnicorn();
        }
#endif
    }
}
