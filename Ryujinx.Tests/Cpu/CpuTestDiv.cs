#define Div

using NUnit.Framework;

namespace Ryujinx.Tests.Cpu
{
    [Category("Div")]
    public sealed class CpuTestDiv : CpuTest
    {
#if Div
        private const int RndCnt = 2;

        [Test, Pairwise, Description("SDIV <Xd>, <Xn>, <Xm>")]
        public void Sdiv_64bit([Values(0u, 31u)] uint rd,
                               [Values(1u, 31u)] uint rn,
                               [Values(2u, 31u)] uint rm,
                               [Values(0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
                                       0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul)] [Random(RndCnt)] ulong xn,
                               [Values(0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
                                       0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul)] [Random(RndCnt)] ulong xm)
        {
            uint opcode = 0x9AC00C00; // SDIV X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = TestContext.CurrentContext.Random.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("SDIV <Wd>, <Wn>, <Wm>")]
        public void Sdiv_32bit([Values(0u, 31u)] uint rd,
                               [Values(1u, 31u)] uint rn,
                               [Values(2u, 31u)] uint rm,
                               [Values(0x00000000u, 0x7FFFFFFFu,
                                       0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
                               [Values(0x00000000u, 0x7FFFFFFFu,
                                       0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm)
        {
            uint opcode = 0x1AC00C00; // SDIV W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("UDIV <Xd>, <Xn>, <Xm>")]
        public void Udiv_64bit([Values(0u, 31u)] uint rd,
                               [Values(1u, 31u)] uint rn,
                               [Values(2u, 31u)] uint rm,
                               [Values(0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
                                       0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul)] [Random(RndCnt)] ulong xn,
                               [Values(0x0000000000000000ul, 0x7FFFFFFFFFFFFFFFul,
                                       0x8000000000000000ul, 0xFFFFFFFFFFFFFFFFul)] [Random(RndCnt)] ulong xm)
        {
            uint opcode = 0x9AC00800; // UDIV X0, X0, X0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            ulong x31 = TestContext.CurrentContext.Random.NextULong();

            SingleOpcode(opcode, x1: xn, x2: xm, x31: x31);

            CompareAgainstUnicorn();
        }

        [Test, Pairwise, Description("UDIV <Wd>, <Wn>, <Wm>")]
        public void Udiv_32bit([Values(0u, 31u)] uint rd,
                               [Values(1u, 31u)] uint rn,
                               [Values(2u, 31u)] uint rm,
                               [Values(0x00000000u, 0x7FFFFFFFu,
                                       0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
                               [Values(0x00000000u, 0x7FFFFFFFu,
                                       0x80000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wm)
        {
            uint opcode = 0x1AC00800; // UDIV W0, W0, W0
            opcode |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);

            uint w31 = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, x1: wn, x2: wm, x31: w31);

            CompareAgainstUnicorn();
        }
#endif
    }
}
