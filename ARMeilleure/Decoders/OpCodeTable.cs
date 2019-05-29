using ARMeilleure.Instructions;
using ARMeilleure.State;
using System;
using System.Collections.Generic;

namespace ARMeilleure.Decoders
{
    static class OpCodeTable
    {
        private const int FastLookupSize = 0x1000;

        private struct InstInfo
        {
            public int Mask  { get; }
            public int Value { get; }

            public InstDescriptor Inst { get; }

            public Type Type { get; }

            public InstInfo(int mask, int value, InstDescriptor inst, Type type)
            {
                Mask  = mask;
                Value = value;
                Inst  = inst;
                Type  = type;
            }
        }

        private static List<InstInfo> _allInstA32 = new List<InstInfo>();
        private static List<InstInfo> _allInstT32 = new List<InstInfo>();
        private static List<InstInfo> _allInstA64 = new List<InstInfo>();

        private static InstInfo[][] _instA32FastLookup = new InstInfo[FastLookupSize][];
        private static InstInfo[][] _instT32FastLookup = new InstInfo[FastLookupSize][];
        private static InstInfo[][] _instA64FastLookup = new InstInfo[FastLookupSize][];

        static OpCodeTable()
        {
#region "OpCode Table (AArch64)"
            //Base
            SetA64("x0011010000xxxxx000000xxxxxxxxxx", InstName.Adc,             InstEmit.Adc,             typeof(OpCodeAluRs));
            SetA64("x0111010000xxxxx000000xxxxxxxxxx", InstName.Adcs,            InstEmit.Adcs,            typeof(OpCodeAluRs));
            SetA64("x00100010xxxxxxxxxxxxxxxxxxxxxxx", InstName.Add,             InstEmit.Add,             typeof(OpCodeAluImm));
            SetA64("00001011<<0xxxxx0xxxxxxxxxxxxxxx", InstName.Add,             InstEmit.Add,             typeof(OpCodeAluRs));
            SetA64("10001011<<0xxxxxxxxxxxxxxxxxxxxx", InstName.Add,             InstEmit.Add,             typeof(OpCodeAluRs));
            SetA64("x0001011001xxxxxxxx0xxxxxxxxxxxx", InstName.Add,             InstEmit.Add,             typeof(OpCodeAluRx));
            SetA64("x0001011001xxxxxxxx100xxxxxxxxxx", InstName.Add,             InstEmit.Add,             typeof(OpCodeAluRx));
            SetA64("x01100010xxxxxxxxxxxxxxxxxxxxxxx", InstName.Adds,            InstEmit.Adds,            typeof(OpCodeAluImm));
            SetA64("00101011<<0xxxxx0xxxxxxxxxxxxxxx", InstName.Adds,            InstEmit.Adds,            typeof(OpCodeAluRs));
            SetA64("10101011<<0xxxxxxxxxxxxxxxxxxxxx", InstName.Adds,            InstEmit.Adds,            typeof(OpCodeAluRs));
            SetA64("x0101011001xxxxxxxx0xxxxxxxxxxxx", InstName.Adds,            InstEmit.Adds,            typeof(OpCodeAluRx));
            SetA64("x0101011001xxxxxxxx100xxxxxxxxxx", InstName.Adds,            InstEmit.Adds,            typeof(OpCodeAluRx));
            SetA64("0xx10000xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Adr,             InstEmit.Adr,             typeof(OpCodeAdr));
            SetA64("1xx10000xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Adrp,            InstEmit.Adrp,            typeof(OpCodeAdr));
            SetA64("0001001000xxxxxxxxxxxxxxxxxxxxxx", InstName.And,             InstEmit.And,             typeof(OpCodeAluImm));
            SetA64("100100100xxxxxxxxxxxxxxxxxxxxxxx", InstName.And,             InstEmit.And,             typeof(OpCodeAluImm));
            SetA64("00001010xx0xxxxx0xxxxxxxxxxxxxxx", InstName.And,             InstEmit.And,             typeof(OpCodeAluRs));
            SetA64("10001010xx0xxxxxxxxxxxxxxxxxxxxx", InstName.And,             InstEmit.And,             typeof(OpCodeAluRs));
            SetA64("0111001000xxxxxxxxxxxxxxxxxxxxxx", InstName.Ands,            InstEmit.Ands,            typeof(OpCodeAluImm));
            SetA64("111100100xxxxxxxxxxxxxxxxxxxxxxx", InstName.Ands,            InstEmit.Ands,            typeof(OpCodeAluImm));
            SetA64("01101010xx0xxxxx0xxxxxxxxxxxxxxx", InstName.Ands,            InstEmit.Ands,            typeof(OpCodeAluRs));
            SetA64("11101010xx0xxxxxxxxxxxxxxxxxxxxx", InstName.Ands,            InstEmit.Ands,            typeof(OpCodeAluRs));
            SetA64("x0011010110xxxxx001010xxxxxxxxxx", InstName.Asrv,            InstEmit.Asrv,            typeof(OpCodeAluRs));
            SetA64("000101xxxxxxxxxxxxxxxxxxxxxxxxxx", InstName.B,               InstEmit.B,               typeof(OpCodeBImmAl));
            SetA64("01010100xxxxxxxxxxxxxxxxxxx0xxxx", InstName.B_Cond,          InstEmit.B_Cond,          typeof(OpCodeBImmCond));
            SetA64("00110011000xxxxx0xxxxxxxxxxxxxxx", InstName.Bfm,             InstEmit.Bfm,             typeof(OpCodeBfm));
            SetA64("1011001101xxxxxxxxxxxxxxxxxxxxxx", InstName.Bfm,             InstEmit.Bfm,             typeof(OpCodeBfm));
            SetA64("00001010xx1xxxxx0xxxxxxxxxxxxxxx", InstName.Bic,             InstEmit.Bic,             typeof(OpCodeAluRs));
            SetA64("10001010xx1xxxxxxxxxxxxxxxxxxxxx", InstName.Bic,             InstEmit.Bic,             typeof(OpCodeAluRs));
            SetA64("01101010xx1xxxxx0xxxxxxxxxxxxxxx", InstName.Bics,            InstEmit.Bics,            typeof(OpCodeAluRs));
            SetA64("11101010xx1xxxxxxxxxxxxxxxxxxxxx", InstName.Bics,            InstEmit.Bics,            typeof(OpCodeAluRs));
            SetA64("100101xxxxxxxxxxxxxxxxxxxxxxxxxx", InstName.Bl,              InstEmit.Bl,              typeof(OpCodeBImmAl));
            SetA64("1101011000111111000000xxxxx00000", InstName.Blr,             InstEmit.Blr,             typeof(OpCodeBReg));
            SetA64("1101011000011111000000xxxxx00000", InstName.Br,              InstEmit.Br,              typeof(OpCodeBReg));
            SetA64("11010100001xxxxxxxxxxxxxxxx00000", InstName.Brk,             null,                     typeof(OpCodeException));
            SetA64("x0110101xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Cbnz,            InstEmit.Cbnz,            typeof(OpCodeBImmCmp));
            SetA64("x0110100xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Cbz,             InstEmit.Cbz,             typeof(OpCodeBImmCmp));
            SetA64("x0111010010xxxxxxxxx10xxxxx0xxxx", InstName.Ccmn,            InstEmit.Ccmn,            typeof(OpCodeCcmpImm));
            SetA64("x0111010010xxxxxxxxx00xxxxx0xxxx", InstName.Ccmn,            InstEmit.Ccmn,            typeof(OpCodeCcmpReg));
            SetA64("x1111010010xxxxxxxxx10xxxxx0xxxx", InstName.Ccmp,            InstEmit.Ccmp,            typeof(OpCodeCcmpImm));
            SetA64("x1111010010xxxxxxxxx00xxxxx0xxxx", InstName.Ccmp,            InstEmit.Ccmp,            typeof(OpCodeCcmpReg));
            SetA64("11010101000000110011xxxx01011111", InstName.Clrex,           null,                     typeof(OpCodeSystem));
            SetA64("x101101011000000000101xxxxxxxxxx", InstName.Cls,             InstEmit.Cls,             typeof(OpCodeAlu));
            SetA64("x101101011000000000100xxxxxxxxxx", InstName.Clz,             InstEmit.Clz,             typeof(OpCodeAlu));
            SetA64("00011010110xxxxx010000xxxxxxxxxx", InstName.Crc32b,          null,                     typeof(OpCodeAluRs));
            SetA64("00011010110xxxxx010001xxxxxxxxxx", InstName.Crc32h,          null,                     typeof(OpCodeAluRs));
            SetA64("00011010110xxxxx010010xxxxxxxxxx", InstName.Crc32w,          null,                     typeof(OpCodeAluRs));
            SetA64("10011010110xxxxx010011xxxxxxxxxx", InstName.Crc32x,          null,                     typeof(OpCodeAluRs));
            SetA64("00011010110xxxxx010100xxxxxxxxxx", InstName.Crc32cb,         null,                     typeof(OpCodeAluRs));
            SetA64("00011010110xxxxx010101xxxxxxxxxx", InstName.Crc32ch,         null,                     typeof(OpCodeAluRs));
            SetA64("00011010110xxxxx010110xxxxxxxxxx", InstName.Crc32cw,         null,                     typeof(OpCodeAluRs));
            SetA64("10011010110xxxxx010111xxxxxxxxxx", InstName.Crc32cx,         null,                     typeof(OpCodeAluRs));
            SetA64("x0011010100xxxxxxxxx00xxxxxxxxxx", InstName.Csel,            InstEmit.Csel,            typeof(OpCodeCsel));
            SetA64("x0011010100xxxxxxxxx01xxxxxxxxxx", InstName.Csinc,           InstEmit.Csinc,           typeof(OpCodeCsel));
            SetA64("x1011010100xxxxxxxxx00xxxxxxxxxx", InstName.Csinv,           InstEmit.Csinv,           typeof(OpCodeCsel));
            SetA64("x1011010100xxxxxxxxx01xxxxxxxxxx", InstName.Csneg,           InstEmit.Csneg,           typeof(OpCodeCsel));
            SetA64("11010101000000110011xxxx10111111", InstName.Dmb,             null,                     typeof(OpCodeSystem));
            SetA64("11010101000000110011xxxx10011111", InstName.Dsb,             null,                     typeof(OpCodeSystem));
            SetA64("01001010xx1xxxxx0xxxxxxxxxxxxxxx", InstName.Eon,             InstEmit.Eon,             typeof(OpCodeAluRs));
            SetA64("11001010xx1xxxxxxxxxxxxxxxxxxxxx", InstName.Eon,             InstEmit.Eon,             typeof(OpCodeAluRs));
            SetA64("0101001000xxxxxxxxxxxxxxxxxxxxxx", InstName.Eor,             InstEmit.Eor,             typeof(OpCodeAluImm));
            SetA64("110100100xxxxxxxxxxxxxxxxxxxxxxx", InstName.Eor,             InstEmit.Eor,             typeof(OpCodeAluImm));
            SetA64("01001010xx0xxxxx0xxxxxxxxxxxxxxx", InstName.Eor,             InstEmit.Eor,             typeof(OpCodeAluRs));
            SetA64("11001010xx0xxxxxxxxxxxxxxxxxxxxx", InstName.Eor,             InstEmit.Eor,             typeof(OpCodeAluRs));
            SetA64("00010011100xxxxx0xxxxxxxxxxxxxxx", InstName.Extr,            InstEmit.Extr,            typeof(OpCodeAluRs));
            SetA64("10010011110xxxxxxxxxxxxxxxxxxxxx", InstName.Extr,            InstEmit.Extr,            typeof(OpCodeAluRs));
            SetA64("11010101000000110010xxxxxxx11111", InstName.Hint,            null,                     typeof(OpCodeSystem));
            SetA64("11010101000000110011xxxx11011111", InstName.Isb,             null,                     typeof(OpCodeSystem));
            SetA64("xx001000110xxxxx1xxxxxxxxxxxxxxx", InstName.Ldar,            null,                     typeof(OpCodeMemEx));
            SetA64("1x001000011xxxxx1xxxxxxxxxxxxxxx", InstName.Ldaxp,           null,                     typeof(OpCodeMemEx));
            SetA64("xx001000010xxxxx1xxxxxxxxxxxxxxx", InstName.Ldaxr,           null,                     typeof(OpCodeMemEx));
            SetA64("<<10100xx1xxxxxxxxxxxxxxxxxxxxxx", InstName.Ldp,             InstEmit.Ldp,             typeof(OpCodeMemPair));
            SetA64("xx111000010xxxxxxxxxxxxxxxxxxxxx", InstName.Ldr,             InstEmit.Ldr,             typeof(OpCodeMemImm));
            SetA64("xx11100101xxxxxxxxxxxxxxxxxxxxxx", InstName.Ldr,             InstEmit.Ldr,             typeof(OpCodeMemImm));
            SetA64("xx111000011xxxxxxxxx10xxxxxxxxxx", InstName.Ldr,             InstEmit.Ldr,             typeof(OpCodeMemReg));
            SetA64("xx011000xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Ldr_Literal,     InstEmit.Ldr_Literal,     typeof(OpCodeMemLit));
            SetA64("0x1110001x0xxxxxxxxxxxxxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            typeof(OpCodeMemImm));
            SetA64("0x1110011xxxxxxxxxxxxxxxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            typeof(OpCodeMemImm));
            SetA64("10111000100xxxxxxxxxxxxxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            typeof(OpCodeMemImm));
            SetA64("1011100110xxxxxxxxxxxxxxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            typeof(OpCodeMemImm));
            SetA64("0x1110001x1xxxxxxxxx10xxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            typeof(OpCodeMemReg));
            SetA64("10111000101xxxxxxxxx10xxxxxxxxxx", InstName.Ldrs,            InstEmit.Ldrs,            typeof(OpCodeMemReg));
            SetA64("xx001000010xxxxx0xxxxxxxxxxxxxxx", InstName.Ldxr,            null,                     typeof(OpCodeMemEx));
            SetA64("1x001000011xxxxx0xxxxxxxxxxxxxxx", InstName.Ldxp,            null,                     typeof(OpCodeMemEx));
            SetA64("x0011010110xxxxx001000xxxxxxxxxx", InstName.Lslv,            InstEmit.Lslv,            typeof(OpCodeAluRs));
            SetA64("x0011010110xxxxx001001xxxxxxxxxx", InstName.Lsrv,            InstEmit.Lsrv,            typeof(OpCodeAluRs));
            SetA64("x0011011000xxxxx0xxxxxxxxxxxxxxx", InstName.Madd,            InstEmit.Madd,            typeof(OpCodeMul));
            SetA64("0111001010xxxxxxxxxxxxxxxxxxxxxx", InstName.Movk,            InstEmit.Movk,            typeof(OpCodeMov));
            SetA64("111100101xxxxxxxxxxxxxxxxxxxxxxx", InstName.Movk,            InstEmit.Movk,            typeof(OpCodeMov));
            SetA64("0001001010xxxxxxxxxxxxxxxxxxxxxx", InstName.Movn,            InstEmit.Movn,            typeof(OpCodeMov));
            SetA64("100100101xxxxxxxxxxxxxxxxxxxxxxx", InstName.Movn,            InstEmit.Movn,            typeof(OpCodeMov));
            SetA64("0101001010xxxxxxxxxxxxxxxxxxxxxx", InstName.Movz,            InstEmit.Movz,            typeof(OpCodeMov));
            SetA64("110100101xxxxxxxxxxxxxxxxxxxxxxx", InstName.Movz,            InstEmit.Movz,            typeof(OpCodeMov));
            SetA64("110101010011xxxxxxxxxxxxxxxxxxxx", InstName.Mrs,             null,                     typeof(OpCodeSystem));
            SetA64("110101010001xxxxxxxxxxxxxxxxxxxx", InstName.Msr,             null,                     typeof(OpCodeSystem));
            SetA64("x0011011000xxxxx1xxxxxxxxxxxxxxx", InstName.Msub,            InstEmit.Msub,            typeof(OpCodeMul));
            SetA64("11010101000000110010000000011111", InstName.Nop,             null,                     typeof(OpCodeSystem));
            SetA64("00101010xx1xxxxx0xxxxxxxxxxxxxxx", InstName.Orn,             InstEmit.Orn,             typeof(OpCodeAluRs));
            SetA64("10101010xx1xxxxxxxxxxxxxxxxxxxxx", InstName.Orn,             InstEmit.Orn,             typeof(OpCodeAluRs));
            SetA64("0011001000xxxxxxxxxxxxxxxxxxxxxx", InstName.Orr,             InstEmit.Orr,             typeof(OpCodeAluImm));
            SetA64("101100100xxxxxxxxxxxxxxxxxxxxxxx", InstName.Orr,             InstEmit.Orr,             typeof(OpCodeAluImm));
            SetA64("00101010xx0xxxxx0xxxxxxxxxxxxxxx", InstName.Orr,             InstEmit.Orr,             typeof(OpCodeAluRs));
            SetA64("10101010xx0xxxxxxxxxxxxxxxxxxxxx", InstName.Orr,             InstEmit.Orr,             typeof(OpCodeAluRs));
            SetA64("1111100110xxxxxxxxxxxxxxxxxxxxxx", InstName.Pfrm,            null,                     typeof(OpCodeMemImm));
            SetA64("11111000100xxxxxxxxx00xxxxxxxxxx", InstName.Pfrm,            null,                     typeof(OpCodeMemImm));
            SetA64("11011000xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Pfrm,            null,                     typeof(OpCodeMemLit));
            SetA64("x101101011000000000000xxxxxxxxxx", InstName.Rbit,            null,                     typeof(OpCodeAlu));
            SetA64("1101011001011111000000xxxxx00000", InstName.Ret,             InstEmit.Ret,             typeof(OpCodeBReg));
            SetA64("x101101011000000000001xxxxxxxxxx", InstName.Rev16,           null,                     typeof(OpCodeAlu));
            SetA64("x101101011000000000010xxxxxxxxxx", InstName.Rev32,           InstEmit.Rev32,           typeof(OpCodeAlu));
            SetA64("1101101011000000000011xxxxxxxxxx", InstName.Rev64,           InstEmit.Rev64,           typeof(OpCodeAlu));
            SetA64("x0011010110xxxxx001011xxxxxxxxxx", InstName.Rorv,            InstEmit.Rorv,            typeof(OpCodeAluRs));
            SetA64("x1011010000xxxxx000000xxxxxxxxxx", InstName.Sbc,             InstEmit.Sbc,             typeof(OpCodeAluRs));
            SetA64("x1111010000xxxxx000000xxxxxxxxxx", InstName.Sbcs,            InstEmit.Sbcs,            typeof(OpCodeAluRs));
            SetA64("00010011000xxxxx0xxxxxxxxxxxxxxx", InstName.Sbfm,            InstEmit.Sbfm,            typeof(OpCodeBfm));
            SetA64("1001001101xxxxxxxxxxxxxxxxxxxxxx", InstName.Sbfm,            InstEmit.Sbfm,            typeof(OpCodeBfm));
            SetA64("x0011010110xxxxx000011xxxxxxxxxx", InstName.Sdiv,            InstEmit.Sdiv,            typeof(OpCodeDiv));
            SetA64("10011011001xxxxx0xxxxxxxxxxxxxxx", InstName.Smaddl,          InstEmit.Smaddl,          typeof(OpCodeMul));
            SetA64("10011011001xxxxx1xxxxxxxxxxxxxxx", InstName.Smsubl,          InstEmit.Smsubl,          typeof(OpCodeMul));
            SetA64("10011011010xxxxx0xxxxxxxxxxxxxxx", InstName.Smulh,           null,                     typeof(OpCodeMul));
            SetA64("xx001000100xxxxx1xxxxxxxxxxxxxxx", InstName.Stlr,            null,                     typeof(OpCodeMemEx));
            SetA64("1x001000001xxxxx1xxxxxxxxxxxxxxx", InstName.Stlxp,           null,                     typeof(OpCodeMemEx));
            SetA64("xx001000000xxxxx1xxxxxxxxxxxxxxx", InstName.Stlxr,           null,                     typeof(OpCodeMemEx));
            SetA64("x010100xx0xxxxxxxxxxxxxxxxxxxxxx", InstName.Stp,             InstEmit.Stp,             typeof(OpCodeMemPair));
            SetA64("xx111000000xxxxxxxxxxxxxxxxxxxxx", InstName.Str,             InstEmit.Str,             typeof(OpCodeMemImm));
            SetA64("xx11100100xxxxxxxxxxxxxxxxxxxxxx", InstName.Str,             InstEmit.Str,             typeof(OpCodeMemImm));
            SetA64("xx111000001xxxxxxxxx10xxxxxxxxxx", InstName.Str,             InstEmit.Str,             typeof(OpCodeMemReg));
            SetA64("1x001000001xxxxx0xxxxxxxxxxxxxxx", InstName.Stxp,            null,                     typeof(OpCodeMemEx));
            SetA64("xx001000000xxxxx0xxxxxxxxxxxxxxx", InstName.Stxr,            null,                     typeof(OpCodeMemEx));
            SetA64("x10100010xxxxxxxxxxxxxxxxxxxxxxx", InstName.Sub,             InstEmit.Sub,             typeof(OpCodeAluImm));
            SetA64("01001011<<0xxxxx0xxxxxxxxxxxxxxx", InstName.Sub,             InstEmit.Sub,             typeof(OpCodeAluRs));
            SetA64("11001011<<0xxxxxxxxxxxxxxxxxxxxx", InstName.Sub,             InstEmit.Sub,             typeof(OpCodeAluRs));
            SetA64("x1001011001xxxxxxxx0xxxxxxxxxxxx", InstName.Sub,             InstEmit.Sub,             typeof(OpCodeAluRx));
            SetA64("x1001011001xxxxxxxx100xxxxxxxxxx", InstName.Sub,             InstEmit.Sub,             typeof(OpCodeAluRx));
            SetA64("x11100010xxxxxxxxxxxxxxxxxxxxxxx", InstName.Subs,            InstEmit.Subs,            typeof(OpCodeAluImm));
            SetA64("01101011<<0xxxxx0xxxxxxxxxxxxxxx", InstName.Subs,            InstEmit.Subs,            typeof(OpCodeAluRs));
            SetA64("11101011<<0xxxxxxxxxxxxxxxxxxxxx", InstName.Subs,            InstEmit.Subs,            typeof(OpCodeAluRs));
            SetA64("x1101011001xxxxxxxx0xxxxxxxxxxxx", InstName.Subs,            InstEmit.Subs,            typeof(OpCodeAluRx));
            SetA64("x1101011001xxxxxxxx100xxxxxxxxxx", InstName.Subs,            InstEmit.Subs,            typeof(OpCodeAluRx));
            SetA64("11010100000xxxxxxxxxxxxxxxx00001", InstName.Svc,             null,                     typeof(OpCodeException));
            SetA64("1101010100001xxxxxxxxxxxxxxxxxxx", InstName.Sys,             null,                     typeof(OpCodeSystem));
            SetA64("x0110111xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Tbnz,            InstEmit.Tbnz,            typeof(OpCodeBImmTest));
            SetA64("x0110110xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Tbz,             InstEmit.Tbz,             typeof(OpCodeBImmTest));
            SetA64("01010011000xxxxx0xxxxxxxxxxxxxxx", InstName.Ubfm,            InstEmit.Ubfm,            typeof(OpCodeBfm));
            SetA64("1101001101xxxxxxxxxxxxxxxxxxxxxx", InstName.Ubfm,            InstEmit.Ubfm,            typeof(OpCodeBfm));
            SetA64("x0011010110xxxxx000010xxxxxxxxxx", InstName.Udiv,            InstEmit.Udiv,            typeof(OpCodeDiv));
            SetA64("10011011101xxxxx0xxxxxxxxxxxxxxx", InstName.Umaddl,          InstEmit.Umaddl,          typeof(OpCodeMul));
            SetA64("10011011101xxxxx1xxxxxxxxxxxxxxx", InstName.Umsubl,          InstEmit.Umsubl,          typeof(OpCodeMul));
            SetA64("10011011110xxxxx0xxxxxxxxxxxxxxx", InstName.Umulh,           null,                     typeof(OpCodeMul));

            //FP & SIMD
            SetA64("0101111011100000101110xxxxxxxxxx", InstName.Abs_S,           null,                     typeof(OpCodeSimd));
            SetA64("0>001110<<100000101110xxxxxxxxxx", InstName.Abs_V,           null,                     typeof(OpCodeSimd));
            SetA64("01011110111xxxxx100001xxxxxxxxxx", InstName.Add_S,           null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<1xxxxx100001xxxxxxxxxx", InstName.Add_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx010000xxxxxxxxxx", InstName.Addhn_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0101111011110001101110xxxxxxxxxx", InstName.Addp_S,          null,                     typeof(OpCodeSimd));
            SetA64("0>001110<<1xxxxx101111xxxxxxxxxx", InstName.Addp_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("000011100x110001101110xxxxxxxxxx", InstName.Addv_V,          null,                     typeof(OpCodeSimd));
            SetA64("01001110<<110001101110xxxxxxxxxx", InstName.Addv_V,          null,                     typeof(OpCodeSimd));
            SetA64("0100111000101000010110xxxxxxxxxx", InstName.Aesd_V,          null,                     typeof(OpCodeSimd));
            SetA64("0100111000101000010010xxxxxxxxxx", InstName.Aese_V,          null,                     typeof(OpCodeSimd));
            SetA64("0100111000101000011110xxxxxxxxxx", InstName.Aesimc_V,        null,                     typeof(OpCodeSimd));
            SetA64("0100111000101000011010xxxxxxxxxx", InstName.Aesmc_V,         null,                     typeof(OpCodeSimd));
            SetA64("0x001110001xxxxx000111xxxxxxxxxx", InstName.And_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110011xxxxx000111xxxxxxxxxx", InstName.Bic_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x10111100000xxx<<x101xxxxxxxxxx", InstName.Bic_Vi,          null,                     typeof(OpCodeSimdImm));
            SetA64("0x101110111xxxxx000111xxxxxxxxxx", InstName.Bif_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110101xxxxx000111xxxxxxxxxx", InstName.Bit_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110011xxxxx000111xxxxxxxxxx", InstName.Bsl_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<100000010010xxxxxxxxxx", InstName.Cls_V,           null,                     typeof(OpCodeSimd));
            SetA64("0x101110<<100000010010xxxxxxxxxx", InstName.Clz_V,           null,                     typeof(OpCodeSimd));
            SetA64("01111110111xxxxx100011xxxxxxxxxx", InstName.Cmeq_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("0101111011100000100110xxxxxxxxxx", InstName.Cmeq_S,          null,                     typeof(OpCodeSimd));
            SetA64("0>101110<<1xxxxx100011xxxxxxxxxx", InstName.Cmeq_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<100000100110xxxxxxxxxx", InstName.Cmeq_V,          null,                     typeof(OpCodeSimd));
            SetA64("01011110111xxxxx001111xxxxxxxxxx", InstName.Cmge_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("0111111011100000100010xxxxxxxxxx", InstName.Cmge_S,          null,                     typeof(OpCodeSimd));
            SetA64("0>001110<<1xxxxx001111xxxxxxxxxx", InstName.Cmge_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>101110<<100000100010xxxxxxxxxx", InstName.Cmge_V,          null,                     typeof(OpCodeSimd));
            SetA64("01011110111xxxxx001101xxxxxxxxxx", InstName.Cmgt_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("0101111011100000100010xxxxxxxxxx", InstName.Cmgt_S,          null,                     typeof(OpCodeSimd));
            SetA64("0>001110<<1xxxxx001101xxxxxxxxxx", InstName.Cmgt_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<100000100010xxxxxxxxxx", InstName.Cmgt_V,          null,                     typeof(OpCodeSimd));
            SetA64("01111110111xxxxx001101xxxxxxxxxx", InstName.Cmhi_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>101110<<1xxxxx001101xxxxxxxxxx", InstName.Cmhi_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("01111110111xxxxx001111xxxxxxxxxx", InstName.Cmhs_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>101110<<1xxxxx001111xxxxxxxxxx", InstName.Cmhs_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0111111011100000100110xxxxxxxxxx", InstName.Cmle_S,          null,                     typeof(OpCodeSimd));
            SetA64("0>101110<<100000100110xxxxxxxxxx", InstName.Cmle_V,          null,                     typeof(OpCodeSimd));
            SetA64("0101111011100000101010xxxxxxxxxx", InstName.Cmlt_S,          null,                     typeof(OpCodeSimd));
            SetA64("0>001110<<100000101010xxxxxxxxxx", InstName.Cmlt_V,          null,                     typeof(OpCodeSimd));
            SetA64("01011110111xxxxx100011xxxxxxxxxx", InstName.Cmtst_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<1xxxxx100011xxxxxxxxxx", InstName.Cmtst_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x00111000100000010110xxxxxxxxxx", InstName.Cnt_V,           null,                     typeof(OpCodeSimd));
            SetA64("0>001110000x<>>>000011xxxxxxxxxx", InstName.Dup_Gp,          null,                     typeof(OpCodeSimdIns));
            SetA64("01011110000xxxxx000001xxxxxxxxxx", InstName.Dup_S,           null,                     typeof(OpCodeSimdIns));
            SetA64("0>001110000x<>>>000001xxxxxxxxxx", InstName.Dup_V,           null,                     typeof(OpCodeSimdIns));
            SetA64("0x101110001xxxxx000111xxxxxxxxxx", InstName.Eor_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0>101110000xxxxx0<xxx0xxxxxxxxxx", InstName.Ext_V,           null,                     typeof(OpCodeSimdExt));
            SetA64("011111101x1xxxxx110101xxxxxxxxxx", InstName.Fabd_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>1011101<1xxxxx110101xxxxxxxxxx", InstName.Fabd_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("000111100x100000110000xxxxxxxxxx", InstName.Fabs_S,          null,                     typeof(OpCodeSimd));
            SetA64("0>0011101<100000111110xxxxxxxxxx", InstName.Fabs_V,          null,                     typeof(OpCodeSimd));
            SetA64("000111100x1xxxxx001010xxxxxxxxxx", InstName.Fadd_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011100<1xxxxx110101xxxxxxxxxx", InstName.Fadd_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("011111100x110000110110xxxxxxxxxx", InstName.Faddp_S,         null,                     typeof(OpCodeSimd));
            SetA64("0>1011100<1xxxxx110101xxxxxxxxxx", InstName.Faddp_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("000111100x1xxxxxxxxx01xxxxx0xxxx", InstName.Fccmp_S,         null,                     typeof(OpCodeSimdFcond));
            SetA64("000111100x1xxxxxxxxx01xxxxx1xxxx", InstName.Fccmpe_S,        null,                     typeof(OpCodeSimdFcond));
            SetA64("010111100x1xxxxx111001xxxxxxxxxx", InstName.Fcmeq_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("010111101x100000110110xxxxxxxxxx", InstName.Fcmeq_S,         null,                     typeof(OpCodeSimd));
            SetA64("0>0011100<1xxxxx111001xxxxxxxxxx", InstName.Fcmeq_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011101<100000110110xxxxxxxxxx", InstName.Fcmeq_V,         null,                     typeof(OpCodeSimd));
            SetA64("011111100x1xxxxx111001xxxxxxxxxx", InstName.Fcmge_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("011111101x100000110010xxxxxxxxxx", InstName.Fcmge_S,         null,                     typeof(OpCodeSimd));
            SetA64("0>1011100<1xxxxx111001xxxxxxxxxx", InstName.Fcmge_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0>1011101<100000110010xxxxxxxxxx", InstName.Fcmge_V,         null,                     typeof(OpCodeSimd));
            SetA64("011111101x1xxxxx111001xxxxxxxxxx", InstName.Fcmgt_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("010111101x100000110010xxxxxxxxxx", InstName.Fcmgt_S,         null,                     typeof(OpCodeSimd));
            SetA64("0>1011101<1xxxxx111001xxxxxxxxxx", InstName.Fcmgt_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011101<100000110010xxxxxxxxxx", InstName.Fcmgt_V,         null,                     typeof(OpCodeSimd));
            SetA64("011111101x100000110110xxxxxxxxxx", InstName.Fcmle_S,         null,                     typeof(OpCodeSimd));
            SetA64("0>1011101<100000110110xxxxxxxxxx", InstName.Fcmle_V,         null,                     typeof(OpCodeSimd));
            SetA64("010111101x100000111010xxxxxxxxxx", InstName.Fcmlt_S,         null,                     typeof(OpCodeSimd));
            SetA64("0>0011101<100000111010xxxxxxxxxx", InstName.Fcmlt_V,         null,                     typeof(OpCodeSimd));
            SetA64("000111100x1xxxxx001000xxxxx0x000", InstName.Fcmp_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("000111100x1xxxxx001000xxxxx1x000", InstName.Fcmpe_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("000111100x1xxxxxxxxx11xxxxxxxxxx", InstName.Fcsel_S,         null,                     typeof(OpCodeSimdFcond));
            SetA64("000111100x10001xx10000xxxxxxxxxx", InstName.Fcvt_S,          null,                     typeof(OpCodeSimd));
            SetA64("x00111100x100100000000xxxxxxxxxx", InstName.Fcvtas_Gp,       null,                     typeof(OpCodeSimdCvt));
            SetA64("x00111100x100101000000xxxxxxxxxx", InstName.Fcvtau_Gp,       null,                     typeof(OpCodeSimdCvt));
            SetA64("0x0011100x100001011110xxxxxxxxxx", InstName.Fcvtl_V,         null,                     typeof(OpCodeSimd));
            SetA64("x00111100x110000000000xxxxxxxxxx", InstName.Fcvtms_Gp,       null,                     typeof(OpCodeSimdCvt));
            SetA64("x00111100x110001000000xxxxxxxxxx", InstName.Fcvtmu_Gp,       null,                     typeof(OpCodeSimdCvt));
            SetA64("0x0011100x100001011010xxxxxxxxxx", InstName.Fcvtn_V,         null,                     typeof(OpCodeSimd));
            SetA64("010111100x100001101010xxxxxxxxxx", InstName.Fcvtns_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>0011100<100001101010xxxxxxxxxx", InstName.Fcvtns_V,        null,                     typeof(OpCodeSimd));
            SetA64("011111100x100001101010xxxxxxxxxx", InstName.Fcvtnu_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>1011100<100001101010xxxxxxxxxx", InstName.Fcvtnu_V,        null,                     typeof(OpCodeSimd));
            SetA64("x00111100x101000000000xxxxxxxxxx", InstName.Fcvtps_Gp,       null,                     typeof(OpCodeSimdCvt));
            SetA64("x00111100x101001000000xxxxxxxxxx", InstName.Fcvtpu_Gp,       null,                     typeof(OpCodeSimdCvt));
            SetA64("x00111100x111000000000xxxxxxxxxx", InstName.Fcvtzs_Gp,       null,                     typeof(OpCodeSimdCvt));
            SetA64(">00111100x011000>xxxxxxxxxxxxxxx", InstName.Fcvtzs_Gp_Fixed, null,                     typeof(OpCodeSimdCvt));
            SetA64("010111101x100001101110xxxxxxxxxx", InstName.Fcvtzs_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>0011101<100001101110xxxxxxxxxx", InstName.Fcvtzs_V,        null,                     typeof(OpCodeSimd));
            SetA64("0x001111001xxxxx111111xxxxxxxxxx", InstName.Fcvtzs_V_Fixed,  null,                     typeof(OpCodeSimdShImm));
            SetA64("0100111101xxxxxx111111xxxxxxxxxx", InstName.Fcvtzs_V_Fixed,  null,                     typeof(OpCodeSimdShImm));
            SetA64("x00111100x111001000000xxxxxxxxxx", InstName.Fcvtzu_Gp,       null,                     typeof(OpCodeSimdCvt));
            SetA64(">00111100x011001>xxxxxxxxxxxxxxx", InstName.Fcvtzu_Gp_Fixed, null,                     typeof(OpCodeSimdCvt));
            SetA64("011111101x100001101110xxxxxxxxxx", InstName.Fcvtzu_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>1011101<100001101110xxxxxxxxxx", InstName.Fcvtzu_V,        null,                     typeof(OpCodeSimd));
            SetA64("0x101111001xxxxx111111xxxxxxxxxx", InstName.Fcvtzu_V_Fixed,  null,                     typeof(OpCodeSimdShImm));
            SetA64("0110111101xxxxxx111111xxxxxxxxxx", InstName.Fcvtzu_V_Fixed,  null,                     typeof(OpCodeSimdShImm));
            SetA64("000111100x1xxxxx000110xxxxxxxxxx", InstName.Fdiv_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>1011100<1xxxxx111111xxxxxxxxxx", InstName.Fdiv_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("000111110x0xxxxx0xxxxxxxxxxxxxxx", InstName.Fmadd_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("000111100x1xxxxx010010xxxxxxxxxx", InstName.Fmax_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011100<1xxxxx111101xxxxxxxxxx", InstName.Fmax_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("000111100x1xxxxx011010xxxxxxxxxx", InstName.Fmaxnm_S,        null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011100<1xxxxx110001xxxxxxxxxx", InstName.Fmaxnm_V,        null,                     typeof(OpCodeSimdReg));
            SetA64("0>1011100<1xxxxx111101xxxxxxxxxx", InstName.Fmaxp_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("000111100x1xxxxx010110xxxxxxxxxx", InstName.Fmin_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011101<1xxxxx111101xxxxxxxxxx", InstName.Fmin_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("000111100x1xxxxx011110xxxxxxxxxx", InstName.Fminnm_S,        null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011101<1xxxxx110001xxxxxxxxxx", InstName.Fminnm_V,        null,                     typeof(OpCodeSimdReg));
            SetA64("0>1011101<1xxxxx111101xxxxxxxxxx", InstName.Fminp_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("010111111xxxxxxx0001x0xxxxxxxxxx", InstName.Fmla_Se,         null,                     typeof(OpCodeSimdRegElemF));
            SetA64("0>0011100<1xxxxx110011xxxxxxxxxx", InstName.Fmla_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011111<xxxxxx0001x0xxxxxxxxxx", InstName.Fmla_Ve,         null,                     typeof(OpCodeSimdRegElemF));
            SetA64("010111111xxxxxxx0101x0xxxxxxxxxx", InstName.Fmls_Se,         null,                     typeof(OpCodeSimdRegElemF));
            SetA64("0>0011101<1xxxxx110011xxxxxxxxxx", InstName.Fmls_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011111<xxxxxx0101x0xxxxxxxxxx", InstName.Fmls_Ve,         null,                     typeof(OpCodeSimdRegElemF));
            SetA64("000111100x100000010000xxxxxxxxxx", InstName.Fmov_S,          null,                     typeof(OpCodeSimd));
            SetA64("00011110xx1xxxxxxxx100xxxxxxxxxx", InstName.Fmov_Si,         null,                     typeof(OpCodeSimdFmov));
            SetA64("0xx0111100000xxx111101xxxxxxxxxx", InstName.Fmov_V,          null,                     typeof(OpCodeSimdImm));
            SetA64("x00111100x100110000000xxxxxxxxxx", InstName.Fmov_Ftoi,       null,                     typeof(OpCodeSimdCvt));
            SetA64("x00111100x100111000000xxxxxxxxxx", InstName.Fmov_Itof,       null,                     typeof(OpCodeSimdCvt));
            SetA64("1001111010101110000000xxxxxxxxxx", InstName.Fmov_Ftoi1,      null,                     typeof(OpCodeSimdCvt));
            SetA64("1001111010101111000000xxxxxxxxxx", InstName.Fmov_Itof1,      null,                     typeof(OpCodeSimdCvt));
            SetA64("000111110x0xxxxx1xxxxxxxxxxxxxxx", InstName.Fmsub_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("000111100x1xxxxx000010xxxxxxxxxx", InstName.Fmul_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("010111111xxxxxxx1001x0xxxxxxxxxx", InstName.Fmul_Se,         null,                     typeof(OpCodeSimdRegElemF));
            SetA64("0>1011100<1xxxxx110111xxxxxxxxxx", InstName.Fmul_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011111<xxxxxx1001x0xxxxxxxxxx", InstName.Fmul_Ve,         null,                     typeof(OpCodeSimdRegElemF));
            SetA64("010111100x1xxxxx110111xxxxxxxxxx", InstName.Fmulx_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("011111111xxxxxxx1001x0xxxxxxxxxx", InstName.Fmulx_Se,        null,                     typeof(OpCodeSimdRegElemF));
            SetA64("0>0011100<1xxxxx110111xxxxxxxxxx", InstName.Fmulx_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0>1011111<xxxxxx1001x0xxxxxxxxxx", InstName.Fmulx_Ve,        null,                     typeof(OpCodeSimdRegElemF));
            SetA64("000111100x100001010000xxxxxxxxxx", InstName.Fneg_S,          null,                     typeof(OpCodeSimd));
            SetA64("0>1011101<100000111110xxxxxxxxxx", InstName.Fneg_V,          null,                     typeof(OpCodeSimd));
            SetA64("000111110x1xxxxx0xxxxxxxxxxxxxxx", InstName.Fnmadd_S,        null,                     typeof(OpCodeSimdReg));
            SetA64("000111110x1xxxxx1xxxxxxxxxxxxxxx", InstName.Fnmsub_S,        null,                     typeof(OpCodeSimdReg));
            SetA64("000111100x1xxxxx100010xxxxxxxxxx", InstName.Fnmul_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("010111101x100001110110xxxxxxxxxx", InstName.Frecpe_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>0011101<100001110110xxxxxxxxxx", InstName.Frecpe_V,        null,                     typeof(OpCodeSimd));
            SetA64("010111100x1xxxxx111111xxxxxxxxxx", InstName.Frecps_S,        null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011100<1xxxxx111111xxxxxxxxxx", InstName.Frecps_V,        null,                     typeof(OpCodeSimdReg));
            SetA64("010111101x100001111110xxxxxxxxxx", InstName.Frecpx_S,        null,                     typeof(OpCodeSimd));
            SetA64("000111100x100110010000xxxxxxxxxx", InstName.Frinta_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>1011100<100001100010xxxxxxxxxx", InstName.Frinta_V,        null,                     typeof(OpCodeSimd));
            SetA64("000111100x100111110000xxxxxxxxxx", InstName.Frinti_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>1011101<100001100110xxxxxxxxxx", InstName.Frinti_V,        null,                     typeof(OpCodeSimd));
            SetA64("000111100x100101010000xxxxxxxxxx", InstName.Frintm_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>0011100<100001100110xxxxxxxxxx", InstName.Frintm_V,        null,                     typeof(OpCodeSimd));
            SetA64("000111100x100100010000xxxxxxxxxx", InstName.Frintn_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>0011100<100001100010xxxxxxxxxx", InstName.Frintn_V,        null,                     typeof(OpCodeSimd));
            SetA64("000111100x100100110000xxxxxxxxxx", InstName.Frintp_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>0011101<100001100010xxxxxxxxxx", InstName.Frintp_V,        null,                     typeof(OpCodeSimd));
            SetA64("000111100x100111010000xxxxxxxxxx", InstName.Frintx_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>1011100<100001100110xxxxxxxxxx", InstName.Frintx_V,        null,                     typeof(OpCodeSimd));
            SetA64("000111100x100101110000xxxxxxxxxx", InstName.Frintz_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>0011101<100001100110xxxxxxxxxx", InstName.Frintz_V,        null,                     typeof(OpCodeSimd));
            SetA64("011111101x100001110110xxxxxxxxxx", InstName.Frsqrte_S,       null,                     typeof(OpCodeSimd));
            SetA64("0>1011101<100001110110xxxxxxxxxx", InstName.Frsqrte_V,       null,                     typeof(OpCodeSimd));
            SetA64("010111101x1xxxxx111111xxxxxxxxxx", InstName.Frsqrts_S,       null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011101<1xxxxx111111xxxxxxxxxx", InstName.Frsqrts_V,       null,                     typeof(OpCodeSimdReg));
            SetA64("000111100x100001110000xxxxxxxxxx", InstName.Fsqrt_S,         null,                     typeof(OpCodeSimd));
            SetA64("0>1011101<100001111110xxxxxxxxxx", InstName.Fsqrt_V,         null,                     typeof(OpCodeSimd));
            SetA64("000111100x1xxxxx001110xxxxxxxxxx", InstName.Fsub_S,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>0011101<1xxxxx110101xxxxxxxxxx", InstName.Fsub_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("01001110000xxxxx000111xxxxxxxxxx", InstName.Ins_Gp,          null,                     typeof(OpCodeSimdIns));
            SetA64("01101110000xxxxx0xxxx1xxxxxxxxxx", InstName.Ins_V,           null,                     typeof(OpCodeSimdIns));
            SetA64("0x00110001000000xxxxxxxxxxxxxxxx", InstName.Ld__Vms,         null,                     typeof(OpCodeSimdMemMs));
            SetA64("0x001100110xxxxxxxxxxxxxxxxxxxxx", InstName.Ld__Vms,         null,                     typeof(OpCodeSimdMemMs));
            SetA64("0x00110101x00000xxxxxxxxxxxxxxxx", InstName.Ld__Vss,         null,                     typeof(OpCodeSimdMemSs));
            SetA64("0x00110111xxxxxxxxxxxxxxxxxxxxxx", InstName.Ld__Vss,         null,                     typeof(OpCodeSimdMemSs));
            SetA64("xx10110xx1xxxxxxxxxxxxxxxxxxxxxx", InstName.Ldp,             null,                     typeof(OpCodeSimdMemPair));
            SetA64("xx111100x10xxxxxxxxx00xxxxxxxxxx", InstName.Ldr,             null,                     typeof(OpCodeSimdMemImm));
            SetA64("xx111100x10xxxxxxxxx01xxxxxxxxxx", InstName.Ldr,             null,                     typeof(OpCodeSimdMemImm));
            SetA64("xx111100x10xxxxxxxxx11xxxxxxxxxx", InstName.Ldr,             null,                     typeof(OpCodeSimdMemImm));
            SetA64("xx111101x1xxxxxxxxxxxxxxxxxxxxxx", InstName.Ldr,             null,                     typeof(OpCodeSimdMemImm));
            SetA64("xx111100x11xxxxxxxxx10xxxxxxxxxx", InstName.Ldr,             null,                     typeof(OpCodeSimdMemReg));
            SetA64("xx011100xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Ldr_Literal,     null,                     typeof(OpCodeSimdMemLit));
            SetA64("0x001110<<1xxxxx100101xxxxxxxxxx", InstName.Mla_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x101111xxxxxxxx0000x0xxxxxxxxxx", InstName.Mla_Ve,          null,                     typeof(OpCodeSimdRegElem));
            SetA64("0x101110<<1xxxxx100101xxxxxxxxxx", InstName.Mls_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x101111xxxxxxxx0100x0xxxxxxxxxx", InstName.Mls_Ve,          null,                     typeof(OpCodeSimdRegElem));
            SetA64("0x00111100000xxx0xx001xxxxxxxxxx", InstName.Movi_V,          null,                     typeof(OpCodeSimdImm));
            SetA64("0x00111100000xxx10x001xxxxxxxxxx", InstName.Movi_V,          null,                     typeof(OpCodeSimdImm));
            SetA64("0x00111100000xxx110x01xxxxxxxxxx", InstName.Movi_V,          null,                     typeof(OpCodeSimdImm));
            SetA64("0xx0111100000xxx111001xxxxxxxxxx", InstName.Movi_V,          null,                     typeof(OpCodeSimdImm));
            SetA64("0x001110<<1xxxxx100111xxxxxxxxxx", InstName.Mul_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x001111xxxxxxxx1000x0xxxxxxxxxx", InstName.Mul_Ve,          null,                     typeof(OpCodeSimdRegElem));
            SetA64("0x10111100000xxx0xx001xxxxxxxxxx", InstName.Mvni_V,          null,                     typeof(OpCodeSimdImm));
            SetA64("0x10111100000xxx10x001xxxxxxxxxx", InstName.Mvni_V,          null,                     typeof(OpCodeSimdImm));
            SetA64("0x10111100000xxx110x01xxxxxxxxxx", InstName.Mvni_V,          null,                     typeof(OpCodeSimdImm));
            SetA64("0111111011100000101110xxxxxxxxxx", InstName.Neg_S,           null,                     typeof(OpCodeSimd));
            SetA64("0>101110<<100000101110xxxxxxxxxx", InstName.Neg_V,           null,                     typeof(OpCodeSimd));
            SetA64("0x10111000100000010110xxxxxxxxxx", InstName.Not_V,           null,                     typeof(OpCodeSimd));
            SetA64("0x001110111xxxxx000111xxxxxxxxxx", InstName.Orn_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110101xxxxx000111xxxxxxxxxx", InstName.Orr_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x00111100000xxx<<x101xxxxxxxxxx", InstName.Orr_Vi,          null,                     typeof(OpCodeSimdImm));
            SetA64("0x101110<<1xxxxx010000xxxxxxxxxx", InstName.Raddhn_V,        null,                     typeof(OpCodeSimdReg));
            SetA64("0x10111001100000010110xxxxxxxxxx", InstName.Rbit_V,          null,                     typeof(OpCodeSimd));
            SetA64("0x00111000100000000110xxxxxxxxxx", InstName.Rev16_V,         null,                     typeof(OpCodeSimd));
            SetA64("0x1011100x100000000010xxxxxxxxxx", InstName.Rev32_V,         null,                     typeof(OpCodeSimd));
            SetA64("0x001110<<100000000010xxxxxxxxxx", InstName.Rev64_V,         null,                     typeof(OpCodeSimd));
            SetA64("0x00111100>>>xxx100011xxxxxxxxxx", InstName.Rshrn_V,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0x101110<<1xxxxx011000xxxxxxxxxx", InstName.Rsubhn_V,        null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx011111xxxxxxxxxx", InstName.Saba_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx010100xxxxxxxxxx", InstName.Sabal_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx011101xxxxxxxxxx", InstName.Sabd_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx011100xxxxxxxxxx", InstName.Sabdl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<100000011010xxxxxxxxxx", InstName.Sadalp_V,        null,                     typeof(OpCodeSimd));
            SetA64("0x001110<<1xxxxx000000xxxxxxxxxx", InstName.Saddl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<100000001010xxxxxxxxxx", InstName.Saddlp_V,        null,                     typeof(OpCodeSimd));
            SetA64("0x001110<<1xxxxx000100xxxxxxxxxx", InstName.Saddw_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("x00111100x100010000000xxxxxxxxxx", InstName.Scvtf_Gp,        null,                     typeof(OpCodeSimdCvt));
            SetA64(">00111100x000010>xxxxxxxxxxxxxxx", InstName.Scvtf_Gp_Fixed,  null,                     typeof(OpCodeSimdCvt));
            SetA64("010111100x100001110110xxxxxxxxxx", InstName.Scvtf_S,         null,                     typeof(OpCodeSimd));
            SetA64("0>0011100<100001110110xxxxxxxxxx", InstName.Scvtf_V,         null,                     typeof(OpCodeSimd));
            SetA64("01011110000xxxxx000000xxxxxxxxxx", InstName.Sha1c_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0101111000101000000010xxxxxxxxxx", InstName.Sha1h_V,         null,                     typeof(OpCodeSimd));
            SetA64("01011110000xxxxx001000xxxxxxxxxx", InstName.Sha1m_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("01011110000xxxxx000100xxxxxxxxxx", InstName.Sha1p_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("01011110000xxxxx001100xxxxxxxxxx", InstName.Sha1su0_V,       null,                     typeof(OpCodeSimdReg));
            SetA64("0101111000101000000110xxxxxxxxxx", InstName.Sha1su1_V,       null,                     typeof(OpCodeSimd));
            SetA64("01011110000xxxxx010000xxxxxxxxxx", InstName.Sha256h_V,       null,                     typeof(OpCodeSimdReg));
            SetA64("01011110000xxxxx010100xxxxxxxxxx", InstName.Sha256h2_V,      null,                     typeof(OpCodeSimdReg));
            SetA64("0101111000101000001010xxxxxxxxxx", InstName.Sha256su0_V,     null,                     typeof(OpCodeSimd));
            SetA64("01011110000xxxxx011000xxxxxxxxxx", InstName.Sha256su1_V,     null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx000001xxxxxxxxxx", InstName.Shadd_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0101111101xxxxxx010101xxxxxxxxxx", InstName.Shl_S,           null,                     typeof(OpCodeSimdShImm));
            SetA64("0x00111100>>>xxx010101xxxxxxxxxx", InstName.Shl_V,           null,                     typeof(OpCodeSimdShImm));
            SetA64("0100111101xxxxxx010101xxxxxxxxxx", InstName.Shl_V,           null,                     typeof(OpCodeSimdShImm));
            SetA64("0x101110<<100001001110xxxxxxxxxx", InstName.Shll_V,          null,                     typeof(OpCodeSimd));
            SetA64("0x00111100>>>xxx100001xxxxxxxxxx", InstName.Shrn_V,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0x001110<<1xxxxx001001xxxxxxxxxx", InstName.Shsub_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x1011110>>>>xxx010101xxxxxxxxxx", InstName.Sli_V,           null,                     typeof(OpCodeSimdShImm));
            SetA64("0x001110<<1xxxxx011001xxxxxxxxxx", InstName.Smax_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx101001xxxxxxxxxx", InstName.Smaxp_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx011011xxxxxxxxxx", InstName.Smin_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx101011xxxxxxxxxx", InstName.Sminp_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx100000xxxxxxxxxx", InstName.Smlal_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x001111xxxxxxxx0010x0xxxxxxxxxx", InstName.Smlal_Ve,        null,                     typeof(OpCodeSimdRegElem));
            SetA64("0x001110<<1xxxxx101000xxxxxxxxxx", InstName.Smlsl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x001111xxxxxxxx0110x0xxxxxxxxxx", InstName.Smlsl_Ve,        null,                     typeof(OpCodeSimdRegElem));
            SetA64("0x001110000xxxxx001011xxxxxxxxxx", InstName.Smov_S,          null,                     typeof(OpCodeSimdIns));
            SetA64("0x001110<<1xxxxx110000xxxxxxxxxx", InstName.Smull_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x001111xxxxxxxx1010x0xxxxxxxxxx", InstName.Smull_Ve,        null,                     typeof(OpCodeSimdRegElem));
            SetA64("01011110xx100000011110xxxxxxxxxx", InstName.Sqabs_S,         null,                     typeof(OpCodeSimd));
            SetA64("0>001110<<100000011110xxxxxxxxxx", InstName.Sqabs_V,         null,                     typeof(OpCodeSimd));
            SetA64("01011110xx1xxxxx000011xxxxxxxxxx", InstName.Sqadd_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<1xxxxx000011xxxxxxxxxx", InstName.Sqadd_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("01011110011xxxxx101101xxxxxxxxxx", InstName.Sqdmulh_S,       null,                     typeof(OpCodeSimdReg));
            SetA64("01011110101xxxxx101101xxxxxxxxxx", InstName.Sqdmulh_S,       null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110011xxxxx101101xxxxxxxxxx", InstName.Sqdmulh_V,       null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110101xxxxx101101xxxxxxxxxx", InstName.Sqdmulh_V,       null,                     typeof(OpCodeSimdReg));
            SetA64("01111110xx100000011110xxxxxxxxxx", InstName.Sqneg_S,         null,                     typeof(OpCodeSimd));
            SetA64("0>101110<<100000011110xxxxxxxxxx", InstName.Sqneg_V,         null,                     typeof(OpCodeSimd));
            SetA64("01111110011xxxxx101101xxxxxxxxxx", InstName.Sqrdmulh_S,      null,                     typeof(OpCodeSimdReg));
            SetA64("01111110101xxxxx101101xxxxxxxxxx", InstName.Sqrdmulh_S,      null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110011xxxxx101101xxxxxxxxxx", InstName.Sqrdmulh_V,      null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110101xxxxx101101xxxxxxxxxx", InstName.Sqrdmulh_V,      null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<1xxxxx010111xxxxxxxxxx", InstName.Sqrshl_V,        null,                     typeof(OpCodeSimdReg));
            SetA64("0101111100>>>xxx100111xxxxxxxxxx", InstName.Sqrshrn_S,       null,                     typeof(OpCodeSimdShImm));
            SetA64("0x00111100>>>xxx100111xxxxxxxxxx", InstName.Sqrshrn_V,       null,                     typeof(OpCodeSimdShImm));
            SetA64("0111111100>>>xxx100011xxxxxxxxxx", InstName.Sqrshrun_S,      null,                     typeof(OpCodeSimdShImm));
            SetA64("0x10111100>>>xxx100011xxxxxxxxxx", InstName.Sqrshrun_V,      null,                     typeof(OpCodeSimdShImm));
            SetA64("0>001110<<1xxxxx010011xxxxxxxxxx", InstName.Sqshl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0101111100>>>xxx100101xxxxxxxxxx", InstName.Sqshrn_S,        null,                     typeof(OpCodeSimdShImm));
            SetA64("0x00111100>>>xxx100101xxxxxxxxxx", InstName.Sqshrn_V,        null,                     typeof(OpCodeSimdShImm));
            SetA64("0111111100>>>xxx100001xxxxxxxxxx", InstName.Sqshrun_S,       null,                     typeof(OpCodeSimdShImm));
            SetA64("0x10111100>>>xxx100001xxxxxxxxxx", InstName.Sqshrun_V,       null,                     typeof(OpCodeSimdShImm));
            SetA64("01011110xx1xxxxx001011xxxxxxxxxx", InstName.Sqsub_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<1xxxxx001011xxxxxxxxxx", InstName.Sqsub_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("01011110<<100001010010xxxxxxxxxx", InstName.Sqxtn_S,         null,                     typeof(OpCodeSimd));
            SetA64("0x001110<<100001010010xxxxxxxxxx", InstName.Sqxtn_V,         null,                     typeof(OpCodeSimd));
            SetA64("01111110<<100001001010xxxxxxxxxx", InstName.Sqxtun_S,        null,                     typeof(OpCodeSimd));
            SetA64("0x101110<<100001001010xxxxxxxxxx", InstName.Sqxtun_V,        null,                     typeof(OpCodeSimd));
            SetA64("0x001110<<1xxxxx000101xxxxxxxxxx", InstName.Srhadd_V,        null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<1xxxxx010101xxxxxxxxxx", InstName.Srshl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0101111101xxxxxx001001xxxxxxxxxx", InstName.Srshr_S,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0x00111100>>>xxx001001xxxxxxxxxx", InstName.Srshr_V,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0100111101xxxxxx001001xxxxxxxxxx", InstName.Srshr_V,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0101111101xxxxxx001101xxxxxxxxxx", InstName.Srsra_S,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0x00111100>>>xxx001101xxxxxxxxxx", InstName.Srsra_V,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0100111101xxxxxx001101xxxxxxxxxx", InstName.Srsra_V,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0>001110<<1xxxxx010001xxxxxxxxxx", InstName.Sshl_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x00111100>>>xxx101001xxxxxxxxxx", InstName.Sshll_V,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0101111101xxxxxx000001xxxxxxxxxx", InstName.Sshr_S,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0x00111100>>>xxx000001xxxxxxxxxx", InstName.Sshr_V,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0100111101xxxxxx000001xxxxxxxxxx", InstName.Sshr_V,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0101111101xxxxxx000101xxxxxxxxxx", InstName.Ssra_S,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0x00111100>>>xxx000101xxxxxxxxxx", InstName.Ssra_V,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0100111101xxxxxx000101xxxxxxxxxx", InstName.Ssra_V,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0x001110<<1xxxxx001000xxxxxxxxxx", InstName.Ssubl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx001100xxxxxxxxxx", InstName.Ssubw_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x00110000000000xxxxxxxxxxxxxxxx", InstName.St__Vms,         null,                     typeof(OpCodeSimdMemMs));
            SetA64("0x001100100xxxxxxxxxxxxxxxxxxxxx", InstName.St__Vms,         null,                     typeof(OpCodeSimdMemMs));
            SetA64("0x00110100x00000xxxxxxxxxxxxxxxx", InstName.St__Vss,         null,                     typeof(OpCodeSimdMemSs));
            SetA64("0x00110110xxxxxxxxxxxxxxxxxxxxxx", InstName.St__Vss,         null,                     typeof(OpCodeSimdMemSs));
            SetA64("xx10110xx0xxxxxxxxxxxxxxxxxxxxxx", InstName.Stp,             null,                     typeof(OpCodeSimdMemPair));
            SetA64("xx111100x00xxxxxxxxx00xxxxxxxxxx", InstName.Str,             null,                     typeof(OpCodeSimdMemImm));
            SetA64("xx111100x00xxxxxxxxx01xxxxxxxxxx", InstName.Str,             null,                     typeof(OpCodeSimdMemImm));
            SetA64("xx111100x00xxxxxxxxx11xxxxxxxxxx", InstName.Str,             null,                     typeof(OpCodeSimdMemImm));
            SetA64("xx111101x0xxxxxxxxxxxxxxxxxxxxxx", InstName.Str,             null,                     typeof(OpCodeSimdMemImm));
            SetA64("xx111100x01xxxxxxxxx10xxxxxxxxxx", InstName.Str,             null,                     typeof(OpCodeSimdMemReg));
            SetA64("01111110111xxxxx100001xxxxxxxxxx", InstName.Sub_S,           null,                     typeof(OpCodeSimdReg));
            SetA64("0>101110<<1xxxxx100001xxxxxxxxxx", InstName.Sub_V,           null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<1xxxxx011000xxxxxxxxxx", InstName.Subhn_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("01011110xx100000001110xxxxxxxxxx", InstName.Suqadd_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>001110<<100000001110xxxxxxxxxx", InstName.Suqadd_V,        null,                     typeof(OpCodeSimd));
            SetA64("0x001110000xxxxx0xx000xxxxxxxxxx", InstName.Tbl_V,           null,                     typeof(OpCodeSimdTbl));
            SetA64("0>001110<<0xxxxx001010xxxxxxxxxx", InstName.Trn1_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<0xxxxx011010xxxxxxxxxx", InstName.Trn2_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<1xxxxx011111xxxxxxxxxx", InstName.Uaba_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<1xxxxx010100xxxxxxxxxx", InstName.Uabal_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<1xxxxx011101xxxxxxxxxx", InstName.Uabd_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<1xxxxx011100xxxxxxxxxx", InstName.Uabdl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<100000011010xxxxxxxxxx", InstName.Uadalp_V,        null,                     typeof(OpCodeSimd));
            SetA64("0x101110<<1xxxxx000000xxxxxxxxxx", InstName.Uaddl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<100000001010xxxxxxxxxx", InstName.Uaddlp_V,        null,                     typeof(OpCodeSimd));
            SetA64("001011100x110000001110xxxxxxxxxx", InstName.Uaddlv_V,        null,                     typeof(OpCodeSimd));
            SetA64("01101110<<110000001110xxxxxxxxxx", InstName.Uaddlv_V,        null,                     typeof(OpCodeSimd));
            SetA64("0x101110<<1xxxxx000100xxxxxxxxxx", InstName.Uaddw_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("x00111100x100011000000xxxxxxxxxx", InstName.Ucvtf_Gp,        null,                     typeof(OpCodeSimdCvt));
            SetA64(">00111100x000011>xxxxxxxxxxxxxxx", InstName.Ucvtf_Gp_Fixed,  null,                     typeof(OpCodeSimdCvt));
            SetA64("011111100x100001110110xxxxxxxxxx", InstName.Ucvtf_S,         null,                     typeof(OpCodeSimd));
            SetA64("0>1011100<100001110110xxxxxxxxxx", InstName.Ucvtf_V,         null,                     typeof(OpCodeSimd));
            SetA64("0x101110<<1xxxxx000001xxxxxxxxxx", InstName.Uhadd_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<1xxxxx001001xxxxxxxxxx", InstName.Uhsub_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<1xxxxx011001xxxxxxxxxx", InstName.Umax_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<1xxxxx101001xxxxxxxxxx", InstName.Umaxp_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<1xxxxx011011xxxxxxxxxx", InstName.Umin_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<1xxxxx101011xxxxxxxxxx", InstName.Uminp_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<1xxxxx100000xxxxxxxxxx", InstName.Umlal_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x101111xxxxxxxx0010x0xxxxxxxxxx", InstName.Umlal_Ve,        null,                     typeof(OpCodeSimdRegElem));
            SetA64("0x101110<<1xxxxx101000xxxxxxxxxx", InstName.Umlsl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x101111xxxxxxxx0110x0xxxxxxxxxx", InstName.Umlsl_Ve,        null,                     typeof(OpCodeSimdRegElem));
            SetA64("0x001110000xxxxx001111xxxxxxxxxx", InstName.Umov_S,          null,                     typeof(OpCodeSimdIns));
            SetA64("0x101110<<1xxxxx110000xxxxxxxxxx", InstName.Umull_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x101111xxxxxxxx1010x0xxxxxxxxxx", InstName.Umull_Ve,        null,                     typeof(OpCodeSimdRegElem));
            SetA64("01111110xx1xxxxx000011xxxxxxxxxx", InstName.Uqadd_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("0>101110<<1xxxxx000011xxxxxxxxxx", InstName.Uqadd_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0>101110<<1xxxxx010111xxxxxxxxxx", InstName.Uqrshl_V,        null,                     typeof(OpCodeSimdReg));
            SetA64("0111111100>>>xxx100111xxxxxxxxxx", InstName.Uqrshrn_S,       null,                     typeof(OpCodeSimdShImm));
            SetA64("0x10111100>>>xxx100111xxxxxxxxxx", InstName.Uqrshrn_V,       null,                     typeof(OpCodeSimdShImm));
            SetA64("0>101110<<1xxxxx010011xxxxxxxxxx", InstName.Uqshl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0111111100>>>xxx100101xxxxxxxxxx", InstName.Uqshrn_S,        null,                     typeof(OpCodeSimdShImm));
            SetA64("0x10111100>>>xxx100101xxxxxxxxxx", InstName.Uqshrn_V,        null,                     typeof(OpCodeSimdShImm));
            SetA64("01111110xx1xxxxx001011xxxxxxxxxx", InstName.Uqsub_S,         null,                     typeof(OpCodeSimdReg));
            SetA64("0>101110<<1xxxxx001011xxxxxxxxxx", InstName.Uqsub_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("01111110<<100001010010xxxxxxxxxx", InstName.Uqxtn_S,         null,                     typeof(OpCodeSimd));
            SetA64("0x101110<<100001010010xxxxxxxxxx", InstName.Uqxtn_V,         null,                     typeof(OpCodeSimd));
            SetA64("0x101110<<1xxxxx000101xxxxxxxxxx", InstName.Urhadd_V,        null,                     typeof(OpCodeSimdReg));
            SetA64("0>101110<<1xxxxx010101xxxxxxxxxx", InstName.Urshl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0111111101xxxxxx001001xxxxxxxxxx", InstName.Urshr_S,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0x10111100>>>xxx001001xxxxxxxxxx", InstName.Urshr_V,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0110111101xxxxxx001001xxxxxxxxxx", InstName.Urshr_V,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0111111101xxxxxx001101xxxxxxxxxx", InstName.Ursra_S,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0x10111100>>>xxx001101xxxxxxxxxx", InstName.Ursra_V,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0110111101xxxxxx001101xxxxxxxxxx", InstName.Ursra_V,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0>101110<<1xxxxx010001xxxxxxxxxx", InstName.Ushl_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x10111100>>>xxx101001xxxxxxxxxx", InstName.Ushll_V,         null,                     typeof(OpCodeSimdShImm));
            SetA64("0111111101xxxxxx000001xxxxxxxxxx", InstName.Ushr_S,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0x10111100>>>xxx000001xxxxxxxxxx", InstName.Ushr_V,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0110111101xxxxxx000001xxxxxxxxxx", InstName.Ushr_V,          null,                     typeof(OpCodeSimdShImm));
            SetA64("01111110xx100000001110xxxxxxxxxx", InstName.Usqadd_S,        null,                     typeof(OpCodeSimd));
            SetA64("0>101110<<100000001110xxxxxxxxxx", InstName.Usqadd_V,        null,                     typeof(OpCodeSimd));
            SetA64("0111111101xxxxxx000101xxxxxxxxxx", InstName.Usra_S,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0x10111100>>>xxx000101xxxxxxxxxx", InstName.Usra_V,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0110111101xxxxxx000101xxxxxxxxxx", InstName.Usra_V,          null,                     typeof(OpCodeSimdShImm));
            SetA64("0x101110<<1xxxxx001000xxxxxxxxxx", InstName.Usubl_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0x101110<<1xxxxx001100xxxxxxxxxx", InstName.Usubw_V,         null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<0xxxxx000110xxxxxxxxxx", InstName.Uzp1_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<0xxxxx010110xxxxxxxxxx", InstName.Uzp2_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0x001110<<100001001010xxxxxxxxxx", InstName.Xtn_V,           null,                     typeof(OpCodeSimd));
            SetA64("0>001110<<0xxxxx001110xxxxxxxxxx", InstName.Zip1_V,          null,                     typeof(OpCodeSimdReg));
            SetA64("0>001110<<0xxxxx011110xxxxxxxxxx", InstName.Zip2_V,          null,                     typeof(OpCodeSimdReg));
#endregion

#region "OpCode Table (AArch32)"
            //Base
            SetA32("<<<<0010100xxxxxxxxxxxxxxxxxxxxx", InstName.Add,             null,                     typeof(OpCode32AluImm));
            SetA32("<<<<0000100xxxxxxxxxxxxxxxx0xxxx", InstName.Add,             null,                     typeof(OpCode32AluRsImm));
            SetA32("<<<<1010xxxxxxxxxxxxxxxxxxxxxxxx", InstName.B,               null,                     typeof(OpCode32BImm));
            SetA32("<<<<1011xxxxxxxxxxxxxxxxxxxxxxxx", InstName.Bl,              null,                     typeof(OpCode32BImm));
            SetA32("1111101xxxxxxxxxxxxxxxxxxxxxxxxx", InstName.Blx,             null,                     typeof(OpCode32BImm));
            SetA32("<<<<000100101111111111110001xxxx", InstName.Bx,              null,                     typeof(OpCode32BReg));
            SetT32("xxxxxxxxxxxxxxxx010001110xxxx000", InstName.Bx,              null,                     typeof(OpCodeT16BReg));
            SetA32("<<<<00110101xxxx0000xxxxxxxxxxxx", InstName.Cmp,             null,                     typeof(OpCode32AluImm));
            SetA32("<<<<00010101xxxx0000xxxxxxx0xxxx", InstName.Cmp,             null,                     typeof(OpCode32AluRsImm));
            SetA32("<<<<100xx0x1xxxxxxxxxxxxxxxxxxxx", InstName.Ldm,             null,                     typeof(OpCode32MemMult));
            SetA32("<<<<010xx0x1xxxxxxxxxxxxxxxxxxxx", InstName.Ldr,             null,                     typeof(OpCode32MemImm));
            SetA32("<<<<010xx1x1xxxxxxxxxxxxxxxxxxxx", InstName.Ldrb,            null,                     typeof(OpCode32MemImm));
            SetA32("<<<<000xx1x0xxxxxxxxxxxx1101xxxx", InstName.Ldrd,            null,                     typeof(OpCode32MemImm8));
            SetA32("<<<<000xx1x1xxxxxxxxxxxx1011xxxx", InstName.Ldrh,            null,                     typeof(OpCode32MemImm8));
            SetA32("<<<<000xx1x1xxxxxxxxxxxx1101xxxx", InstName.Ldrsb,           null,                     typeof(OpCode32MemImm8));
            SetA32("<<<<000xx1x1xxxxxxxxxxxx1111xxxx", InstName.Ldrsh,           null,                     typeof(OpCode32MemImm8));
            SetA32("<<<<0011101x0000xxxxxxxxxxxxxxxx", InstName.Mov,             null,                     typeof(OpCode32AluImm));
            SetA32("<<<<0001101x0000xxxxxxxxxxx0xxxx", InstName.Mov,             null,                     typeof(OpCode32AluRsImm));
            SetT32("xxxxxxxxxxxxxxxx00100xxxxxxxxxxx", InstName.Mov,             null,                     typeof(OpCodeT16AluImm8));
            SetA32("<<<<100xx0x0xxxxxxxxxxxxxxxxxxxx", InstName.Stm,             null,                     typeof(OpCode32MemMult));
            SetA32("<<<<010xx0x0xxxxxxxxxxxxxxxxxxxx", InstName.Str,             null,                     typeof(OpCode32MemImm));
            SetA32("<<<<010xx1x0xxxxxxxxxxxxxxxxxxxx", InstName.Strb,            null,                     typeof(OpCode32MemImm));
            SetA32("<<<<000xx1x0xxxxxxxxxxxx1111xxxx", InstName.Strd,            null,                     typeof(OpCode32MemImm8));
            SetA32("<<<<000xx1x0xxxxxxxxxxxx1011xxxx", InstName.Strh,            null,                     typeof(OpCode32MemImm8));
            SetA32("<<<<0010010xxxxxxxxxxxxxxxxxxxxx", InstName.Sub,             null,                     typeof(OpCode32AluImm));
            SetA32("<<<<0000010xxxxxxxxxxxxxxxx0xxxx", InstName.Sub,             null,                     typeof(OpCode32AluRsImm));
#endregion

            FillFastLookupTable(_instA32FastLookup, _allInstA32);
            FillFastLookupTable(_instT32FastLookup, _allInstT32);
            FillFastLookupTable(_instA64FastLookup, _allInstA64);
        }

        private static void FillFastLookupTable(InstInfo[][] table, List<InstInfo> allInsts)
        {
            List<InstInfo>[] temp = new List<InstInfo>[FastLookupSize];

            for (int index = 0; index < FastLookupSize; index++)
            {
                temp[index] = new List<InstInfo>();
            }

            foreach (InstInfo inst in allInsts)
            {
                int mask  = ToFastLookupIndex(inst.Mask);
                int value = ToFastLookupIndex(inst.Value);

                for (int index = 0; index < FastLookupSize; index++)
                {
                    if ((index & mask) == value)
                    {
                        temp[index].Add(inst);
                    }
                }
            }

            for (int index = 0; index < FastLookupSize; index++)
            {
                table[index] = temp[index].ToArray();
            }
        }

        private static void SetA32(string encoding, InstName name, InstEmitter emitter, Type type)
        {
            Set(encoding, ExecutionMode.Aarch32Arm, new InstDescriptor(name, emitter), type);
        }

        private static void SetT32(string encoding, InstName name, InstEmitter emitter, Type type)
        {
            Set(encoding, ExecutionMode.Aarch32Thumb, new InstDescriptor(name, emitter), type);
        }

        private static void SetA64(string encoding, InstName name, InstEmitter emitter, Type type)
        {
            Set(encoding, ExecutionMode.Aarch64, new InstDescriptor(name, emitter), type);
        }

        private static void Set(string encoding, ExecutionMode mode, InstDescriptor inst, Type type)
        {
            int bit   = encoding.Length - 1;
            int value = 0;
            int xMask = 0;
            int xBits = 0;

            int[] xPos = new int[encoding.Length];

            int blacklisted = 0;

            for (int index = 0; index < encoding.Length; index++, bit--)
            {
                //Note: < and > are used on special encodings.
                //The < means that we should never have ALL bits with the '<' set.
                //So, when the encoding has <<, it means that 00, 01, and 10 are valid,
                //but not 11. <<< is 000, 001, ..., 110 but NOT 111, and so on...
                //For >, the invalid value is zero. So, for >> 01, 10 and 11 are valid,
                //but 00 isn't.
                char chr = encoding[index];

                if (chr == '1')
                {
                    value |= 1 << bit;
                }
                else if (chr == 'x')
                {
                    xMask |= 1 << bit;
                }
                else if (chr == '>')
                {
                    xPos[xBits++] = bit;
                }
                else if (chr == '<')
                {
                    xPos[xBits++] = bit;

                    blacklisted |= 1 << bit;
                }
                else if (chr != '0')
                {
                    throw new ArgumentException(nameof(encoding));
                }
            }

            xMask = ~xMask;

            if (xBits == 0)
            {
                InsertInst(new InstInfo(xMask, value, inst, type), mode);

                return;
            }

            for (int index = 0; index < (1 << xBits); index++)
            {
                int mask = 0;

                for (int x = 0; x < xBits; x++)
                {
                    mask |= ((index >> x) & 1) << xPos[x];
                }

                if (mask != blacklisted)
                {
                    InsertInst(new InstInfo(xMask, value | mask, inst, type), mode);
                }
            }
        }

        private static void InsertInst(InstInfo info, ExecutionMode mode)
        {
            switch (mode)
            {
                case ExecutionMode.Aarch32Arm:   _allInstA32.Add(info); break;
                case ExecutionMode.Aarch32Thumb: _allInstT32.Add(info); break;
                case ExecutionMode.Aarch64:      _allInstA64.Add(info); break;
            }
        }

        public static (InstDescriptor inst, Type type) GetInstA32(int opCode)
        {
            return GetInstFromList(_instA32FastLookup[ToFastLookupIndex(opCode)], opCode);
        }

        public static (InstDescriptor inst, Type type) GetInstT32(int opCode)
        {
            return GetInstFromList(_instT32FastLookup[ToFastLookupIndex(opCode)], opCode);
        }

        public static (InstDescriptor inst, Type type) GetInstA64(int opCode)
        {
            return GetInstFromList(_instA64FastLookup[ToFastLookupIndex(opCode)], opCode);
        }

        private static (InstDescriptor inst, Type type) GetInstFromList(InstInfo[] insts, int opCode)
        {
            foreach (InstInfo info in insts)
            {
                if ((opCode & info.Mask) == info.Value)
                {
                    return (info.Inst, info.Type);
                }
            }

            //TODO: Change null with Und impl.
            return (new InstDescriptor(InstName.Und, null), typeof(OpCode));
        }

        private static int ToFastLookupIndex(int value)
        {
            return ((value >> 10) & 0x00F) | ((value >> 18) & 0xFF0);
        }
    }
}
