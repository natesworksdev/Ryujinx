#define AluBinary32

using ARMeilleure.State;
using NUnit.Framework;
using System;

namespace Ryujinx.Tests.Cpu
{

    [Category("AluBinary32")]
    public sealed class CpuTestAluBinary32 : CpuTest32
    {
#if AluBinary32

        public struct CrcTest
        {
            public uint Crc;
            public uint Value;
            public bool C;

            public uint[] Results; // One result for each CRC variant (8, 16, 32)

            public CrcTest(uint crc, uint value, bool c, params uint[] results)
            {
                Crc = crc;
                Value = value;
                C = c;
                Results = results;
            }
        }

#region "ValueSource (CRC32/CRC32C)"
        private static CrcTest[] _CRC32_Test_Values_()
        {
            // Created with SoftFallback.Crc32*

            return new CrcTest[]
            {
                new CrcTest(0x00000000u, 0x00_00_00_00u, false, 0x00000000, 0x00000000, 0x00000000),
                new CrcTest(0x00000000u, 0x7F_FF_FF_FFu, false, 0x2d02ef8d, 0xbe2612ff, 0x3303a3c3),
                new CrcTest(0x00000000u, 0x80_00_00_00u, false, 0x00000000, 0x00000000, 0xedb88320),
                new CrcTest(0x00000000u, 0xFF_FF_FF_FFu, false, 0x2d02ef8d, 0xbe2612ff, 0xdebb20e3),
                new CrcTest(0x00000000u, 0x9D_CB_12_F0u, false, 0xbdbdf21c, 0xe70590f5, 0x3f7480c5),

                new CrcTest(0xFFFFFFFFu, 0x00_00_00_00u, false, 0x2dfd1072, 0xbe26ed00, 0xdebb20e3),
                new CrcTest(0xFFFFFFFFu, 0x7F_FF_FF_FFu, false, 0x00ffffff, 0x0000ffff, 0xedb88320),
                new CrcTest(0xFFFFFFFFu, 0x80_00_00_00u, false, 0x2dfd1072, 0xbe26ed00, 0x3303a3c3),
                new CrcTest(0xFFFFFFFFu, 0xFF_FF_FF_FFu, false, 0x00ffffff, 0x0000ffff, 0x00000000),
                new CrcTest(0xFFFFFFFFu, 0x9D_CB_12_F0u, false, 0x9040e26e, 0x59237df5, 0xe1cfa026)//,

                /*
                new CrcTest(0x00000000u, 0x00_00_00_00u, true, 0x527D5351, 0, 0),
                new CrcTest(0x00000000u, 0x7F_FF_FF_FFu, true, 0, 0, 0),
                new CrcTest(0x00000000u, 0x80_00_00_00u, true, 0, 0, 0),
                new CrcTest(0x00000000u, 0xFF_FF_FF_FFu, true, 0, 0, 0),
                new CrcTest(0x00000000u, 0x9D_CB_12_F0u, true, 0, 0, 0),

                new CrcTest(0xFFFFFFFFu, 0x00_00_00_00u, true, 0, 0, 0),
                new CrcTest(0xFFFFFFFFu, 0x7F_FF_FF_FFu, true, 0, 0, 0),
                new CrcTest(0xFFFFFFFFu, 0x80_00_00_00u, true, 0, 0, 0),
                new CrcTest(0xFFFFFFFFu, 0xFF_FF_FF_FFu, true, 0, 0, 0),
                new CrcTest(0xFFFFFFFFu, 0x9D_CB_12_F0u, true, 0, 0, 0),
                */
            };
        }
#endregion

        private const int RndCnt = 2;

        [Test, Combinatorial, Description("CRC32 <Wd>, <Wn>, <Xm>")]
        public void Crc32_Crc32c_b_h_w([Values(0u)] uint rd,
                                       [Values(1u)] uint rn,
                                       [Values(2u)] uint rm,
                                       [Range(0u, 2u)] uint size,
                                       [ValueSource("_CRC32_Test_Values_")] CrcTest test)
        {
            uint opcode = 0xe1000040; // CRC32B R0, R0, R0
            opcode |= ((rm & 15) << 0) | ((rd & 15) << 12) | ((rn & 15) << 16);
            opcode |= size << 21;
            if (test.C)
            {
                opcode |= 1 << 9;
            }

            uint sp = TestContext.CurrentContext.Random.NextUInt();

            SingleOpcode(opcode, r1: test.Crc, r2: test.Value, sp: sp, runUnicorn: false);

            ExecutionContext context = GetContext();
            ulong result = context.GetX((int)rd);
            Assert.That(result == test.Results[size]);

            // Unicorn does not yet support crc instructions.
            // CompareAgainstUnicorn();
        }
#endif
    }
}
