using ChocolArm64.Memory;
using ChocolArm64.State;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;

namespace ChocolArm64
{
    class TranslatedSub
    {
        private delegate long ArmSubroutine(CpuThreadState register, MemoryManager memory);

        private ArmSubroutine _execDelegate;

        public static int StateArgIdx  { get; private set; }
        public static int MemoryArgIdx { get; private set; }

        public static Type[] FixedArgTypes { get; private set; }

        public DynamicMethod Method { get; private set; }

        public ReadOnlyCollection<Register> SubArgs { get; private set; }

        public TranslationTier Tier { get; private set; }

        public TranslatedSub(DynamicMethod method, List<Register> subArgs, TranslationTier tier)
        {
            Method  = method                ?? throw new ArgumentNullException(nameof(method));;
            SubArgs = subArgs?.AsReadOnly() ?? throw new ArgumentNullException(nameof(subArgs));

            Tier = tier;

            PrepareDelegate();
        }

        static TranslatedSub()
        {
            MethodInfo mthdInfo = typeof(ArmSubroutine).GetMethod("Invoke");

            ParameterInfo[] Params = mthdInfo.GetParameters();

            FixedArgTypes = new Type[Params.Length];

            for (int index = 0; index < Params.Length; index++)
            {
                Type argType = Params[index].ParameterType;

                FixedArgTypes[index] = argType;

                if (argType == typeof(CpuThreadState))
                {
                    StateArgIdx = index;
                }
                else if (argType == typeof(MemoryManager))
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

            foreach (Register reg in SubArgs)
            {
                generator.EmitLdarg(StateArgIdx);

                generator.Emit(OpCodes.Ldfld, reg.GetField());
            }

            generator.Emit(OpCodes.Call, Method);
            generator.Emit(OpCodes.Ret);

            _execDelegate = (ArmSubroutine)mthd.CreateDelegate(typeof(ArmSubroutine));
        }

        public long Execute(CpuThreadState threadState, MemoryManager memory)
        {
            return _execDelegate(threadState, memory);
        }
    }
}