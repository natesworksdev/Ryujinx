#define AluBinary32

using NUnit.Framework;

namespace Ryujinx.Tests.Cpu
{
    [Category("AluBinary32")]
    public sealed class CpuTestAluBinary32 : CpuTest32
    {
#if AluBinary32
        private const int RndCnt = 2;

        [Test, Pairwise, Description("CRC32X <Wd>, <Wn>, <Xm>")]
        public void Crc32_Crc32c_b_h_w([Values(0u, 13u)] uint rd,
                                       [Values(1u, 13u)] uint rn,
                                       [Values(2u, 13u)] uint rm,
                                       [Values] bool c,
                                       [Range(0u, 2u)] uint size,
                                       [Values(0x00000000u, 0xFFFFFFFFu)] [Random(RndCnt)] uint wn,
                                       [Values(0x00_00_00_00u,
                                               0x7F_FF_FF_FFu,
                                               0x80_00_00_00u,
                                               0xFF_FF_FF_FFu)] [Random(RndCnt)] uint wm)
        {
            uint opcode = 0xe1000040; // CRC32B R0, R0, R0
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);
            opcode |= size << 21;
            if (c)
            {
                opcode |= 1 << 9;
            }

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: wn, r2: wm, sp: sp);

            CompareAgainstUnicorn();
        }
#endif
    }
}
