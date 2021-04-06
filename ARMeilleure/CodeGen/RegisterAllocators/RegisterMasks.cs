using ARMeilleure.IntermediateRepresentation;
using System;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    readonly struct RegisterMasks
    {
        public readonly int IntAvailableRegisters;
        public readonly int VecAvailableRegisters;
        public readonly int IntCallerSavedRegisters;
        public readonly int VecCallerSavedRegisters;
        public readonly int IntCalleeSavedRegisters;
        public readonly int VecCalleeSavedRegisters;

        public RegisterMasks(
            int intAvailableRegisters,
            int vecAvailableRegisters,
            int intCallerSavedRegisters,
            int vecCallerSavedRegisters,
            int intCalleeSavedRegisters,
            int vecCalleeSavedRegisters)
        {
            IntAvailableRegisters   = intAvailableRegisters;
            VecAvailableRegisters   = vecAvailableRegisters;
            IntCallerSavedRegisters = intCallerSavedRegisters;
            VecCallerSavedRegisters = vecCallerSavedRegisters;
            IntCalleeSavedRegisters = intCalleeSavedRegisters;
            VecCalleeSavedRegisters = vecCalleeSavedRegisters;
        }

        public int GetAvailableRegisters(RegisterType type)
        {
            if (type == RegisterType.Integer)
            {
                return IntAvailableRegisters;
            }
            else if (type == RegisterType.Vector)
            {
                return VecAvailableRegisters;
            }
            else
            {
                throw new ArgumentException($"Invalid register type \"{type}\".");
            }
        }
    }
}