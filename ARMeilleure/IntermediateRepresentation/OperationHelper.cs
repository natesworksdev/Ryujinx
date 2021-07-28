using ARMeilleure.Common;

namespace ARMeilleure.IntermediateRepresentation
{
    static class OperationHelper
    {
        public static Operation Operation(Instruction instruction, Operand destination)
        {
            return Operation().With(instruction, destination);
        }

        public static Operation Operation(Instruction instruction, Operand destination,
            Operand[] sources)
        {
            return Operation().With(instruction, destination, sources);
        }

        public static Operation Operation(Instruction instruction, Operand destination, 
            Operand source0)
        {
            return Operation().With(instruction, destination, source0);
        }

        public static Operation Operation(Instruction instruction, Operand destination, 
            Operand source0, Operand source1)
        {
            return Operation().With(instruction, destination, source0, source1);
        }

        public static Operation Operation(Instruction instruction, Operand destination,
            Operand source0, Operand source1, Operand source2)
        {
            return Operation().With(instruction, destination, source0, source1, source2);
        }

        public static Operation Operation(
            Instruction instruction,
            Operand[] destinations,
            Operand[] sources)
        {
            return Operation().With(instruction, destinations, sources);
        }

        #region "ThreadStaticPool"
        public static void PrepareOperationPool(int groupId = 0)
        {
        }

        private static Operation Operation()
        {
            return IntermediateRepresentation.Operation.New();
        }

        public static void ResetOperationPool(int groupId = 0)
        {
        }

        public static void DisposeOperationPools()
        {
        }
        #endregion
    }
}
