using ChocolArm64.Decoder;
using ChocolArm64.Instruction;
using ChocolArm64.State;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace ChocolArm64.Translation
{
    internal class AILEmitterCtx
    {
        private ATranslatorCache _cache;

        private Dictionary<long, AILLabel> _labels;

        private int _blkIndex;
        private int _opcIndex;

        private ABlock[] _graph;
        private ABlock   _root;
        public  ABlock   CurrBlock => _graph[_blkIndex];
        public  AOpCode  CurrOp    => _graph[_blkIndex].OpCodes[_opcIndex];

        private AILEmitter _emitter;

        private AILBlock _ilBlock;

        private AOpCode _optOpLastCompare;
        private AOpCode _optOpLastFlagSet;

        //This is the index of the temporary register, used to store temporary
        //values needed by some functions, since IL doesn't have a swap instruction.
        //You can use any value here as long it doesn't conflict with the indices
        //for the other registers. Any value >= 64 or < 0 will do.
        private const int Tmp1Index = -1;
        private const int Tmp2Index = -2;
        private const int Tmp3Index = -3;
        private const int Tmp4Index = -4;
        private const int Tmp5Index = -5;
        private const int Tmp6Index = -6;

        public AILEmitterCtx(
            ATranslatorCache cache,
            ABlock[]         graph,
            ABlock           root,
            string           subName)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _root  = root  ?? throw new ArgumentNullException(nameof(root));

            _labels = new Dictionary<long, AILLabel>();

            _emitter = new AILEmitter(graph, root, subName);

            _ilBlock = _emitter.GetIlBlock(0);

            _opcIndex = -1;

            if (graph.Length == 0 || !AdvanceOpCode())
            {
                throw new ArgumentException(nameof(graph));
            }
        }

        public ATranslatedSub GetSubroutine()
        {
            return _emitter.GetSubroutine();
        }

        public bool AdvanceOpCode()
        {
            if (_opcIndex + 1 == CurrBlock.OpCodes.Count &&
                _blkIndex + 1 == _graph.Length)
            {
                return false;
            }

            while (++_opcIndex >= (CurrBlock?.OpCodes.Count ?? 0))
            {
                _blkIndex++;
                _opcIndex = -1;

                _optOpLastFlagSet = null;
                _optOpLastCompare = null;

                _ilBlock = _emitter.GetIlBlock(_blkIndex);
            }

            return true;
        }

        public void EmitOpCode()
        {
            if (_opcIndex == 0)
            {
                MarkLabel(GetLabel(CurrBlock.Position));

                EmitSynchronization();
            }

            CurrOp.Emitter(this);

            _ilBlock.Add(new AILBarrier());
        }

        private void EmitSynchronization()
        {
            EmitLdarg(ATranslatedSub.StateArgIdx);

            EmitLdc_I4(CurrBlock.OpCodes.Count);

            EmitPrivateCall(typeof(AThreadState), nameof(AThreadState.Synchronize));

            EmitLdc_I4(0);

            AILLabel lblContinue = new AILLabel();

            Emit(OpCodes.Bne_Un_S, lblContinue);

            EmitLdc_I8(0);

            Emit(OpCodes.Ret);

            MarkLabel(lblContinue);
        }

        public bool TryOptEmitSubroutineCall()
        {
            if (CurrBlock.Next == null)
            {
                return false;
            }

            if (CurrOp.Emitter != AInstEmit.Bl)
            {
                return false;
            }

            if (!_cache.TryGetSubroutine(((AOpCodeBImmAl)CurrOp).Imm, out ATranslatedSub subroutine))
            {
                return false;
            }

            for (int index = 0; index < ATranslatedSub.FixedArgTypes.Length; index++)
            {
                EmitLdarg(index);
            }

            foreach (ARegister reg in subroutine.Params)
            {
                switch (reg.Type)
                {
                    case ARegisterType.Flag:   Ldloc(reg.Index, AIoType.Flag);   break;
                    case ARegisterType.Int:    Ldloc(reg.Index, AIoType.Int);    break;
                    case ARegisterType.Vector: Ldloc(reg.Index, AIoType.Vector); break;
                }
            }

            EmitCall(subroutine.Method);

            subroutine.AddCaller(_root.Position);

            return true;
        }

        public void TryOptMarkCondWithoutCmp()
        {
            _optOpLastCompare = CurrOp;

            AInstEmitAluHelper.EmitDataLoadOpers(this);

            Stloc(Tmp4Index, AIoType.Int);
            Stloc(Tmp3Index, AIoType.Int);
        }

        private Dictionary<ACond, OpCode> _branchOps = new Dictionary<ACond, OpCode>()
        {
            { ACond.Eq,    OpCodes.Beq    },
            { ACond.Ne,    OpCodes.Bne_Un },
            { ACond.GeUn, OpCodes.Bge_Un },
            { ACond.LtUn, OpCodes.Blt_Un },
            { ACond.GtUn, OpCodes.Bgt_Un },
            { ACond.LeUn, OpCodes.Ble_Un },
            { ACond.Ge,    OpCodes.Bge    },
            { ACond.Lt,    OpCodes.Blt    },
            { ACond.Gt,    OpCodes.Bgt    },
            { ACond.Le,    OpCodes.Ble    }
        };

        public void EmitCondBranch(AILLabel target, ACond cond)
        {
            OpCode ilOp;

            int intCond = (int)cond;

            if (_optOpLastCompare != null &&
                _optOpLastCompare == _optOpLastFlagSet && _branchOps.ContainsKey(cond))
            {
                Ldloc(Tmp3Index, AIoType.Int, _optOpLastCompare.RegisterSize);
                Ldloc(Tmp4Index, AIoType.Int, _optOpLastCompare.RegisterSize);

                ilOp = _branchOps[cond];
            }
            else if (intCond < 14)
            {
                int condTrue = intCond >> 1;

                switch (condTrue)
                {
                    case 0: EmitLdflg((int)APState.ZBit); break;
                    case 1: EmitLdflg((int)APState.CBit); break;
                    case 2: EmitLdflg((int)APState.NBit); break;
                    case 3: EmitLdflg((int)APState.VBit); break;

                    case 4:
                        EmitLdflg((int)APState.CBit);
                        EmitLdflg((int)APState.ZBit);

                        Emit(OpCodes.Not);
                        Emit(OpCodes.And);
                        break;

                    case 5:
                    case 6:
                        EmitLdflg((int)APState.NBit);
                        EmitLdflg((int)APState.VBit);

                        Emit(OpCodes.Ceq);

                        if (condTrue == 6)
                        {
                            EmitLdflg((int)APState.ZBit);

                            Emit(OpCodes.Not);
                            Emit(OpCodes.And);
                        }
                        break;
                }

                ilOp = (intCond & 1) != 0
                    ? OpCodes.Brfalse
                    : OpCodes.Brtrue;
            }
            else
            {
                ilOp = OpCodes.Br;
            }

            Emit(ilOp, target);
        }

        public void EmitCast(AIntType intType)
        {
            switch (intType)
            {
                case AIntType.UInt8:  Emit(OpCodes.Conv_U1); break;
                case AIntType.UInt16: Emit(OpCodes.Conv_U2); break;
                case AIntType.UInt32: Emit(OpCodes.Conv_U4); break;
                case AIntType.UInt64: Emit(OpCodes.Conv_U8); break;
                case AIntType.Int8:   Emit(OpCodes.Conv_I1); break;
                case AIntType.Int16:  Emit(OpCodes.Conv_I2); break;
                case AIntType.Int32:  Emit(OpCodes.Conv_I4); break;
                case AIntType.Int64:  Emit(OpCodes.Conv_I8); break;
            }

            bool sz64 = CurrOp.RegisterSize != ARegisterSize.Int32;

            if (sz64 == (intType == AIntType.UInt64 ||
                         intType == AIntType.Int64))
            {
                return;
            }

            if (sz64)
            {
                Emit(intType >= AIntType.Int8
                    ? OpCodes.Conv_I8
                    : OpCodes.Conv_U8);
            }
            else
            {
                Emit(OpCodes.Conv_U4);
            }
        }

        public void EmitLsl(int amount)
        {
            EmitIlShift(amount, OpCodes.Shl);
        }

        public void EmitLsr(int amount)
        {
            EmitIlShift(amount, OpCodes.Shr_Un);
        }

        public void EmitAsr(int amount)
        {
            EmitIlShift(amount, OpCodes.Shr);
        }

        private void EmitIlShift(int amount, OpCode ilOp)
        {
            if (amount > 0)
            {
                EmitLdc_I4(amount);

                Emit(ilOp);
            }
        }

        public void EmitRor(int amount)
        {
            if (amount > 0)
            {
                Stloc(Tmp2Index, AIoType.Int);
                Ldloc(Tmp2Index, AIoType.Int);

                EmitLdc_I4(amount);

                Emit(OpCodes.Shr_Un);

                Ldloc(Tmp2Index, AIoType.Int);

                EmitLdc_I4(CurrOp.GetBitsCount() - amount);

                Emit(OpCodes.Shl);
                Emit(OpCodes.Or);
            }
        }

        public AILLabel GetLabel(long position)
        {
            if (!_labels.TryGetValue(position, out AILLabel output))
            {
                output = new AILLabel();

                _labels.Add(position, output);
            }

            return output;
        }

        public void MarkLabel(AILLabel label)
        {
            _ilBlock.Add(label);
        }

        public void Emit(OpCode ilOp)
        {
            _ilBlock.Add(new AILOpCode(ilOp));
        }

        public void Emit(OpCode ilOp, AILLabel label)
        {
            _ilBlock.Add(new AILOpCodeBranch(ilOp, label));
        }

        public void Emit(string text)
        {
            _ilBlock.Add(new AILOpCodeLog(text));
        }

        public void EmitLdarg(int index)
        {
            _ilBlock.Add(new AILOpCodeLoad(index, AIoType.Arg));
        }

        public void EmitLdintzr(int index)
        {
            if (index != AThreadState.ZrIndex)
            {
                EmitLdint(index);
            }
            else
            {
                EmitLdc_I(0);
            }
        }

        public void EmitStintzr(int index)
        {
            if (index != AThreadState.ZrIndex)
            {
                EmitStint(index);
            }
            else
            {
                Emit(OpCodes.Pop);
            }
        }

        public void EmitLoadState(ABlock retBlk)
        {
            _ilBlock.Add(new AILOpCodeLoad(Array.IndexOf(_graph, retBlk), AIoType.Fields));
        }

        public void EmitStoreState()
        {
            _ilBlock.Add(new AILOpCodeStore(Array.IndexOf(_graph, CurrBlock), AIoType.Fields));
        }

        public void EmitLdtmp()
        {
            EmitLdint(Tmp1Index);
        }

        public void EmitSttmp()
        {
            EmitStint(Tmp1Index);
        }

        public void EmitLdvectmp()
        {
            EmitLdvec(Tmp5Index);
        }

        public void EmitStvectmp()
        {
            EmitStvec(Tmp5Index);
        }

        public void EmitLdvectmp2()
        {
            EmitLdvec(Tmp6Index);
        }

        public void EmitStvectmp2()
        {
            EmitStvec(Tmp6Index);
        }

        public void EmitLdint(int index)
        {
            Ldloc(index, AIoType.Int);
        }

        public void EmitStint(int index)
        {
            Stloc(index, AIoType.Int);
        }

        public void EmitLdvec(int index)
        {
            Ldloc(index, AIoType.Vector);
        }

        public void EmitStvec(int index)
        {
            Stloc(index, AIoType.Vector);
        }

        public void EmitLdflg(int index)
        {
            Ldloc(index, AIoType.Flag);
        }

        public void EmitStflg(int index)
        {
            _optOpLastFlagSet = CurrOp;

            Stloc(index, AIoType.Flag);
        }

        private void Ldloc(int index, AIoType ioType)
        {
            _ilBlock.Add(new AILOpCodeLoad(index, ioType, CurrOp.RegisterSize));
        }

        private void Ldloc(int index, AIoType ioType, ARegisterSize registerSize)
        {
            _ilBlock.Add(new AILOpCodeLoad(index, ioType, registerSize));
        }

        private void Stloc(int index, AIoType ioType)
        {
            _ilBlock.Add(new AILOpCodeStore(index, ioType, CurrOp.RegisterSize));
        }

        public void EmitCallPropGet(Type objType, string propName)
        {
            if (objType == null)
            {
                throw new ArgumentNullException(nameof(objType));
            }

            if (propName == null)
            {
                throw new ArgumentNullException(nameof(propName));
            }

            EmitCall(objType.GetMethod($"get_{propName}"));
        }

        public void EmitCallPropSet(Type objType, string propName)
        {
            if (objType == null)
            {
                throw new ArgumentNullException(nameof(objType));
            }

            if (propName == null)
            {
                throw new ArgumentNullException(nameof(propName));
            }

            EmitCall(objType.GetMethod($"set_{propName}"));
        }

        public void EmitCall(Type objType, string mthdName)
        {
            if (objType == null)
            {
                throw new ArgumentNullException(nameof(objType));
            }

            if (mthdName == null)
            {
                throw new ArgumentNullException(nameof(mthdName));
            }

            EmitCall(objType.GetMethod(mthdName));
        }

        public void EmitPrivateCall(Type objType, string mthdName)
        {
            if (objType == null)
            {
                throw new ArgumentNullException(nameof(objType));
            }

            if (mthdName == null)
            {
                throw new ArgumentNullException(nameof(mthdName));
            }

            EmitCall(objType.GetMethod(mthdName, BindingFlags.Instance | BindingFlags.NonPublic));
        }

        public void EmitCall(MethodInfo mthdInfo)
        {
            if (mthdInfo == null)
            {
                throw new ArgumentNullException(nameof(mthdInfo));
            }

            _ilBlock.Add(new AILOpCodeCall(mthdInfo));
        }

        public void EmitLdc_I(long value)
        {
            if (CurrOp.RegisterSize == ARegisterSize.Int32)
            {
                EmitLdc_I4((int)value);
            }
            else
            {
                EmitLdc_I8(value);
            }
        }

        public void EmitLdc_I4(int value)
        {
            _ilBlock.Add(new AILOpCodeConst(value));
        }

        public void EmitLdc_I8(long value)
        {
            _ilBlock.Add(new AILOpCodeConst(value));
        }

        public void EmitLdc_R4(float value)
        {
            _ilBlock.Add(new AILOpCodeConst(value));
        }

        public void EmitLdc_R8(double value)
        {
            _ilBlock.Add(new AILOpCodeConst(value));
        }

        public void EmitZnFlagCheck()
        {
            EmitZnCheck(OpCodes.Ceq, (int)APState.ZBit);
            EmitZnCheck(OpCodes.Clt, (int)APState.NBit);
        }

        private void EmitZnCheck(OpCode ilCmpOp, int flag)
        {
            Emit(OpCodes.Dup);
            Emit(OpCodes.Ldc_I4_0);

            if (CurrOp.RegisterSize != ARegisterSize.Int32)
            {
                Emit(OpCodes.Conv_I8);
            }

            Emit(ilCmpOp);

            EmitStflg(flag);
        }
    }
}
