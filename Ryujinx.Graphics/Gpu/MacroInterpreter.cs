using ChocolArm64.Memory;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu
{
    class MacroInterpreter
    {
        private INvGpuEngine Engine;

        public Queue<int> Fifo { get; private set; }

        private int[] Gprs;

        private int MethAddr;
        private int MethIncr;

        private bool Carry;

        private long Pc;

        public MacroInterpreter(INvGpuEngine Engine)
        {
            this.Engine = Engine;

            Fifo = new Queue<int>();    

            Gprs = new int[8];
        }

        public void Execute(AMemory Memory, long Position, int Param)
        {
            Reset();

            Pc = Position;

            Gprs[1] = Param;

            while (Step(Memory));
        }

        private void Reset()
        {
            for (int Index = 0; Index < Gprs.Length; Index++)
            {
                Gprs[Index] = 0;
            }

            MethAddr = 0;
            MethIncr = 0;

            Carry = false;
        }

        private bool Step(AMemory Memory)
        {
            long BaseAddr = Pc;

            int OpCode = Memory.ReadInt32(Pc);

            Pc += 4;

            int Op = (OpCode >> 0) & 7;

            if (Op < 7)
            {
                //Operation produces a value.
                int AsgOp = (OpCode >> 4) & 7;

                int Result = GetInstResult(OpCode);

                switch (AsgOp)
                {
                    //Fetch parameter and ignore result.
                    case 0: SetDstGpr(OpCode, FetchParam()); break;

                    //Move result.
                    case 1: SetDstGpr(OpCode, Result); break;

                    //Move result and use as Method Address.
                    case 2: SetDstGpr(OpCode, Result); SetMethAddr(Result); break;

                    //Fetch parameter and send result.
                    case 3: SetDstGpr(OpCode, FetchParam()); Send(Memory, Result); break;

                    //Move and send result.
                    case 4: SetDstGpr(OpCode, Result); Send(Memory, Result); break;

                    //Fetch parameter and use result as Method Address.
                    case 5: SetDstGpr(OpCode, FetchParam()); SetMethAddr(Result); break;

                    //Move result and use as Method Address, then fetch and send paramter.
                    case 6: SetDstGpr(OpCode, Result); SetMethAddr(Result); Send(Memory, FetchParam()); break;

                    //Move result and use as Method Address, then send bits 17:12 of result.
                    case 7: SetDstGpr(OpCode, Result); SetMethAddr(Result); Send(Memory, (Result >> 12) & 0x3f); break;
                }
            }
            else
            {
                //Branch.
                bool OnNotZero = ((OpCode >> 4) & 1) != 0;

                bool Taken = OnNotZero
                    ? GetGprA(OpCode) != 0
                    : GetGprA(OpCode) == 0;

                if (Taken)
                {
                    //Execute one more instruction due to delay slot.
                    bool KeepExecuting = Step(Memory);

                    Pc = BaseAddr + (GetImm(OpCode) << 2);

                    return KeepExecuting;
                }
            }

            if ((OpCode & 0x80) != 0)
            {
                //Exit (with a delay slot).
                Step(Memory);

                return false;
            }

            return true;
        }

        private int GetInstResult(int OpCode)
        {
            int Low = OpCode & 7;

            switch (Low)
            {
                //Arithmetic or Logical operation.
                case 0:
                {
                    int AluOp = (OpCode >> 17) & 0x1f;

                    return GetAluResult(AluOp, GetGprA(OpCode), GetGprB(OpCode));
                }

                //Add Immediate.
                case 1:
                {
                    return GetGprA(OpCode) + GetImm(OpCode);
                }

                //Bitfield.
                case 2:
                case 3:
                case 4:
                {
                    int BfSrcBit = (OpCode >> 17) & 0x1f;
                    int BfSize   = (OpCode >> 22) & 0x1f;
                    int BfDstBit = (OpCode >> 27) & 0x1f;

                    int BfMask = (1 << BfSize) - 1;

                    int Dst = GetGprA(OpCode);
                    int Src = GetGprB(OpCode);

                    switch (Low)
                    {
                        //Bitfield move.
                        case 2:
                        {
                            Src = (Src >> BfSrcBit) & BfMask;

                            Dst &= ~(BfMask << BfDstBit);

                            Dst |= Src << BfDstBit;

                            return Dst;
                        }
                        
                        //Bitfield extract with left shift by immediate.
                        case 3:
                        {
                            Src = (Src >> Dst) & BfMask;

                            return Src << BfDstBit;
                        }

                        //Bitfield extract with left shift by register.
                        case 4:
                        {
                            Src = (Src >> BfSrcBit) & BfMask;

                            return Src << Dst;
                        }
                    }

                    break;
                }

                case 5:
                {
                    return Read(GetGprA(OpCode) + GetImm(OpCode));
                }
            }

            throw new ArgumentException(nameof(OpCode));
        }

        private int GetAluResult(int SubOp, int A, int B)
        {
            switch (SubOp)
            {
                //Add.
                case 0: return A + B;

                //Add with Carry.
                case 1:
                {
                    ulong C = Carry ? 1UL : 0UL;

                    ulong Result = (ulong)A + (ulong)B + C;

                    Carry = Result > 0xffffffff;

                    return (int)Result;
                }

                //Subtract.
                case 2: return A - B;

                //Subtract with Borrow.
                case 3:
                {
                    ulong C = Carry ? 0UL : 1UL;

                    ulong Result = (ulong)A - (ulong)B - C;

                    Carry = Result < 0x100000000;

                    return (int)Result;
                }

                //Exclusive Or.
                case 8: return A ^ B;

                //Or.
                case 9: return A | B;

                //And.
                case 10: return A & B;

                //And Not.
                case 11: return A & ~B;

                //Not And.
                case 12: return ~(A & B);
            }

            throw new ArgumentOutOfRangeException(nameof(SubOp));
        }

        private int GetImm(int OpCode)
        {
            //Note: The immediate is signed, the sign-extension is intended here.
            return OpCode >> 14;
        }

        private void SetMethAddr(int Value)
        {
            MethAddr = (Value >>  0) & 0xfff;
            MethIncr = (Value >> 12) & 0x3f;
        }

        private void SetDstGpr(int OpCode, int Value)
        {
            Gprs[(OpCode >> 8) & 7] = Value;
        }

        private int GetGprA(int OpCode)
        {
            return GetGprValue((OpCode >> 11) & 7);
        }

        private int GetGprB(int OpCode)
        {
            return GetGprValue((OpCode >> 14) & 7);
        }

        private int GetGprValue(int Index)
        {
            return Index != 0 ? Gprs[Index] : 0;
        }

        private int FetchParam()
        {
            Fifo.TryDequeue(out int Value);

            return Value;
        }

        private int Read(int Reg)
        {
            return Engine.Registers[Reg];
        }

        private void Send(AMemory Memory, int Value)
        {
            NsGpuPBEntry PBEntry = new NsGpuPBEntry(MethAddr, 0, Value);

            Engine.CallMethod(Memory, PBEntry);

            MethAddr += MethIncr;
        }
    }
}