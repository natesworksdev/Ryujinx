#define SimdTbl

using ARMeilleure.State;
using System;
using System.Collections.Generic;
using Xunit;

namespace Ryujinx.Tests.Cpu
{
    [Collection("SimdTbl")]
    public sealed class CpuTestSimdTbl : CpuTest
    {
#if SimdTbl

        #region "Helper methods"
        private static ulong GenIdxsForTbls(int regs)
        {
            const byte IdxInRngMin = 0;
            byte idxInRngMax = (byte)((16 * regs) - 1);
            byte idxOutRngMin = (byte)(16 * regs);
            const byte IdxOutRngMax = 255;

            ulong idxs = 0ul;

            for (int cnt = 1; cnt <= 8; cnt++)
            {
                ulong idxInRng = Random.Shared.NextByte(IdxInRngMin, idxInRngMax);
                ulong idxOutRng = Random.Shared.NextByte(idxOutRngMin, IdxOutRngMax);

                ulong idx = Random.Shared.NextBool() ? idxInRng : idxOutRng;

                idxs = (idxs << 8) | idx;
            }

            return idxs;
        }
        #endregion

        #region "ValueSource (Types)"
        private static ulong[] _8B_()
        {
            return new[] {
                0x0000000000000000ul, 0x7F7F7F7F7F7F7F7Ful,
                0x8080808080808080ul, 0xFFFFFFFFFFFFFFFFul,
            };
        }

        private static IEnumerable<ulong> _GenIdxsForTbl1_()
        {
            yield return 0x0000000000000000ul;
            yield return 0x7F7F7F7F7F7F7F7Ful;
            yield return 0x8080808080808080ul;
            yield return 0xFFFFFFFFFFFFFFFFul;

            for (int cnt = 1; cnt <= RndCntIdxs; cnt++)
            {
                yield return GenIdxsForTbls(regs: 1);
            }
        }

        private static IEnumerable<ulong> _GenIdxsForTbl2_()
        {
            yield return 0x0000000000000000ul;
            yield return 0x7F7F7F7F7F7F7F7Ful;
            yield return 0x8080808080808080ul;
            yield return 0xFFFFFFFFFFFFFFFFul;

            for (int cnt = 1; cnt <= RndCntIdxs; cnt++)
            {
                yield return GenIdxsForTbls(regs: 2);
            }
        }

        private static IEnumerable<ulong> _GenIdxsForTbl3_()
        {
            yield return 0x0000000000000000ul;
            yield return 0x7F7F7F7F7F7F7F7Ful;
            yield return 0x8080808080808080ul;
            yield return 0xFFFFFFFFFFFFFFFFul;

            for (int cnt = 1; cnt <= RndCntIdxs; cnt++)
            {
                yield return GenIdxsForTbls(regs: 3);
            }
        }

        private static IEnumerable<ulong> _GenIdxsForTbl4_()
        {
            yield return 0x0000000000000000ul;
            yield return 0x7F7F7F7F7F7F7F7Ful;
            yield return 0x8080808080808080ul;
            yield return 0xFFFFFFFFFFFFFFFFul;

            for (int cnt = 1; cnt <= RndCntIdxs; cnt++)
            {
                yield return GenIdxsForTbls(regs: 4);
            }
        }
        #endregion

        #region "ValueSource (Opcodes)"
        private static uint[] _SingleRegisterTable_V_8B_16B_()
        {
            return new[]
            {
                0x0E000000u, // TBL V0.8B, { V0.16B }, V0.8B
                0x0E001000u, // TBX V0.8B, { V0.16B }, V0.8B
            };
        }

        private static uint[] _TwoRegisterTable_V_8B_16B_()
        {
            return new[]
            {
                0x0E002000u, // TBL V0.8B, { V0.16B, V1.16B }, V0.8B
                0x0E003000u, // TBX V0.8B, { V0.16B, V1.16B }, V0.8B
            };
        }

        private static uint[] _ThreeRegisterTable_V_8B_16B_()
        {
            return new[]
            {
                0x0E004000u, // TBL V0.8B, { V0.16B, V1.16B, V2.16B }, V0.8B
                0x0E005000u, // TBX V0.8B, { V0.16B, V1.16B, V2.16B }, V0.8B
            };
        }

        private static uint[] _FourRegisterTable_V_8B_16B_()
        {
            return new[]
            {
                0x0E006000u, // TBL V0.8B, { V0.16B, V1.16B, V2.16B, V3.16B }, V0.8B
                0x0E006000u, // TBX V0.8B, { V0.16B, V1.16B, V2.16B, V3.16B }, V0.8B
            };
        }
        #endregion

        private const int RndCntIdxs = 2;

        private static readonly uint[] _testData_rd =
        {
            0u,
        };
        private static readonly uint[] _testData_rn =
        {
            1u,
        };
        private static readonly uint[] _testData_rm =
        {
            2u,
        };
        private static readonly uint[] _testData_q =
        {
            0b0u, 0b1u, // <8B, 16B>
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, ulong, ulong, ulong, uint> TestData = new(_SingleRegisterTable_V_8B_16B_(), _testData_rd, _testData_rn, _testData_rm, _8B_(), _8B_(), _GenIdxsForTbl1_(), _testData_q);

        [Theory]
        [MemberData(nameof(TestData))]
        public void SingleRegisterTable_V_8B_16B(uint opcodes, uint rd, uint rn, uint rm, ulong z, ulong table0, ulong indexes, uint q)
        {
            opcodes |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcodes |= ((q & 1) << 30);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(table0, table0);
            V128 v2 = MakeVectorE0E1(indexes, q == 1u ? indexes : 0ul);

            SingleOpcode(opcodes, v0: v0, v1: v1, v2: v2);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_Two_rm =
        {
            3u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, ulong, ulong, ulong, ulong, uint> TestData_Two =
            new(_TwoRegisterTable_V_8B_16B_(), _testData_rd, _testData_rn, _testData_Two_rm, _8B_(), _8B_(), _8B_(), _GenIdxsForTbl2_(), _testData_q);

        [Theory]
        [MemberData(nameof(TestData_Two))]
        public void TwoRegisterTable_V_8B_16B(uint opcodes, uint rd, uint rn, uint rm, ulong z, ulong table0, ulong table1, ulong indexes, uint q)
        {
            opcodes |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcodes |= ((q & 1) << 30);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(table0, table0);
            V128 v2 = MakeVectorE0E1(table1, table1);
            V128 v3 = MakeVectorE0E1(indexes, q == 1u ? indexes : 0ul);

            SingleOpcode(opcodes, v0: v0, v1: v1, v2: v2, v3: v3);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_ModTwo_rd =
        {
            30u, 1u,
        };
        private static readonly uint[] _testData_ModTwo_rn =
        {
            31u,
        };
        private static readonly uint[] _testData_ModTwo_rm =
        {
            1u, 30u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, ulong, ulong, ulong, ulong, uint> TestData_ModTwo =
            new(_TwoRegisterTable_V_8B_16B_(), _testData_ModTwo_rd, _testData_ModTwo_rn, _testData_ModTwo_rm, _8B_(), _8B_(), _8B_(), _GenIdxsForTbl2_(), _testData_q);

        [Theory]
        [MemberData(nameof(TestData_ModTwo))]
        public void Mod_TwoRegisterTable_V_8B_16B(uint opcodes, uint rd, uint rn, uint rm, ulong z, ulong table0, ulong table1, ulong indexes, uint q)
        {
            opcodes |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcodes |= ((q & 1) << 30);

            V128 v30 = MakeVectorE0E1(z, z);
            V128 v31 = MakeVectorE0E1(table0, table0);
            V128 v0 = MakeVectorE0E1(table1, table1);
            V128 v1 = MakeVectorE0E1(indexes, indexes);

            SingleOpcode(opcodes, v0: v0, v1: v1, v30: v30, v31: v31);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_Three_rm =
        {
            4u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, ulong, ulong, ulong, ulong, ulong, uint> TestData_Three =
            new(_TwoRegisterTable_V_8B_16B_(), _testData_rd, _testData_rn, _testData_Three_rm, _8B_(), _8B_(), _8B_(), _8B_(), _GenIdxsForTbl3_(), _testData_q);

        [Theory]
        [MemberData(nameof(TestData_Three))]
        public void ThreeRegisterTable_V_8B_16B(uint opcodes, uint rd, uint rn, uint rm, ulong z, ulong table0, ulong table1, ulong table2, ulong indexes, uint q)
        {
            opcodes |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcodes |= ((q & 1) << 30);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(table0, table0);
            V128 v2 = MakeVectorE0E1(table1, table1);
            V128 v3 = MakeVectorE0E1(table2, table2);
            V128 v4 = MakeVectorE0E1(indexes, q == 1u ? indexes : 0ul);

            SingleOpcode(opcodes, v0: v0, v1: v1, v2: v2, v3: v3, v4: v4);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_ModThree_rd =
        {
            30u, 2u,
        };
        private static readonly uint[] _testData_ModThree_rm =
        {
            2u, 30u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, ulong, ulong, ulong, ulong, ulong, uint> TestData_ModThree =
            new(_ThreeRegisterTable_V_8B_16B_(), _testData_ModThree_rd, _testData_ModTwo_rn, _testData_ModThree_rm, _8B_(), _8B_(), _8B_(), _8B_(), _GenIdxsForTbl3_(), _testData_q);

        [Theory]
        [MemberData(nameof(TestData_ModThree))]
        public void Mod_ThreeRegisterTable_V_8B_16B(uint opcodes, uint rd, uint rn, uint rm, ulong z, ulong table0, ulong table1, ulong table2, ulong indexes, uint q)
        {
            opcodes |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcodes |= ((q & 1) << 30);

            V128 v30 = MakeVectorE0E1(z, z);
            V128 v31 = MakeVectorE0E1(table0, table0);
            V128 v0 = MakeVectorE0E1(table1, table1);
            V128 v1 = MakeVectorE0E1(table2, table2);
            V128 v2 = MakeVectorE0E1(indexes, indexes);

            SingleOpcode(opcodes, v0: v0, v1: v1, v2: v2, v30: v30, v31: v31);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_Four_rm =
        {
            5u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, ulong, ulong, ulong, ulong, ulong, ulong, uint> TestData_Four =
            new(_FourRegisterTable_V_8B_16B_(), _testData_rd, _testData_rn, _testData_Four_rm, _8B_(), _8B_(), _8B_(), _8B_(), _8B_(), _GenIdxsForTbl4_(), _testData_q);

        [Theory]
        [MemberData(nameof(TestData_Four))]
        public void FourRegisterTable_V_8B_16B(uint opcodes, uint rd, uint rn, uint rm, ulong z, ulong table0, ulong table1, ulong table2, ulong table3, ulong indexes, uint q)
        {
            opcodes |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcodes |= ((q & 1) << 30);

            V128 v0 = MakeVectorE0E1(z, z);
            V128 v1 = MakeVectorE0E1(table0, table0);
            V128 v2 = MakeVectorE0E1(table1, table1);
            V128 v3 = MakeVectorE0E1(table2, table2);
            V128 v4 = MakeVectorE0E1(table3, table3);
            V128 v5 = MakeVectorE0E1(indexes, q == 1u ? indexes : 0ul);

            SingleOpcode(opcodes, v0: v0, v1: v1, v2: v2, v3: v3, v4: v4, v5: v5);

            CompareAgainstUnicorn();
        }

        private static readonly uint[] _testData_ModFour_rd =
        {
            30u, 3u,
        };
        private static readonly uint[] _testData_ModFour_rm =
        {
            3u, 30u,
        };

        public static readonly MatrixTheoryData<uint, uint, uint, uint, ulong, ulong, ulong, ulong, ulong, ulong, uint> TestData_ModFour =
            new(_FourRegisterTable_V_8B_16B_(), _testData_ModFour_rd, _testData_ModTwo_rn, _testData_ModFour_rm, _8B_(), _8B_(), _8B_(), _8B_(), _8B_(), _GenIdxsForTbl4_(), _testData_q);

        [Theory]
        [MemberData(nameof(TestData_ModFour))]
        public void Mod_FourRegisterTable_V_8B_16B(uint opcodes, uint rd, uint rn, uint rm, ulong z, ulong table0, ulong table1, ulong table2, ulong table3, ulong indexes, uint q)
        {
            opcodes |= ((rm & 31) << 16) | ((rn & 31) << 5) | ((rd & 31) << 0);
            opcodes |= ((q & 1) << 30);

            V128 v30 = MakeVectorE0E1(z, z);
            V128 v31 = MakeVectorE0E1(table0, table0);
            V128 v0 = MakeVectorE0E1(table1, table1);
            V128 v1 = MakeVectorE0E1(table2, table2);
            V128 v2 = MakeVectorE0E1(table3, table3);
            V128 v3 = MakeVectorE0E1(indexes, indexes);

            SingleOpcode(opcodes, v0: v0, v1: v1, v2: v2, v3: v3, v30: v30, v31: v31);

            CompareAgainstUnicorn();
        }
#endif
    }
}
