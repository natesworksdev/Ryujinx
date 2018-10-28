using ChocolArm64.Decoder;
using ChocolArm64.State;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Runtime.Intrinsics;

namespace ChocolArm64.Translation
{
    class AilEmitter
    {
        public ALocalAlloc LocalAlloc { get; private set; }

        public ILGenerator Generator { get; private set; }

        private Dictionary<ARegister, int> _locals;

        private AilBlock[] _ilBlocks;

        private AilBlock _root;

        private ATranslatedSub _subroutine;

        private string _subName;

        private int _localsCount;

        public AilEmitter(ABlock[] graph, ABlock root, string subName)
        {
            _subName = subName;

            _locals = new Dictionary<ARegister, int>();

            _ilBlocks = new AilBlock[graph.Length];

            AilBlock GetBlock(int index)
            {
                if (index < 0 || index >= _ilBlocks.Length)
                {
                    return null;
                }

                if (_ilBlocks[index] == null)
                {
                    _ilBlocks[index] = new AilBlock();
                }

                return _ilBlocks[index];
            }

            for (int index = 0; index < _ilBlocks.Length; index++)
            {
                AilBlock block = GetBlock(index);

                block.Next   = GetBlock(Array.IndexOf(graph, graph[index].Next));
                block.Branch = GetBlock(Array.IndexOf(graph, graph[index].Branch));
            }

            _root = _ilBlocks[Array.IndexOf(graph, root)];
        }

        public AilBlock GetIlBlock(int index) => _ilBlocks[index];

        public ATranslatedSub GetSubroutine()
        {
            LocalAlloc = new ALocalAlloc(_ilBlocks, _root);

            InitSubroutine();
            InitLocals();

            foreach (AilBlock ilBlock in _ilBlocks)
            {
                ilBlock.Emit(this);
            }

            return _subroutine;
        }

        private void InitSubroutine()
        {
            List<ARegister> Params = new List<ARegister>();

            void SetParams(long inputs, ARegisterType baseType)
            {
                for (int bit = 0; bit < 64; bit++)
                {
                    long mask = 1L << bit;

                    if ((inputs & mask) != 0)
                    {
                        Params.Add(GetRegFromBit(bit, baseType));
                    }
                }
            }

            SetParams(LocalAlloc.GetIntInputs(_root), ARegisterType.Int);
            SetParams(LocalAlloc.GetVecInputs(_root), ARegisterType.Vector);

            DynamicMethod mthd = new DynamicMethod(_subName, typeof(long), GetParamTypes(Params));

            Generator = mthd.GetILGenerator();

            _subroutine = new ATranslatedSub(mthd, Params);
        }

        private void InitLocals()
        {
            int paramsStart = ATranslatedSub.FixedArgTypes.Length;

            _locals = new Dictionary<ARegister, int>();

            for (int index = 0; index < _subroutine.Params.Count; index++)
            {
                ARegister reg = _subroutine.Params[index];

                Generator.EmitLdarg(index + paramsStart);
                Generator.EmitStloc(GetLocalIndex(reg));
            }
        }

        private Type[] GetParamTypes(IList<ARegister> Params)
        {
            Type[] fixedArgs = ATranslatedSub.FixedArgTypes;

            Type[] output = new Type[Params.Count + fixedArgs.Length];

            fixedArgs.CopyTo(output, 0);

            int typeIdx = fixedArgs.Length;

            for (int index = 0; index < Params.Count; index++)
            {
                output[typeIdx++] = GetFieldType(Params[index].Type);
            }

            return output;
        }

        public int GetLocalIndex(ARegister reg)
        {
            if (!_locals.TryGetValue(reg, out int index))
            {
                Generator.DeclareLocal(GetLocalType(reg));

                index = _localsCount++;

                _locals.Add(reg, index);
            }

            return index;
        }

        public Type GetLocalType(ARegister reg) => GetFieldType(reg.Type);

        public Type GetFieldType(ARegisterType regType)
        {
            switch (regType)
            {
                case ARegisterType.Flag:   return typeof(bool);
                case ARegisterType.Int:    return typeof(ulong);
                case ARegisterType.Vector: return typeof(Vector128<float>);
            }

            throw new ArgumentException(nameof(regType));
        }

        public static ARegister GetRegFromBit(int bit, ARegisterType baseType)
        {
            if (bit < 32)
            {
                return new ARegister(bit, baseType);
            }
            else if (baseType == ARegisterType.Int)
            {
                return new ARegister(bit & 0x1f, ARegisterType.Flag);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(bit));
            }
        }

        public static bool IsRegIndex(int index)
        {
            return index >= 0 && index < 32;
        }
    }
}