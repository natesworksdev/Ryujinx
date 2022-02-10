using ARMeilleure.State;
using NUnit.Framework;
using System;

namespace Ryujinx.Tests.Cpu
{
    [Category("Thumb")]
    public sealed class CpuTestThumb : CpuTest32
    {
        private const int RndCnt = 2;

        public static uint RotateRight(uint value, int count)
        {
            return (value >> count) | (value << (32 - count));
        }

        [Test, Pairwise]
        public void ShiftImm([Range(0u, 2u)] uint shiftType, [Range(1u, 0x1fu)] uint shiftImm, [Random(RndCnt)] uint w1, [Random(RndCnt)] uint w2)
        {
            uint opcode = 0x0000; // MOVS <Rd>, <Rm>, <shift> #<amount>

            uint rd = 1;
            uint rm = 2;
            opcode |= ((rd & 7) << 0) | ((rm & 7) << 3) | ((shiftImm & 0x1f) << 6) | ((shiftType & 3) << 11);

            SingleThumbOpcode((ushort)opcode, r1: w1, r2: w2, runUnicorn: false);

            switch (shiftType)
            {
                case 0:
                    Assert.That(GetContext().GetX(1), Is.EqualTo((w2 << (int)shiftImm) & 0xffffffffu));
                    break;
                case 1:
                    Assert.That(GetContext().GetX(1), Is.EqualTo((w2 >> (int)shiftImm) & 0xffffffffu));
                    break;
                case 2:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(((int)w2 >> (int)shiftImm) & 0xffffffffu));
                    break;
            }
        }

        [Test, Pairwise]
        public void AddSubReg([Range(0u, 1u)] uint op, [Random(RndCnt)] uint w1, [Random(RndCnt)] uint w2)
        {
            uint opcode = 0x1800; // ADDS <Rd>, <Rn>, <Rm>

            uint rd = 0;
            uint rn = 1;
            uint rm = 2;
            opcode |= ((rd & 7) << 0) | ((rn & 7) << 3) | ((rm & 7) << 6) | ((op & 1) << 9);

            SingleThumbOpcode((ushort)opcode, r1: w1, r2: w2, runUnicorn: false);

            switch (op)
            {
                case 0:
                    Assert.That(GetContext().GetX(0), Is.EqualTo((w1 + w2) & 0xffffffffu));
                    break;
                case 1:
                    Assert.That(GetContext().GetX(0), Is.EqualTo((w1 - w2) & 0xffffffffu));
                    break;
            }
        }

        [Test, Pairwise]
        public void AddSubImm3([Range(0u, 1u)] uint op, [Range(0u, 7u)] uint imm, [Random(RndCnt)] uint w1)
        {
            uint opcode = 0x1c00; // ADDS <Rd>, <Rn>, #<imm3>

            uint rd = 0;
            uint rn = 1;
            opcode |= ((rd & 7) << 0) | ((rn & 7) << 3) | ((imm & 7) << 6) | ((op & 1) << 9);

            SingleThumbOpcode((ushort)opcode, r1: w1, runUnicorn: false);

            switch (op)
            {
                case 0:
                    Assert.That(GetContext().GetX(0), Is.EqualTo((w1 + imm) & 0xffffffffu));
                    break;
                case 1:
                    Assert.That(GetContext().GetX(0), Is.EqualTo((w1 - imm) & 0xffffffffu));
                    break;
            }
        }

        [Test, Pairwise]
        public void AluImm8([Range(0u, 3u)] uint op, [Random(RndCnt)] uint imm, [Random(RndCnt)] uint w1)
        {
            imm &= 0xff;

            uint opcode = 0x2000; // MOVS <Rdn>, #<imm8>

            uint rdn = 1;
            opcode |= ((imm & 0xff) << 0) | ((rdn & 7) << 8) | ((op & 3) << 11);

            SingleThumbOpcode((ushort)opcode, r1: w1, runUnicorn: false);

            switch (op)
            {
                case 0:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(imm));
                    break;
                case 1:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(w1));
                cmpFlags:
                    {
                        uint result = w1 - imm;
                        uint overflow = (result ^ w1) & (w1 ^ imm);
                        Assert.That(GetContext().GetPstateFlag(PState.NFlag), Is.EqualTo((result >> 31) != 0));
                        Assert.That(GetContext().GetPstateFlag(PState.ZFlag), Is.EqualTo(result == 0));
                        Assert.That(GetContext().GetPstateFlag(PState.CFlag), Is.EqualTo(w1 >= imm));
                        Assert.That(GetContext().GetPstateFlag(PState.VFlag), Is.EqualTo((overflow >> 31) != 0));
                    }
                    break;
                case 2:
                    Assert.That(GetContext().GetX(1), Is.EqualTo((w1 + imm) & 0xffffffffu));
                    break;
                case 3:
                    Assert.That(GetContext().GetX(1), Is.EqualTo((w1 - imm) & 0xffffffffu));
                    goto cmpFlags;
            }
        }

        [Test, Pairwise]
        public void AluRegLow([Range(0u, 0xfu)] uint op, [Random(RndCnt)] uint w1, [Random(RndCnt)] uint w2)
        {
            uint opcode = 0x4000; // ANDS <Rdn>, <Rm>

            uint rd = 1;
            uint rm = 2;
            opcode |= ((rd & 7) << 0) | ((rm & 7) << 3) | ((op & 0xf) << 6);

            SingleThumbOpcode((ushort)opcode, r1: w1, r2: w2, runUnicorn: false);

            uint shift = w2 & 0xff;
            switch (op)
            {
                case 0:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(w1 & w2));
                    break;
                case 1:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(w1 ^ w2));
                    break;
                case 2:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(shift >= 32 ? 0 : (uint)(w1 << (int)shift)));
                    break;
                case 3:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(shift >= 32 ? 0 : (uint)(w1 >> (int)shift)));
                    break;
                case 4:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(shift >= 32 ? (uint)((int)w1 >> 31) : (uint)((int)w1 >> (int)shift)));
                    break;
                case 5:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(w1 + w2));
                    break;
                case 6:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(w1 + ~w2));
                    break;
                case 7:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(RotateRight(w1, (int)shift & 31)));
                    break;
                case 8:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(w1));
                    {
                        uint result = w1 & w2;
                        Assert.That(GetContext().GetPstateFlag(PState.NFlag), Is.EqualTo((result >> 31) != 0));
                        Assert.That(GetContext().GetPstateFlag(PState.ZFlag), Is.EqualTo(result == 0));
                    }
                    break;
                case 9:
                    Assert.That(GetContext().GetX(1), Is.EqualTo((uint)-w2));
                    break;
                case 10:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(w1));
                    {
                        uint result = w1 - w2;
                        uint overflow = (result ^ w1) & (w1 ^ w2);
                        Assert.That(GetContext().GetPstateFlag(PState.NFlag), Is.EqualTo((result >> 31) != 0));
                        Assert.That(GetContext().GetPstateFlag(PState.ZFlag), Is.EqualTo(result == 0));
                        Assert.That(GetContext().GetPstateFlag(PState.CFlag), Is.EqualTo(w1 >= w2));
                        Assert.That(GetContext().GetPstateFlag(PState.VFlag), Is.EqualTo((overflow >> 31) != 0));
                    }
                    break;
                case 11:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(w1));
                    {
                        uint result = w1 + w2;
                        uint overflow = (result ^ w1) & ~(w1 ^ w2);
                        Assert.That(GetContext().GetPstateFlag(PState.NFlag), Is.EqualTo((result >> 31) != 0));
                        Assert.That(GetContext().GetPstateFlag(PState.ZFlag), Is.EqualTo(result == 0));
                        Assert.That(GetContext().GetPstateFlag(PState.CFlag), Is.EqualTo(result < w1));
                        Assert.That(GetContext().GetPstateFlag(PState.VFlag), Is.EqualTo((overflow >> 31) != 0));
                    }
                    break;
                case 12:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(w1 | w2));
                    break;
                case 13:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(w1 * w2));
                    break;
                case 14:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(w1 & ~w2));
                    break;
                case 15:
                    Assert.That(GetContext().GetX(1), Is.EqualTo(~w2));
                    break;
            }
        }
    }
}
