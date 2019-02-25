using ChocolArm64.Memory;
using ChocolArm64.State;
using System;
using System.Reflection;
using System.Reflection.Emit;

namespace ChocolArm64.Translation
{
    delegate long ArmSubroutine(CpuThreadState state, MemoryManager memory);

    class TranslatedSub
    {
        public ArmSubroutine Delegate { get; private set; }

        public static int StateArgIdx  { get; }
        public static int MemoryArgIdx { get; }

        public static Type[] FixedArgTypes { get; }

        public DynamicMethod Method { get; }

        public TranslationTier Tier { get; }

        public long IntNiRegsMask { get; }
        public long VecNiRegsMask { get; }

        public TranslatedSub(
            DynamicMethod   method,
            TranslationTier tier,
            long            intNiRegsMask,
            long            vecNiRegsMask)
        {
            Method        = method ?? throw new ArgumentNullException(nameof(method));;
            Tier          = tier;
            IntNiRegsMask = intNiRegsMask;
            VecNiRegsMask = vecNiRegsMask;
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

        public void PrepareMethod()
        {
            Delegate = (ArmSubroutine)Method.CreateDelegate(typeof(ArmSubroutine));
        }

        public long Execute(CpuThreadState threadState, MemoryManager memory)
        {
            return Delegate(threadState, memory);
        }
    }
}