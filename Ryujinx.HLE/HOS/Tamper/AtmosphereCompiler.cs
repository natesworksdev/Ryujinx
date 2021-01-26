using Ryujinx.Common.Logging;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Tamper.Conditions;
using Ryujinx.HLE.HOS.Tamper.Operations;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ryujinx.HLE.HOS.Tamper
{
    public class AtmosphereCompiler
    {
        const byte OpCodeStoreImmA   = 0x0;
        const byte OpCodeBeginCond   = 0x1;
        const byte OpCodeEndCond     = 0x2;
        const byte OpCodeFor         = 0x3;
        const byte OpCodeSet         = 0x4;
        const byte OpCodeLoad        = 0x5;
        const byte OpCodeStoreRegA   = 0x6;
        const byte OpCodeArithmetic1 = 0x7;
        const byte OpCodeInputCond   = 0x8;
        const byte OpCodeArithmetic2 = 0x9;
        const byte OpCodeStoreImRgA  = 0xA;

        /////////////////////////////////////////////

        const int OpCodeIndex  = 0;

        /////////////////////////////////////////////

        const int MemOffExe  = 0;
        const int MemOffHeap = 1;

        /////////////////////////////////////////////

        const int LdWidthIndex = 1;
        const int LdMemIndex   = 2;
        const int LdRegIndex   = 3;
        const int LdAddrIndex  = 6;

        const int LdAddrSize  = 10;

        /////////////////////////////////////////////

        const int SetRegIndex   = 3;
        const int SetValueIndex = 8;

        const int SetValueSize = 16;

        /////////////////////////////////////////////

        const int StRWidthIndex   = 1;
        const int StRAddrRegIndex = 3;
        const int StRDoIncIndex   = 4;
        const int StRDoOffIndex   = 5;
        const int StROffRegIndex  = 6;
        const int StRValueIndex   = 8;

        const int StRValueSize = 16;

        /////////////////////////////////////////////

        const int StIWidthIndex  = 1;
        const int StIMemIndex    = 2;
        const int StIOffRegIndex = 3;
        const int StIOffImmIndex = 6;
        const int StIValueIndex  = 16;

        const int StIOffImmSize = 10;
        const int StIValueSize4 = 8;
        const int StIValueSize8 = 16;

        /////////////////////////////////////////////

        const int StAWidthIndex   = 1;
        const int StASrcRegIndex  = 2;
        const int StAAddrRegIndex = 3;
        const int StADoIncIndex   = 4;
        const int StAOffTypeIndex = 5;
        const int StAOffRegIndex  = 6;
        const int StAOffImmIndex  = 7;

        const int StANoOff           = 0;
        const int StARegOff          = 1;
        const int StAImmOff          = 2;
        const int StAMRWithBaseReg   = 3;
        const int StAMRWithImmOff    = 4;
        const int StAMRWithImmRegOff = 5;

        const int StAValueSize1  = 1;
        const int StAValueSize8  = 9;

        /////////////////////////////////////////////

        const int Ar1WidthIndex  = 1;
        const int Ar1DstRegIndex = 3;
        const int Ar1OpTypeIndex = 4;
        const int Ar1ValueIndex  = 8;

        const int Ar1ValueSize = 8;

        /////////////////////////////////////////////

        const int Ar2WidthIndex  = 1;
        const int Ar2OpTypeIndex = 2;
        const int Ar2DstRegIndex = 3;
        const int Ar2LhsRegIndex = 4;
        const int Ar2UseValIndex = 5; // TODO standardize value / immediate
        const int Ar2RhsRegIndex = 6;
        const int Ar2ValueIndex  = 8;

        const int Ar2ValueSize4  = 8;
        const int Ar2ValueSize8  = 16;

        /////////////////////////////////////////////

        const byte ArOpAdd = 0;
        const byte ArOpSub = 1;
        const byte ArOpMul = 2;
        const byte ArOpLsh = 3;
        const byte ArOpRsh = 4;
        const byte ArOpAnd = 5;
        const byte ArOpOr  = 6;
        const byte ArOpNot = 7;
        const byte ArOpXor = 8;
        const byte ArOpMov = 9;

        /////////////////////////////////////////////

        const int IfWidthIndex    = 1;
        const int IfMemIndex      = 2;
        const int IfCondTypeIndex = 3;
        const int IfOffImmIndex   = 6;
        const int IfValueIndex    = 16;

        const int IfOffImmSize = 10;
        const int IfValueSize4 = 8;
        const int IfValueSize8 = 16;

        /////////////////////////////////////////////

        const int ForModeIndex  = 1;
        const int ForRegIndex   = 3;
        const int ForItersIndex = 8;

        const int ForItersSize = 8;

        const byte ForModeBegin = 0;
        const byte ForModeEnd   = 1;

        /////////////////////////////////////////////

        const byte CmpCondGT = 1;
        const byte CmpCondGE = 2;
        const byte CmpCondLT = 3;
        const byte CmpCondLE = 4;
        const byte CmpCondEQ = 5;
        const byte CmpCondNE = 6;

        /////////////////////////////////////////////

        const int InputMaskIndex = 1;

        const int InputMaskSize = 7;

        /////////////////////////////////////////////

        struct CompilationBlock
        {
            public byte[] BaseInstruction;
            public List<IOperation> Operations;

            public CompilationBlock(byte[] baseInstruction)
            {
                BaseInstruction = baseInstruction;
                Operations = new List<IOperation>();
            }
        }

        struct CompilationData
        {
            public CompilationBlock CurrentBlock { get { return BlockStack.Peek(); } }
            public List<IOperation> CurrentOperations { get { return CurrentBlock.Operations; } }

            public Parameter<IVirtualMemoryManager> Memory;
            public Parameter<long> PressedKeys;
            public Stack<CompilationBlock> BlockStack;
            public Dictionary<byte, Register> Registers;
            public ulong ExeAddress;
            public ulong HeapAddress;

            public CompilationData(ulong exeAddress, ulong heapAddress)
            {
                Memory = new Parameter<IVirtualMemoryManager>(null);
                PressedKeys = new Parameter<long>(0);
                BlockStack = new Stack<CompilationBlock>();
                Registers = new Dictionary<byte, Register>();
                ExeAddress = exeAddress;
                HeapAddress = heapAddress;
            }
        }

        public TamperProgram Compile(IEnumerable<string> rawInstructions, ulong exeAddress, ulong heapAddress)
        {
            try
            {
                return CompileImpl(rawInstructions, exeAddress, heapAddress);
            }
            catch(TamperCompilationException exception)
            {
                // Just print the message without the stack trace.
                Logger.Error?.Print(LogClass.TamperMachine, exception.Message);
            }
            catch (Exception exception)
            {
                Logger.Error?.Print(LogClass.TamperMachine, exception.ToString());
            }

            Logger.Error?.Print(LogClass.TamperMachine, "There was a problem while compiling the Atmosphere cheat");

            return null;
        }

        private TamperProgram CompileImpl(IEnumerable<string> rawInstructions, ulong exeAddress, ulong heapAddress)
        {
            CompilationData data = new CompilationData(exeAddress, heapAddress);
            data.BlockStack.Push(new CompilationBlock(null));

            // Parse the instructions.

            foreach (string rawInstruction in rawInstructions)
            {
                Logger.Debug?.Print(LogClass.TamperMachine, $"Compiling instruction {rawInstruction}");

                byte[] instruction = ParseRawInstruction(rawInstruction);
                byte opcode = instruction[OpCodeIndex];

                switch (opcode)
                {
                    case OpCodeStoreImmA:   EmitStoreImmA  (instruction, ref data); break;
                    case OpCodeBeginCond:   EmitBeginCond  (instruction, ref data); break;
                    case OpCodeEndCond:     EmitEndCond    (instruction, ref data); break;
                    case OpCodeFor:         EmitFor        (instruction, ref data); break;
                    case OpCodeSet:         EmitSet        (instruction, ref data); break;
                    case OpCodeLoad:        EmitLoad       (instruction, ref data); break;
                    case OpCodeStoreRegA:   EmitStoreRegA  (instruction, ref data); break;
                    case OpCodeArithmetic1: EmitArithmetic1(instruction, ref data); break;
                    case OpCodeInputCond  : EmitBeginCond  (instruction, ref data); break;
                    case OpCodeArithmetic2: EmitArithmetic2(instruction, ref data); break;
                    case OpCodeStoreImRgA:  EmitStoreImRgA (instruction, ref data); break;
                    default:
                        throw new TamperCompilationException($"Opcode {opcode} not implemented in Atmosphere cheat");
                }
            }

            // Initialize only the registers used.

            Value<ulong> zero = new Value<ulong>(0UL);
            int position = 0;

            foreach (Register register in data.Registers.Values)
            {
                data.CurrentOperations.Insert(position, new OpMov<ulong>(register, zero));
                position++;
            }

            // TODO check block stack size

            return new TamperProgram(data.Memory, data.PressedKeys, new Block(data.CurrentOperations));
        }

        private void EmitSet(byte[] instruction, ref CompilationData data)
        {
            Register srcReg = GetRegister(instruction[SetRegIndex], ref data);
            ulong value = GetImmediate(instruction, SetValueIndex, SetValueSize);
            Value<ulong> dstValue = new Value<ulong>(value);

            data.CurrentOperations.Add(new OpMov<ulong>(srcReg, dstValue));
        }

        private void EmitLoad(byte[] instruction, ref CompilationData data)
        {
            byte width = instruction[LdWidthIndex];
            byte source = instruction[LdMemIndex];
            Register dstReg = GetRegister(instruction[LdRegIndex], ref data);
            ulong address = GetImmediate(instruction, LdAddrIndex, LdAddrSize);
            address += GetAddressShift(source, ref data);

            Value<ulong> loadAddr = new Value<ulong>(address);
            Pointer srcMem = new Pointer(loadAddr, data.Memory);

            Emit(typeof(OpMov<>), width, ref data, dstReg, srcMem);
        }

        private void EmitStoreImmA(byte[] instruction, ref CompilationData data)
        {
            // 0TMR00AA AAAAAAAA VVVVVVVV (VVVVVVVV)
            // T: Width of memory write(1, 2, 4, or 8 bytes).
            // M: Memory region to write to(0 = Main NSO, 1 = Heap).
            // R: Register to use as an offset from memory region base.
            // A: Immediate offset to use from memory region base.
            // V: Value to write.

            byte width = instruction[StIWidthIndex];
            byte source = instruction[StIMemIndex];
            Register offReg = GetRegister(instruction[StIOffRegIndex], ref data);
            ulong offImm = GetImmediate(instruction, StIOffImmIndex, StIOffImmSize);
            offImm += GetAddressShift(source, ref data);

            ulong value = GetImmediate(instruction, StIValueIndex, width > 4 ? StIValueSize8 : StIValueSize4);
            Value<ulong> storeValue = new Value<ulong>(value);

            Value<ulong> storeAddr = new Value<ulong>(0);
            Value<ulong> offImmValue = new Value<ulong>(offImm);
            data.CurrentOperations.Add(new OpAdd<ulong>(storeAddr, offReg, offImmValue));

            Pointer dstMem = new Pointer(storeAddr, data.Memory);

            Emit(typeof(OpMov<>), width, ref data, dstMem, storeValue);
        }

        private void EmitStoreRegA(byte[] instruction, ref CompilationData data)
        {
            // 6T0RIor0 VVVVVVVV VVVVVVVV
            // T: Width of memory write(1, 2, 4, or 8 bytes).
            // R: Register used as base memory address.
            // I: Increment register flag(0 = do not increment R, 1 = increment R by T).
            // o: Offset register enable flag(0 = do not add r to address, 1 = add r to address).
            // r: Register used as offset when o is 1.
            // V: Value to write to memory.

            byte width = instruction[StRWidthIndex];
            IOperand srcReg = GetRegister(instruction[StRAddrRegIndex], ref data);
            IOperand storeAddr = srcReg;
            byte doIncrement = instruction[StRDoIncIndex];
            byte doOffset = instruction[StRDoOffIndex];
            ulong value = GetImmediate(instruction, StRValueIndex, StRValueSize); // TODO: Optimize to 'width'?
            Value<ulong> storeValue = new Value<ulong>(value);

            switch (doOffset)
            {
                case 0:
                    // Don't offset the address register by another register.
                    break;
                case 1:
                    // Replace the source address by the sum of the base and offset registers.
                    storeAddr = new Value<ulong>(0);
                    IOperand offsetReg = GetRegister(instruction[StROffRegIndex], ref data);
                    data.CurrentOperations.Add(new OpAdd<ulong>(storeAddr, srcReg, offsetReg));
                    break;
                default:
                    throw new TamperCompilationException($"Invalid offset mode {doOffset} in Atmosphere cheat");
            }

            Pointer dstMem = new Pointer(storeAddr, data.Memory);

            Emit(typeof(OpMov<>), width, ref data, dstMem, storeValue);

            switch (doIncrement)
            {
                case 0:
                    // Don't increment the address register by width.
                    break;
                case 1:
                    // Increment the address register by width.
                    IOperand increment = new Value<ulong>(width);
                    data.CurrentOperations.Add(new OpAdd<ulong>(srcReg, srcReg, increment));
                    break;
                default:
                    throw new TamperCompilationException($"Invalid increment mode {doIncrement} in Atmosphere cheat");
            }
        }

        private void EmitStoreImRgA(byte[] instruction, ref CompilationData data)
        {
            // ATSRIOxa (aaaaaaaa)
            // T: Width of memory write (1, 2, 4, or 8 bytes).
            // S: Register to write to memory.
            // R: Register to use as base address.
            // I: Increment register flag (0 = do not increment R, 1 = increment R by T).
            // O: Offset type, see below.
            // x: Register used as offset when O is 1, Memory type when O is 3, 4 or 5.
            // a: Value used as offset when O is 2, 4 or 5.

            /* const int StAWidthIndex   = 1;
               const int StASrcRegIndex  = 2;
               const int StAAddrRegIndex = 3;
               const int StADoIncIndex   = 4;
               const int StAOffTypeIndex = 5;
               const int StAOffRegIndex  = 6;
               const int StAOffImmIndex  = 7;

               const int StANoOff           = 0;
               const int StARegOff          = 1;
               const int StAImmOff          = 2;
               const int StAMRWithBaseReg   = 3;
               const int StAMRWithImmOff    = 4;
               const int StAMRWithImmRegOff = 5;

               const int StAValueSize1  = 1;
               const int StAValueSize8  = 9;*/

            byte width = instruction[StAWidthIndex];
            Register srcReg = GetRegister(instruction[StASrcRegIndex], ref data);
            Register addrReg = GetRegister(instruction[StAAddrRegIndex], ref data);
            byte doIncrement = instruction[StADoIncIndex];
            byte offsetType = instruction[StAOffTypeIndex];
            byte offRegOrMem = instruction[StAOffRegIndex];
            Register offReg = GetRegister(offRegOrMem, ref data);
            ulong offImm = GetImmediate(instruction, StAOffImmIndex, instruction.Length <= 8 ? StAValueSize1 : StAValueSize8);

            Value<ulong> offImmValue;
            IOperand storeAddr;

            switch (offsetType)
            {
                case StANoOff:
                    // *($R) = $S
                    storeAddr = addrReg;
                    break;
                case StARegOff:
                    // *($R + $x) = $S
                    storeAddr = new Value<ulong>(0);
                    data.CurrentOperations.Add(new OpAdd<ulong>(storeAddr, addrReg, offReg));
                    break;
                case StAImmOff:
                    // *(#a) = $S
                    storeAddr = new Value<ulong>(offImm);
                    break;
                case StAMRWithBaseReg:
                    // *(?x + $R) = $S
                    offImm = GetAddressShift(offRegOrMem, ref data);
                    offImmValue = new Value<ulong>(offImm);
                    storeAddr = new Value<ulong>(0);
                    data.CurrentOperations.Add(new OpAdd<ulong>(storeAddr, addrReg, offImmValue));
                    break;
                case StAMRWithImmOff:
                    // *(?x + #a) = $S
                    offImm += GetAddressShift(offRegOrMem, ref data);
                    storeAddr = new Value<ulong>(offImm);
                    break;
                case StAMRWithImmRegOff:
                    // *(?x + #a + $R) = $S
                    offImm += GetAddressShift(offRegOrMem, ref data);
                    offImmValue = new Value<ulong>(offImm);
                    storeAddr = new Value<ulong>(0);
                    data.CurrentOperations.Add(new OpAdd<ulong>(storeAddr, addrReg, offImmValue));
                    break;
                default:
                    throw new TamperCompilationException($"Invalid offset type {offsetType} in Atmosphere cheat");
            }

            Pointer dstMem = new Pointer(storeAddr, data.Memory);

            Emit(typeof(OpMov<>), width, ref data, dstMem, srcReg);

            switch (doIncrement)
            {
                case 0:
                    // Don't increment the address register by width.
                    break;
                case 1:
                    // Increment the address register by width.
                    IOperand increment = new Value<ulong>(width);
                    data.CurrentOperations.Add(new OpAdd<ulong>(addrReg, addrReg, increment));
                    break;
                default:
                    throw new TamperCompilationException($"Invalid increment mode {doIncrement} in Atmosphere cheat");
            }
        }

        private void EmitArithmetic1(byte[] instruction, ref CompilationData data)
        {
            // 7T0RC000 VVVVVVVV
            // T: Width of arithmetic operation(1, 2, 4, or 8 bytes).
            // R: Register to apply arithmetic to.
            // C: Arithmetic operation to apply, see below.
            // V: Value to use for arithmetic operation.

            byte width = instruction[Ar1WidthIndex];
            Register register = GetRegister(instruction[Ar1DstRegIndex], ref data);
            byte operation = instruction[Ar1OpTypeIndex];
            ulong value = GetImmediate(instruction, Ar1ValueIndex, Ar1ValueSize); // TODO: Optimize to 'width'?
            Value<ulong> opValue = new Value<ulong>(value);

            switch (operation)
            {
                case ArOpAdd: Emit(typeof(OpAdd<>), width, ref data, register, register, opValue); break;
                case ArOpSub: Emit(typeof(OpSub<>), width, ref data, register, register, opValue); break;
                case ArOpMul: Emit(typeof(OpMul<>), width, ref data, register, register, opValue); break;
                case ArOpLsh: Emit(typeof(OpLsh<>), width, ref data, register, register, opValue); break;
                case ArOpRsh: Emit(typeof(OpRsh<>), width, ref data, register, register, opValue); break;
                default:
                    throw new TamperCompilationException($"Invalid arithmetic operation {operation} in Atmosphere cheat");
            }
        }

        private void EmitArithmetic2(byte[] instruction, ref CompilationData data)
        {
            // 9TCRS0s0
            // T: Width of arithmetic operation(1, 2, 4, or 8 bytes).
            // C: Arithmetic operation to apply, see below.
            // R: Register to store result in.
            // S: Register to use as left - hand operand.
            // s: Register to use as right - hand operand.

            // 9TCRS100 VVVVVVVV (VVVVVVVV)
            // T: Width of arithmetic operation(1, 2, 4, or 8 bytes).
            // C: Arithmetic operation to apply, see below.
            // R: Register to store result in.
            // S: Register to use as left - hand operand.
            // V: Value to use as right - hand operand.

            byte width = instruction[Ar2WidthIndex];
            byte operation = instruction[Ar2OpTypeIndex];
            Register dstReg = GetRegister(instruction[Ar2DstRegIndex], ref data);
            Register lhsReg = GetRegister(instruction[Ar2LhsRegIndex], ref data);
            byte useValue = instruction[Ar2UseValIndex];
            IOperand rhsOperand;

            switch (useValue)
            {
                case 0:
                    // Use a register as right-hand side.
                    rhsOperand = GetRegister(instruction[Ar2RhsRegIndex], ref data);
                    break;
                case 1:
                    // Use an immediate as right-hand side.
                    ulong value = GetImmediate(instruction, Ar2ValueIndex, width > 4 ? Ar2ValueSize4 : Ar2ValueSize8);
                    rhsOperand = new Value<ulong>(value);
                    break;
                default:
                    throw new TamperCompilationException($"Invalid rhs switch {useValue} in Atmosphere cheat");
            }

            switch (operation)
            {
                case ArOpAdd: Emit(typeof(OpAdd<>), width, ref data, dstReg, lhsReg, rhsOperand); break;
                case ArOpSub: Emit(typeof(OpSub<>), width, ref data, dstReg, lhsReg, rhsOperand); break;
                case ArOpMul: Emit(typeof(OpMul<>), width, ref data, dstReg, lhsReg, rhsOperand); break;
                case ArOpLsh: Emit(typeof(OpLsh<>), width, ref data, dstReg, lhsReg, rhsOperand); break;
                case ArOpRsh: Emit(typeof(OpRsh<>), width, ref data, dstReg, lhsReg, rhsOperand); break;
                case ArOpAnd: Emit(typeof(OpAnd<>), width, ref data, dstReg, lhsReg, rhsOperand); break;
                case ArOpOr : Emit(typeof(OpOr <>), width, ref data, dstReg, lhsReg, rhsOperand); break;
                case ArOpNot: Emit(typeof(OpNot<>), width, ref data, dstReg, lhsReg            ); break;
                case ArOpXor: Emit(typeof(OpXor<>), width, ref data, dstReg, lhsReg, rhsOperand); break;
                case ArOpMov: Emit(typeof(OpMov<>), width, ref data, dstReg, lhsReg            ); break;
                default:
                    throw new TamperCompilationException($"Invalid arithmetic operation {operation} in Atmosphere cheat");
            }
        }

        private void EmitBeginCond(byte[] instruction, ref CompilationData data)
        {
            // Just start a new compilation block and parse the instruction itself at the end.
            data.BlockStack.Push(new CompilationBlock(instruction));
        }

        private void EmitEndCond(byte[] instruction, ref CompilationData data)
        {
            // 1TMC00AA AAAAAAAA VVVVVVVV (VVVVVVVV)
            // T: Width of memory write (1, 2, 4, or 8 bytes).
            // M: Memory region to write to (0 = Main NSO, 1 = Heap).
            // C: Condition to use, see below.
            // A: Immediate offset to use from memory region base.
            // V: Value to compare to.

            // 8kkkkkkk
            // k: Keypad mask to check against, see below.
            // Note that for multiple button combinations, the bitmasks should be ORd together.
            // The Keypad Values are the direct output of hidKeysDown().

            // 20000000

            // Use the conditional begin instruction stored in the stack.
            instruction = data.CurrentBlock.BaseInstruction;

            byte opcode = instruction[OpCodeIndex];

            ICondition condOp;

            switch (opcode)
            {
                case OpCodeBeginCond:
                    condOp = GetIfCondition(instruction, ref data);
                    break;
                case OpCodeInputCond:
                    condOp = GetInputCondition(instruction, ref data);
                    break;
                default:
                    throw new TamperCompilationException($"Conditional end does not match opcode {opcode} in Atmosphere cheat");
            }

            // Create a conditional block with the current operations and nest it in the upper
            // block of the stack.

            IfBlock block = new IfBlock(condOp, data.CurrentOperations);
            data.BlockStack.Pop();
            data.CurrentOperations.Add(block);
        }

        private void EmitFor(byte[] instruction, ref CompilationData data)
        {
            // 300R0000 VVVVVVVV
            // R: Register to use as loop counter.
            // V: Number of iterations to loop.

            // 310R0000

            byte mode = instruction[ForModeIndex];
            byte countRegIndex = instruction[ForRegIndex];

            switch (mode)
            {
                case ForModeBegin:
                    // Just start a new compilation block and parse the instruction itself at the end.
                    data.BlockStack.Push(new CompilationBlock(instruction));
                    return;
                case ForModeEnd:
                    break;
                default:
                    throw new TamperCompilationException($"Invalid loop {mode} in Atmosphere cheat");
            }

            // Use the loop begin instruction stored in the stack.
            instruction = data.CurrentBlock.BaseInstruction;

            byte opcode = instruction[OpCodeIndex];

            if (opcode != OpCodeFor)
            {
                throw new TamperCompilationException($"Loop end does not match opcode {opcode} in Atmosphere cheat");
            }

            byte newCountRegIndex = instruction[ForRegIndex];
            Register countReg = GetRegister(countRegIndex, ref data);
            ulong countImm = GetImmediate(instruction, ForItersIndex, ForItersSize);

            if (countRegIndex != newCountRegIndex)
            {
                throw new TamperCompilationException($"The register used for the loop changed from {countRegIndex} to {newCountRegIndex} in Atmosphere cheat");
            }

            // Create a loop block with the current operations and nest it in the upper
            // block of the stack.

            ForBlock block = new ForBlock(countImm, countReg, data.CurrentOperations);
            data.BlockStack.Pop();
            data.CurrentOperations.Add(block);
        }

        private void Emit(Type instruction, byte width, ref CompilationData data, params IOperand[] operands)
        {
            data.CurrentOperations.Add((IOperation)Create(instruction, width, operands));
        }

        private Object Create(Type instruction, byte width, params IOperand[] operands)
        {
            Type realType;

            switch (width)
            {
                case 1: realType = instruction.MakeGenericType(typeof(byte  )); break;
                case 2: realType = instruction.MakeGenericType(typeof(ushort)); break;
                case 4: realType = instruction.MakeGenericType(typeof(uint  )); break;
                case 8: realType = instruction.MakeGenericType(typeof(ulong )); break;
                default:
                    throw new TamperCompilationException($"Invalid instruction width {width} in Atmosphere cheat");
            }

            return Activator.CreateInstance(realType, operands);
        }

        private ICondition GetIfCondition(byte[] instruction, ref CompilationData data)
        {
            byte width = instruction[IfWidthIndex];
            byte source = instruction[IfMemIndex];
            byte condition = instruction[IfCondTypeIndex];

            ulong address = GetImmediate(instruction, IfOffImmIndex, IfOffImmSize);
            address += GetAddressShift(source, ref data);

            Value<ulong> loadAddr = new Value<ulong>(address);
            Pointer srcMem = new Pointer(loadAddr, data.Memory);

            ulong value = GetImmediate(instruction, IfValueIndex, width > 4 ? IfValueSize4 : IfValueSize8);
            Value<ulong> compValue = new Value<ulong>(address);

            switch (condition)
            {
                case CmpCondGT: return (ICondition)Create(typeof(CondGT<>), width, srcMem, compValue);
                case CmpCondGE: return (ICondition)Create(typeof(CondGE<>), width, srcMem, compValue);
                case CmpCondLT: return (ICondition)Create(typeof(CondLT<>), width, srcMem, compValue);
                case CmpCondLE: return (ICondition)Create(typeof(CondLE<>), width, srcMem, compValue);
                case CmpCondEQ: return (ICondition)Create(typeof(CondEQ<>), width, srcMem, compValue);
                case CmpCondNE: return (ICondition)Create(typeof(CondNE<>), width, srcMem, compValue);
                default:
                    throw new TamperCompilationException($"Invalid condition {condition} in Atmosphere cheat");
            }
        }

        private ICondition GetInputCondition(byte[] instruction, ref CompilationData data)
        {
            ulong mask = GetImmediate(instruction, InputMaskIndex, InputMaskSize);
            return new InputMask((long)mask, data.PressedKeys);
        }

        private ulong GetImmediate(byte[] instruction, int index, int quartetCount)
        {
            ulong value = 0;

            for (int i = 0; i < quartetCount; i++)
            {
                value <<= 4;
                value |= (ulong)instruction[index + i];
            }

            return value;
        }

        private ulong GetAddressShift(byte source, ref CompilationData data) // TODO address -> position?
        {
            switch (source)
            {
                case MemOffExe:
                    // Memory address is relative to the code start.
                    return data.ExeAddress;
                case MemOffHeap:
                    // Memory address is relative to the heap.
                    return data.HeapAddress;
                default:
                    throw new TamperCompilationException($"Invalid memory source {source} in Atmosphere cheat");
            }
        }

        private Register GetRegister(byte index, ref CompilationData data)
        {
            if (data.Registers.TryGetValue(index, out Register register))
            {
                return register;
            }

            register = new Register();
            data.Registers.Add(index, register);

            return register;
        }

        private byte[] ParseRawInstruction(string rawInstruction)
        {
            const int wordSize = 2 * sizeof(uint);

            // Instructions are multi-word, with 32bit words. Split the raw instruction
            // and parse each word into individual quartets of bits.

            var words = rawInstruction.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            byte[] instruction = new byte[wordSize * words.Length];

            if (words.Length == 0)
            {
                throw new TamperCompilationException("Empty instruction in Atmosphere cheat");
            }

            for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
            {
                string word = words[wordIndex];

                if (word.Length != wordSize)
                {
                    throw new TamperCompilationException($"Invalid word length for {word} in Atmosphere cheat");
                }

                for (int quartetIndex = 0; quartetIndex < wordSize; quartetIndex++)
                {
                    int index = wordIndex * wordSize + quartetIndex;
                    string byteData = word.Substring(quartetIndex, 1);

                    instruction[index] = byte.Parse(byteData, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
            }

            return instruction;
        }
    }
}
