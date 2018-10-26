using ChocolArm64.Memory;
using ChocolArm64.State;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace ChocolArm64
{
    internal class ATranslatedSub
    {
        private delegate long AA64Subroutine(AThreadState register, AMemory memory);

        private const int MinCallCountForReJit = 250;

        private AA64Subroutine _execDelegate;

        public static int StateArgIdx  { get; private set; }
        public static int MemoryArgIdx { get; private set; }

        public static Type[] FixedArgTypes { get; private set; }

        public DynamicMethod Method { get; private set; }

        public ReadOnlyCollection<ARegister> Params { get; private set; }

        private HashSet<long> _callers;

        private ATranslatedSubType _type;

        private int _callCount;

        private bool _needsReJit;

        public ATranslatedSub(DynamicMethod method, List<ARegister> Params)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            if (Params == null)
            {
                throw new ArgumentNullException(nameof(Params));
            }

            Method = method;
            this.Params = Params.AsReadOnly();

            _callers = new HashSet<long>();

            PrepareDelegate();
        }

        static ATranslatedSub()
        {
            MethodInfo mthdInfo = typeof(AA64Subroutine).GetMethod("Invoke");

            ParameterInfo[] Params = mthdInfo.GetParameters();

            FixedArgTypes = new Type[Params.Length];

            for (int index = 0; index < Params.Length; index++)
            {
                Type paramType = Params[index].ParameterType;

                FixedArgTypes[index] = paramType;

                if (paramType == typeof(AThreadState))
                {
                    StateArgIdx = index;
                }
                else if (paramType == typeof(AMemory))
                {
                    MemoryArgIdx = index;
                }
            }
        }

        private void PrepareDelegate()
        {
            string name = $"{Method.Name}_Dispatch";

            DynamicMethod mthd = new DynamicMethod(name, typeof(long), FixedArgTypes);

            ILGenerator generator = mthd.GetILGenerator();

            generator.EmitLdargSeq(FixedArgTypes.Length);

            foreach (ARegister reg in Params)
            {
                generator.EmitLdarg(StateArgIdx);

                generator.Emit(OpCodes.Ldfld, reg.GetField());
            }

            generator.Emit(OpCodes.Call, Method);
            generator.Emit(OpCodes.Ret);

            _execDelegate = (AA64Subroutine)mthd.CreateDelegate(typeof(AA64Subroutine));
        }

        public bool ShouldReJit()
        {
            if (_needsReJit && _callCount < MinCallCountForReJit)
            {
                _callCount++;

                return false;
            }

            return _needsReJit;
        }

        public long Execute(AThreadState threadState, AMemory memory)
        {
            return _execDelegate(threadState, memory);
        }

        public void AddCaller(long position)
        {
            lock (_callers)
            {
                _callers.Add(position);
            }
        }

        public long[] GetCallerPositions()
        {
            lock (_callers)
            {
                return _callers.ToArray();
            }
        }

        public void SetType(ATranslatedSubType type)
        {
            _type = type;

            if (type == ATranslatedSubType.SubTier0)
            {
                _needsReJit = true;
            }
        }

        public void MarkForReJit()
        {
            _needsReJit = true;
        }
    }
}