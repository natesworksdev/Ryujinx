using ARMeilleure.IntermediateRepresentation;
using System;

namespace ARMeilleure.CodeGen.RegisterAllocators
{
    readonly struct RegisterMasks
    {
        public readonly int IntAvailableRegisters   { get; }
        public readonly int VecAvailableRegisters   { get; }
        public readonly int IntCallerSavedRegisters { get; }
        public readonly int VecCallerSavedRegisters { get; }
        public readonly int IntCalleeSavedRegisters { get; }
        public readonly int VecCalleeSavedRegisters { get; }

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

        public readonly int GetAvailableRegisters(RegisterType type)
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