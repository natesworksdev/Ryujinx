using System;
using System.IO;

using static Spv.Specification;

namespace Spv.Generator.Test
{
    class Tester
    {
        public class TestModule : Module
        {
            public TestModule() : base(Specification.Version) {}

            protected override void Construct()
            {
                AddCapability(Capability.Shader);
                SetMemoryModel(AddressingModel.Logical, MemoryModel.Simple);

                Instruction floatType = TypeFloat(32);
                Instruction vec4Type = TypeVector(floatType, 4);
                Instruction vec4OutputPtrType = TypePointer(StorageClass.Output, vec4Type);
                Instruction outputColor = Variable(vec4OutputPtrType, StorageClass.Output);

                Name(outputColor, "outputColor");
                AddGlobalVariable(outputColor);

                Instruction rColor = Constant(floatType, 0.5f);
                Instruction gColor = Constant(floatType, 0.0f);
                Instruction bColor = Constant(floatType, 0.0f);
                Instruction aColor = Constant(floatType, 1.0f);

                Instruction compositeColor = ConstantComposite(vec4Type, rColor, gColor, bColor, aColor);

                Instruction voidType = TypeVoid();

                Instruction mainFunctionType = TypeFunction(voidType, true);
                Instruction mainFunction = Function(voidType, FunctionControlMask.MaskNone, mainFunctionType);
                AddLabel(Label());
                Store(outputColor, compositeColor);
                Return();
                FunctionEnd();

                AddEntryPoint(ExecutionModel.Fragment, mainFunction, "main", outputColor);
                AddExecutionMode(mainFunction, ExecutionMode.OriginLowerLeft);
            }
        }

        static void Main(string[] Args)
        {
            Module module = new TestModule();

            byte[] ModuleData = module.Generate();

            File.WriteAllBytes(Args[0], ModuleData);

            Console.WriteLine(Args[0]);
        }
    }
}
