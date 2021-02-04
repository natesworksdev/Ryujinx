using Ryujinx.Common.Logging;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Tamper.Atmosphere.Conditions;
using Ryujinx.HLE.HOS.Tamper.Atmosphere.Operations;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ryujinx.HLE.HOS.Tamper.Atmosphere
{
    internal class AtmosphereCompiler
    {
        const byte OpCodeStoreImmA    = 0x0;
        const byte OpCodeBeginCond    = 0x1;
        const byte OpCodeEndCond      = 0x2;
        const byte OpCodeFor          = 0x3;
        const byte OpCodeSet          = 0x4;
        const byte OpCodeLoad         = 0x5;
        const byte OpCodeStoreRegA    = 0x6;
        const byte OpCodeArithmetic1  = 0x7;
        const byte OpCodeInputCond    = 0x8;
        const byte OpCodeArithmetic2  = 0x9;
        const byte OpCodeStoreImRgA   = 0xA;
        const ushort Ex2OpCodeCond2   = 0xC0;
        const ushort Ex2OpCodeRegSR   = 0xC1;
        const ushort Ex2OpCodeRegSRM  = 0xC2;
        const ushort Ex2OpCodeRegRWS  = 0xC3;
        const ushort Ex3OpCodePause   = 0xFF0;
        const ushort Ex3OpCodeResume  = 0xFF1;
        const ushort Ex3OpCodeLog     = 0xFFF;

        /////////////////////////////////////////////

        const int OpCodeIndex = 0;

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

        const int Ar2ValueSize4  = 8; // TODO fix space
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

        const int LogWidthIndex   = 3;
        const int LogLogIdIndex   = 4;
        const int LogOpTypeIndex  = 5;
        const int LogSourceIndex  = 6;
        const int LogOffsetIndex  = 7;

        const int LogModeWithImmOff    = 0;
        const int LogModeWithRegOff    = 1;
        const int LogAddrRegWithImmOff = 2;
        const int LogAddrRegWithRegOff = 3;
        const int LogAddrReg           = 4;

        const int LogOffImmSize = 9;

        /////////////////////////////////////////////

        const int If2WidthIndex = 2;
        const int If2CondIndex = 3;
        const int If2SrcRegIndex = 4;
        const int If2OpTypeIndex = 5;
        const int If2SourceIndex = 6;
        const int If2OffsetIndex = 7;
        const int If2ValueIndex = 8;

        const int If2ModeWithImmOff = 0;
        const int If2ModeWithRegOff = 1;
        const int If2AddrRegWithImmOff = 2;
        const int If2AddrRegWithRegOff = 3;
        const int If2ImmValue = 4;
        const int If2AddrReg = 5;

        const int If2OffImmSize = 9;
        const int If2ValueSize8 = 8;
        const int If2ValueSize16 = 16;

        /////////////////////////////////////////////

        const int SRDstRegIndex = 3;
        const int SRSrcRegIndex = 5;
        const int SROpTypeIndex = 6;

        /////////////////////////////////////////////

        const int SRMOpTypeIndex  = 2;
        const int SRMRegMaskIndex = 4;

        const int SRMRegMaskSize = 4;

        /////////////////////////////////////////////

        const int RWSStaticRegIndex  = 5;
        const int RWSRegIndex        = 7;

        const byte RWSFirstWriteReg = 0x80;

        const int RWSStaticRegSize = 2;

        /////////////////////////////////////////////

        const int RegOpRestore    = 0;
        const int RegOpSave       = 1;
        const int RegOpClearSaved = 2;
        const int RegOpClear      = 3;

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

        class CompilationData
        {
            public CompilationBlock CurrentBlock { get { return BlockStack.Peek(); } }
            public List<IOperation> CurrentOperations { get { return CurrentBlock.Operations; } }

            public ITamperedProcess Process { get; }
            public Parameter<long> PressedKeys { get; }
            public Stack<CompilationBlock> BlockStack { get; }
            public Dictionary<byte, Register> Registers { get; }
            public Dictionary<byte, Register> SavedRegisters { get; }
            public Dictionary<byte, Register> StaticRegisters { get; }
            public ulong ExeAddress { get; }
            public ulong HeapAddress { get; }

            public CompilationData(ulong exeAddress, ulong heapAddress, ITamperedProcess process)
            {
                Process = process;
                PressedKeys = new Parameter<long>(0);
                BlockStack = new Stack<CompilationBlock>();
                Registers = new Dictionary<byte, Register>();
                SavedRegisters = new Dictionary<byte, Register>();
                StaticRegisters = new Dictionary<byte, Register>();
                ExeAddress = exeAddress;
                HeapAddress = heapAddress;
            }
        }

        public ITamperProgram Compile(IEnumerable<string> rawInstructions, ulong exeAddress, ulong heapAddress, ITamperedProcess process)
        {
            try
            {
                return CompileImpl(rawInstructions, exeAddress, heapAddress, process);
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

        private ITamperProgram CompileImpl(IEnumerable<string> rawInstructions, ulong exeAddress, ulong heapAddress, ITamperedProcess process)
        {
            CompilationData cData = new CompilationData(exeAddress, heapAddress, process);
            cData.BlockStack.Push(new CompilationBlock(null));

            // Parse the instructions.

            foreach (string rawInstruction in rawInstructions)
            {
                Logger.Debug?.Print(LogClass.TamperMachine, $"Compiling instruction {rawInstruction}");

                byte[] instruction = ParseRawInstruction(rawInstruction);
                ushort opcode = GetExtendedOpcode(instruction);

                switch (opcode)
                {
                    case OpCodeStoreImmA:   EmitStoreImmA  (instruction, cData); break;
                    case OpCodeBeginCond:   EmitBeginCond  (instruction, cData); break;
                    case OpCodeEndCond:     EmitEndCond    (instruction, cData); break;
                    case OpCodeFor:         EmitFor        (instruction, cData); break;
                    case OpCodeSet:         EmitSet        (instruction, cData); break;
                    case OpCodeLoad:        EmitLoad       (instruction, cData); break;
                    case OpCodeStoreRegA:   EmitStoreRegA  (instruction, cData); break;
                    case OpCodeArithmetic1: EmitArithmetic1(instruction, cData); break;
                    case OpCodeInputCond:   EmitBeginCond  (instruction, cData); break;
                    case OpCodeArithmetic2: EmitArithmetic2(instruction, cData); break;
                    case OpCodeStoreImRgA:  EmitStoreImRgA (instruction, cData); break;
                    case Ex2OpCodeCond2:    EmitBeginCond  (instruction, cData); break;
                    case Ex2OpCodeRegSR:    EmitRegSR      (instruction, cData); break;
                    case Ex2OpCodeRegSRM:   EmitRegSRM     (instruction, cData); break;
                    case Ex2OpCodeRegRWS:   EmitRegRWS     (instruction, cData); break;
                    case Ex3OpCodePause:    EmitPause      (instruction, cData); break;
                    case Ex3OpCodeResume:   EmitResume     (instruction, cData); break;
                    case Ex3OpCodeLog:      EmitLog        (instruction, cData); break;
                    default:
                        throw new TamperCompilationException($"Opcode {opcode} not implemented in Atmosphere cheat");
                }
            }

            // Initialize only the registers used.

            Value<ulong> zero = new Value<ulong>(0UL);
            int position = 0;

            foreach (Register register in cData.Registers.Values)
            {
                cData.CurrentOperations.Insert(position, new OpMov<ulong>(register, zero));
                position++;
            }

            // TODO check block stack size

            return new AtmosphereProgram(process, cData.PressedKeys, new Block(cData.CurrentOperations));
        }

        private void EmitSet(byte[] instruction, CompilationData cData)
        {
            Register srcReg = GetRegister(instruction[SetRegIndex], cData);
            ulong value = GetImmediate(instruction, SetValueIndex, SetValueSize);
            Value<ulong> dstValue = new Value<ulong>(value);

            cData.CurrentOperations.Add(new OpMov<ulong>(srcReg, dstValue));
        }

        private void EmitLoad(byte[] instruction, CompilationData cData)
        {
            byte width = instruction[LdWidthIndex];
            byte source = instruction[LdMemIndex];
            Register dstReg = GetRegister(instruction[LdRegIndex], cData);
            ulong address = GetImmediate(instruction, LdAddrIndex, LdAddrSize);
            Pointer srcMem = EmitPointer(source, address, cData);

            Emit(typeof(OpMov<>), width, cData, dstReg, srcMem);
        }

        private void EmitStoreImmA(byte[] instruction, CompilationData cData)
        {
            // 0TMR00AA AAAAAAAA VVVVVVVV (VVVVVVVV)
            // T: Width of memory write(1, 2, 4, or 8 bytes).
            // M: Memory region to write to(0 = Main NSO, 1 = Heap).
            // R: Register to use as an offset from memory region base.
            // A: Immediate offset to use from memory region base.
            // V: Value to write.

            byte width = instruction[StIWidthIndex];
            byte source = instruction[StIMemIndex];
            Register offReg = GetRegister(instruction[StIOffRegIndex], cData);
            ulong offImm = GetImmediate(instruction, StIOffImmIndex, StIOffImmSize);

            Pointer dstMem = EmitPointer(source, offReg, offImm, cData);

            ulong value = GetImmediate(instruction, StIValueIndex, width > 4 ? StIValueSize8 : StIValueSize4);
            Value<ulong> storeValue = new Value<ulong>(value);

            Emit(typeof(OpMov<>), width, cData, dstMem, storeValue);
        }

        private void EmitStoreRegA(byte[] instruction, CompilationData cData)
        {
            // 6T0RIor0 VVVVVVVV VVVVVVVV
            // T: Width of memory write(1, 2, 4, or 8 bytes).
            // R: Register used as base memory address.
            // I: Increment register flag(0 = do not increment R, 1 = increment R by T).
            // o: Offset register enable flag(0 = do not add r to address, 1 = add r to address).
            // r: Register used as offset when o is 1.
            // V: Value to write to memory.

            byte width = instruction[StRWidthIndex];
            Register srcReg = GetRegister(instruction[StRAddrRegIndex], cData);
            byte doIncrement = instruction[StRDoIncIndex];
            byte doOffset = instruction[StRDoOffIndex];
            ulong value = GetImmediate(instruction, StRValueIndex, StRValueSize); // TODO: Optimize to 'width'?
            Value<ulong> storeValue = new Value<ulong>(value);

            Pointer dstMem;

            switch (doOffset)
            {
                case 0:
                    // Don't offset the address register by another register.
                    dstMem = EmitPointer(srcReg, cData);
                    break;
                case 1:
                    // Replace the source address by the sum of the base and offset registers.
                    Register offsetReg = GetRegister(instruction[StROffRegIndex], cData);
                    dstMem = EmitPointer(srcReg, offsetReg, cData);
                    break;
                default:
                    throw new TamperCompilationException($"Invalid offset mode {doOffset} in Atmosphere cheat");
            }

            Emit(typeof(OpMov<>), width, cData, dstMem, storeValue);

            switch (doIncrement)
            {
                case 0:
                    // Don't increment the address register by width.
                    break;
                case 1:
                    // Increment the address register by width.
                    IOperand increment = new Value<ulong>(width);
                    cData.CurrentOperations.Add(new OpAdd<ulong>(srcReg, srcReg, increment));
                    break;
                default:
                    throw new TamperCompilationException($"Invalid increment mode {doIncrement} in Atmosphere cheat");
            }
        }

        private void EmitStoreImRgA(byte[] instruction, CompilationData cData)
        {
            // ATSRIOxa (aaaaaaaa)
            // T: Width of memory write (1, 2, 4, or 8 bytes).
            // S: Register to write to memory.
            // R: Register to use as base address.
            // I: Increment register flag (0 = do not increment R, 1 = increment R by T).
            // O: Offset type, see below.
            // x: Register used as offset when O is 1, Memory type when O is 3, 4 or 5.
            // a: Value used as offset when O is 2, 4 or 5.

            byte width = instruction[StAWidthIndex];
            Register srcReg = GetRegister(instruction[StASrcRegIndex], cData);
            Register addrReg = GetRegister(instruction[StAAddrRegIndex], cData);
            byte doIncrement = instruction[StADoIncIndex];
            byte offsetType = instruction[StAOffTypeIndex];
            byte offRegOrMem = instruction[StAOffRegIndex];
            Register offReg = GetRegister(offRegOrMem, cData);
            ulong offImm = GetImmediate(instruction, StAOffImmIndex, instruction.Length <= 8 ? StAValueSize1 : StAValueSize8);

            Pointer dstMem;

            switch (offsetType)
            {
                case StANoOff:
                    // *($R) = $S
                    dstMem = EmitPointer(addrReg, cData);
                    break;
                case StARegOff:
                    // *($R + $x) = $S
                    dstMem = EmitPointer(addrReg, offReg, cData);
                    break;
                case StAImmOff:
                    // *(#a) = $S
                    dstMem = EmitPointer(offImm, cData);
                    break;
                case StAMRWithBaseReg:
                    // *(?x + $R) = $S
                    dstMem = EmitPointer(offRegOrMem, addrReg, cData);
                    break;
                case StAMRWithImmOff:
                    // *(?x + #a) = $S
                    dstMem = EmitPointer(offRegOrMem, offImm, cData);
                    break;
                case StAMRWithImmRegOff:
                    // *(?x + #a + $R) = $S
                    dstMem = EmitPointer(offRegOrMem, addrReg, offImm, cData);
                    break;
                default:
                    throw new TamperCompilationException($"Invalid offset type {offsetType} in Atmosphere cheat");
            }

            Emit(typeof(OpMov<>), width, cData, dstMem, srcReg);

            switch (doIncrement)
            {
                case 0:
                    // Don't increment the address register by width.
                    break;
                case 1:
                    // Increment the address register by width.
                    IOperand increment = new Value<ulong>(width);
                    cData.CurrentOperations.Add(new OpAdd<ulong>(addrReg, addrReg, increment));
                    break;
                default:
                    throw new TamperCompilationException($"Invalid increment mode {doIncrement} in Atmosphere cheat");
            }
        }

        private void EmitArithmetic1(byte[] instruction, CompilationData cData)
        {
            // 7T0RC000 VVVVVVVV
            // T: Width of arithmetic operation(1, 2, 4, or 8 bytes).
            // R: Register to apply arithmetic to.
            // C: Arithmetic operation to apply, see below.
            // V: Value to use for arithmetic operation.

            byte width = instruction[Ar1WidthIndex];
            Register register = GetRegister(instruction[Ar1DstRegIndex], cData);
            byte operation = instruction[Ar1OpTypeIndex];
            ulong value = GetImmediate(instruction, Ar1ValueIndex, Ar1ValueSize); // TODO: Optimize to 'width'?
            Value<ulong> opValue = new Value<ulong>(value);

            switch (operation)
            {
                case ArOpAdd: Emit(typeof(OpAdd<>), width, cData, register, register, opValue); break;
                case ArOpSub: Emit(typeof(OpSub<>), width, cData, register, register, opValue); break;
                case ArOpMul: Emit(typeof(OpMul<>), width, cData, register, register, opValue); break;
                case ArOpLsh: Emit(typeof(OpLsh<>), width, cData, register, register, opValue); break;
                case ArOpRsh: Emit(typeof(OpRsh<>), width, cData, register, register, opValue); break;
                default:
                    throw new TamperCompilationException($"Invalid arithmetic operation {operation} in Atmosphere cheat");
            }
        }

        private void EmitArithmetic2(byte[] instruction, CompilationData cData)
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
            Register dstReg = GetRegister(instruction[Ar2DstRegIndex], cData);
            Register lhsReg = GetRegister(instruction[Ar2LhsRegIndex], cData);
            byte useValue = instruction[Ar2UseValIndex];
            IOperand rhsOperand;

            switch (useValue)
            {
                case 0:
                    // Use a register as right-hand side.
                    rhsOperand = GetRegister(instruction[Ar2RhsRegIndex], cData);
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
                case ArOpAdd: Emit(typeof(OpAdd<>), width, cData, dstReg, lhsReg, rhsOperand); break;
                case ArOpSub: Emit(typeof(OpSub<>), width, cData, dstReg, lhsReg, rhsOperand); break;
                case ArOpMul: Emit(typeof(OpMul<>), width, cData, dstReg, lhsReg, rhsOperand); break;
                case ArOpLsh: Emit(typeof(OpLsh<>), width, cData, dstReg, lhsReg, rhsOperand); break;
                case ArOpRsh: Emit(typeof(OpRsh<>), width, cData, dstReg, lhsReg, rhsOperand); break;
                case ArOpAnd: Emit(typeof(OpAnd<>), width, cData, dstReg, lhsReg, rhsOperand); break;
                case ArOpOr : Emit(typeof(OpOr <>), width, cData, dstReg, lhsReg, rhsOperand); break;
                case ArOpNot: Emit(typeof(OpNot<>), width, cData, dstReg, lhsReg            ); break;
                case ArOpXor: Emit(typeof(OpXor<>), width, cData, dstReg, lhsReg, rhsOperand); break;
                case ArOpMov: Emit(typeof(OpMov<>), width, cData, dstReg, lhsReg            ); break;
                default:
                    throw new TamperCompilationException($"Invalid arithmetic operation {operation} in Atmosphere cheat");
            }
        }

        private void EmitBeginCond(byte[] instruction, CompilationData cData)
        {
            // Just start a new compilation block and parse the instruction itself at the end.
            cData.BlockStack.Push(new CompilationBlock(instruction));
        }

        private void EmitEndCond(byte[] instruction, CompilationData cData)
        {
            // 20000000

            // Use the conditional begin instruction stored in the stack.
            instruction = cData.CurrentBlock.BaseInstruction;
            ushort opcode = GetExtendedOpcode(instruction);

            // Pop the current block of operations from the stack so control instructions
            // for the conditional can be emitted in the upper block.
            IEnumerable<IOperation> operations = cData.CurrentOperations;
            cData.BlockStack.Pop();

            ICondition condOp;

            switch (opcode)
            {
                case OpCodeBeginCond:
                    condOp = GetIfCondition(instruction, cData);
                    break;
                case OpCodeInputCond:
                    condOp = GetInputCondition(instruction, cData);
                    break;
                case Ex2OpCodeCond2:
                    condOp = GetIf2Condition(instruction, cData);
                    break;
                default:
                    throw new TamperCompilationException($"Conditional end does not match opcode {opcode} in Atmosphere cheat");
            }

            // Create a conditional block with the current operations and nest it in the upper
            // block of the stack.

            IfBlock block = new IfBlock(condOp, operations);
            cData.CurrentOperations.Add(block);
        }

        private void EmitFor(byte[] instruction, CompilationData cData)
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
                    cData.BlockStack.Push(new CompilationBlock(instruction));
                    return;
                case ForModeEnd:
                    break;
                default:
                    throw new TamperCompilationException($"Invalid loop {mode} in Atmosphere cheat");
            }

            // Use the loop begin instruction stored in the stack.
            instruction = cData.CurrentBlock.BaseInstruction;

            byte opcode = instruction[OpCodeIndex];

            if (opcode != OpCodeFor)
            {
                throw new TamperCompilationException($"Loop end does not match opcode {opcode} in Atmosphere cheat");
            }

            byte newCountRegIndex = instruction[ForRegIndex];
            Register countReg = GetRegister(countRegIndex, cData);
            ulong countImm = GetImmediate(instruction, ForItersIndex, ForItersSize);

            if (countRegIndex != newCountRegIndex)
            {
                throw new TamperCompilationException($"The register used for the loop changed from {countRegIndex} to {newCountRegIndex} in Atmosphere cheat");
            }

            // Create a loop block with the current operations and nest it in the upper
            // block of the stack.

            ForBlock block = new ForBlock(countImm, countReg, cData.CurrentOperations);
            cData.BlockStack.Pop();
            cData.CurrentOperations.Add(block);
        }

        private void EmitRegSR(byte[] instruction, CompilationData cData)
        {
            // C10D0Sx0
            // D: Destination index.
            // S: Source index.
            // x: Operand Type, see below.

            byte opType = instruction[SROpTypeIndex];
            byte dstRegIndex = instruction[SRDstRegIndex];
            byte srcRegIndex = instruction[SRSrcRegIndex];
            EmitRegSR(opType, dstRegIndex, srcRegIndex, cData);
        }

        private void EmitRegSR(byte opType, byte dstRegIndex, byte srcRegIndex, CompilationData cData)
        {
            IOperand dstOp;
            IOperand srcOp;

            switch (opType)
            {
                case RegOpRestore:
                    dstOp = GetRegister(dstRegIndex, cData);
                    srcOp = GetSavedRegister(srcRegIndex, cData);
                    break;
                case RegOpSave:
                    dstOp = GetSavedRegister(dstRegIndex, cData);
                    srcOp = GetRegister(srcRegIndex, cData);
                    break;
                case RegOpClearSaved:
                    dstOp = new Value<ulong>(0);
                    srcOp = GetSavedRegister(srcRegIndex, cData);
                    break;
                case RegOpClear:
                    dstOp = new Value<ulong>(0);
                    srcOp = GetRegister(srcRegIndex, cData);
                    break;
                default:
                    throw new TamperCompilationException($"Invalid register operation type {opType} in Atmosphere cheat");
            }

            cData.CurrentOperations.Add(new OpMov<ulong>(dstOp, srcOp));
        }

        private void EmitRegSRM(byte[] instruction, CompilationData cData)
        {
            // C2x0XXXX
            // x: Operand Type, see below.
            // X: 16-bit bitmask, bit i == save or restore register i.

            byte opType = instruction[SRMOpTypeIndex];
            ulong mask = GetImmediate(instruction, SRMRegMaskIndex, SRMRegMaskSize);

            for (byte regIndex = 0; mask != 0; mask >>= 1, regIndex++)
            {
                if ((mask & 0x1) != 0)
                {
                    EmitRegSR(opType, regIndex, regIndex, cData);
                }
            }
        }

        private void EmitRegRWS(byte[] instruction, CompilationData cData)
        {
            // C3000XXx
            // XX: Static register index, 0x00 to 0x7F for reading or 0x80 to 0xFF for writing.
            // x: Register index.

            ulong staticRegIndex = GetImmediate(instruction, RWSStaticRegIndex, RWSStaticRegSize);
            Register register = GetRegister(instruction[RWSRegIndex], cData);

            IOperand srcReg;
            IOperand dstReg;

            if (staticRegIndex < RWSFirstWriteReg)
            {
                // Read from static register.
                srcReg = GetStaticRegister((byte)staticRegIndex, cData);
                dstReg = register;
            }
            else
            {
                // Write to static register.
                srcReg = register;
                dstReg = GetStaticRegister((byte)(staticRegIndex - RWSFirstWriteReg), cData);
            }

            cData.CurrentOperations.Add(new OpMov<ulong>(dstReg, srcReg));
        }

        private void EmitPause(byte[] instruction, CompilationData cData)
        {
            cData.CurrentOperations.Add(new OpProcCtrl(cData.Process, true));
        }

        private void EmitResume(byte[] instruction, CompilationData cData)
        {
            cData.CurrentOperations.Add(new OpProcCtrl(cData.Process, false));
        }

        private void EmitLog(byte[] instruction, CompilationData cData)
        {
            // FFFTIX##
            // FFFTI0Ma aaaaaaaa
            // FFFTI1Mr
            // FFFTI2Ra aaaaaaaa
            // FFFTI3Rr
            // FFFTI4V0
            // T: Width of memory write (1, 2, 4, or 8 bytes).
            // I: Log id.
            // X: Operand Type, see below.
            // M: Memory Type (operand types 0 and 1).
            // R: Address Register (operand types 2 and 3).
            // a: Relative Address (operand types 0 and 2).
            // r: Offset Register (operand types 1 and 3).
            // V: Value Register (operand type 4).

            byte width = instruction[LogWidthIndex];
            byte logId = instruction[LogLogIdIndex];
            byte opType = instruction[LogOpTypeIndex];
            byte addrRegOrMem = instruction[LogSourceIndex];
            byte offRegOrImm = instruction[LogOffsetIndex];
            ulong offImm;
            Register addrReg;
            Register offReg;
            IOperand srcOp;

            switch (opType)
            {
                case LogModeWithImmOff:
                    // *(?x + #a)
                    offImm = GetImmediate(instruction, LogOffsetIndex, LogOffImmSize);
                    srcOp = EmitPointer(addrRegOrMem, offImm, cData);
                    break;
                case LogModeWithRegOff:
                    // *(?x + $r)
                    offReg = GetRegister(instruction[offRegOrImm], cData);
                    srcOp = EmitPointer(addrRegOrMem, offReg, cData);
                    break;
                case LogAddrRegWithImmOff:
                    // *($R + #a)
                    addrReg = GetRegister(instruction[addrRegOrMem], cData);
                    offImm = GetImmediate(instruction, LogOffsetIndex, LogOffImmSize);
                    srcOp = EmitPointer(addrReg, offImm, cData);
                    break;
                case LogAddrRegWithRegOff:
                    // *($R + $r)
                    addrReg = GetRegister(instruction[addrRegOrMem], cData);
                    offReg = GetRegister(instruction[offRegOrImm], cData);
                    srcOp = EmitPointer(addrReg, offReg, cData);
                    break;
                case LogAddrReg:
                    // $V
                    srcOp = GetRegister(instruction[addrRegOrMem], cData);
                    break;
                default:
                    throw new TamperCompilationException($"Invalid operand type {opType} in Atmosphere cheat");
            }

            Emit(typeof(OpLog<>), width, cData, logId, srcOp);
        }

        private Pointer EmitPointer(ulong addressImmediate, CompilationData cData)
        {
            Value<ulong> addressImmediateValue = new Value<ulong>(addressImmediate);
            return new Pointer(addressImmediateValue, cData.Process);
        }

        private Pointer EmitPointer(Register addressRegister, CompilationData cData)
        {
            return new Pointer(addressRegister, cData.Process);
        }

        private Pointer EmitPointer(Register addressRegister, ulong offsetImmediate, CompilationData cData)
        {
            Value<ulong> offsetImmediateValue = new Value<ulong>(offsetImmediate);
            Value<ulong> finalAddressValue = new Value<ulong>(0);
            cData.CurrentOperations.Add(new OpAdd<ulong>(finalAddressValue, addressRegister, offsetImmediateValue));
            return new Pointer(finalAddressValue, cData.Process);
        }

        private Pointer EmitPointer(Register addressRegister, Register offsetRegister, CompilationData cData)
        {
            Value<ulong> finalAddressValue = new Value<ulong>(0);
            cData.CurrentOperations.Add(new OpAdd<ulong>(finalAddressValue, addressRegister, offsetRegister));
            return new Pointer(finalAddressValue, cData.Process);
        }

        private Pointer EmitPointer(Register addressRegister, Register offsetRegister, ulong offsetImmediate, CompilationData cData)
        {
            Value<ulong> offsetImmediateValue = new Value<ulong>(offsetImmediate);
            Value<ulong> finalOffsetValue = new Value<ulong>(0);
            cData.CurrentOperations.Add(new OpAdd<ulong>(finalOffsetValue, offsetRegister, offsetImmediateValue));
            Value<ulong> finalAddressValue = new Value<ulong>(0);
            cData.CurrentOperations.Add(new OpAdd<ulong>(finalAddressValue, addressRegister, finalOffsetValue));
            return new Pointer(finalAddressValue, cData.Process);
        }

        private Pointer EmitPointer(byte memorySource, ulong offsetImmediate, CompilationData cData)
        {
            offsetImmediate += GetAddressShift(memorySource, cData);
            return EmitPointer(offsetImmediate, cData);
        }

        private Pointer EmitPointer(byte memorySource, Register offsetRegister, CompilationData cData)
        {
            ulong offsetImmediate = GetAddressShift(memorySource, cData);
            return EmitPointer(offsetRegister, offsetImmediate, cData);
        }

        private Pointer EmitPointer(byte memorySource, Register offsetRegister, ulong offsetImmediate, CompilationData cData)
        {
            offsetImmediate += GetAddressShift(memorySource, cData);
            return EmitPointer(offsetRegister, offsetImmediate, cData);
        }

        private void Emit(Type instruction, byte width, CompilationData cData, params Object[] operands)
        {
            cData.CurrentOperations.Add((IOperation)Create(instruction, width, operands));
        }

        private Object Create(Type instruction, byte width, params Object[] operands)
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

        private ICondition GetIfCondition(byte[] instruction, CompilationData cData)
        {
            // 1TMC00AA AAAAAAAA VVVVVVVV (VVVVVVVV)
            // T: Width of memory write (1, 2, 4, or 8 bytes).
            // M: Memory region to write to (0 = Main NSO, 1 = Heap).
            // C: Condition to use, see below.
            // A: Immediate offset to use from memory region base.
            // V: Value to compare to.

            byte width = instruction[IfWidthIndex];
            byte source = instruction[IfMemIndex];
            byte condition = instruction[IfCondTypeIndex];

            ulong address = GetImmediate(instruction, IfOffImmIndex, IfOffImmSize);
            Pointer srcMem = EmitPointer(source, address, cData);

            ulong value = GetImmediate(instruction, IfValueIndex, width > 4 ? IfValueSize4 : IfValueSize8);
            Value<ulong> compValue = new Value<ulong>(address);

            return GetCondition(condition, width, srcMem, compValue);
        }

        private ICondition GetInputCondition(byte[] instruction, CompilationData cData)
        {
            // 8kkkkkkk
            // k: Keypad mask to check against, see below.
            // Note that for multiple button combinations, the bitmasks should be ORd together.
            // The Keypad Values are the direct output of hidKeysDown().

            ulong mask = GetImmediate(instruction, InputMaskIndex, InputMaskSize);
            return new InputMask((long)mask, cData.PressedKeys);
        }

        private ICondition GetIf2Condition(byte[] instruction, CompilationData cData)
        {
            // C0TcSX##
            // C0TcS0Ma aaaaaaaa
            // C0TcS1Mr
            // C0TcS2Ra aaaaaaaa
            // C0TcS3Rr
            // C0TcS400 VVVVVVVV(VVVVVVVV)
            // C0TcS5X0
            // T: Width of memory write(1, 2, 4, or 8 bytes).
            // c: Condition to use, see below.
            // S: Source Register.
            // X: Operand Type, see below.
            // M: Memory Type(operand types 0 and 1).
            // R: Address Register(operand types 2 and 3).
            // a: Relative Address(operand types 0 and 2).
            // r: Offset Register(operand types 1 and 3).
            // X: Other Register(operand type 5).
            // V: Value to compare to(operand type 4).

            byte width = instruction[If2WidthIndex];
            byte condition = instruction[If2CondIndex];
            Register srcReg = GetRegister(instruction[If2SrcRegIndex], cData);
            byte opType = instruction[If2OpTypeIndex];
            byte addrRegOrMem = instruction[If2SourceIndex];
            byte offRegOrImm = instruction[If2OffsetIndex];
            ulong offImm;
            ulong valueImm;
            Register addrReg;
            Register offReg;
            IOperand srcOp;

            switch (condition)
            {
                case If2ModeWithImmOff:
                    // *(?x + #a)
                    offImm = GetImmediate(instruction, If2OffsetIndex, If2OffImmSize);
                    srcOp = EmitPointer(addrRegOrMem, offImm, cData);
                    break;
                case If2ModeWithRegOff:
                    // *(?x + $r)
                    offReg = GetRegister(instruction[offRegOrImm], cData);
                    srcOp = EmitPointer(addrRegOrMem, offReg, cData);
                    break;
                case If2AddrRegWithImmOff:
                    // *($R + #a)
                    addrReg = GetRegister(instruction[addrRegOrMem], cData);
                    offImm = GetImmediate(instruction, If2OffsetIndex, If2OffImmSize);
                    srcOp = EmitPointer(addrReg, offImm, cData);
                    break;
                case If2AddrRegWithRegOff:
                    // *($R + $r)
                    addrReg = GetRegister(instruction[addrRegOrMem], cData);
                    offReg = GetRegister(instruction[offRegOrImm], cData);
                    srcOp = EmitPointer(addrReg, offReg, cData);
                    break;
                case If2ImmValue:
                    valueImm = GetImmediate(instruction, If2ValueIndex, width > 4 ? If2ValueSize8 : If2ValueSize16);
                    srcOp = new Value<ulong>(valueImm);
                    break;
                case If2AddrReg:
                    // $V
                    srcOp = GetRegister(instruction[addrRegOrMem], cData);
                    break;
                default:
                    throw new TamperCompilationException($"Invalid operand type {opType} in Atmosphere cheat");
            }

            return GetCondition(condition, width, srcReg, srcOp);
        }

        private ICondition GetCondition(byte condition, byte width, IOperand lhs, IOperand rhs)
        {
            switch (condition)
            {
                case CmpCondGT: return (ICondition)Create(typeof(CondGT<>), width, lhs, rhs);
                case CmpCondGE: return (ICondition)Create(typeof(CondGE<>), width, lhs, rhs);
                case CmpCondLT: return (ICondition)Create(typeof(CondLT<>), width, lhs, rhs);
                case CmpCondLE: return (ICondition)Create(typeof(CondLE<>), width, lhs, rhs);
                case CmpCondEQ: return (ICondition)Create(typeof(CondEQ<>), width, lhs, rhs);
                case CmpCondNE: return (ICondition)Create(typeof(CondNE<>), width, lhs, rhs);
                default:
                    throw new TamperCompilationException($"Invalid condition {condition} in Atmosphere cheat");
            }
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

        private ulong GetAddressShift(byte source, CompilationData cData) // TODO address -> position?
        {
            switch (source)
            {
                case MemOffExe:
                    // Memory address is relative to the code start.
                    return cData.ExeAddress;
                case MemOffHeap:
                    // Memory address is relative to the heap.
                    return cData.HeapAddress;
                default:
                    throw new TamperCompilationException($"Invalid memory source {source} in Atmosphere cheat");
            }
        }

        private Register GetRegister(byte index, CompilationData cData)
        {
            if (cData.Registers.TryGetValue(index, out Register register))
            {
                return register;
            }

            register = new Register();
            cData.Registers.Add(index, register);

            return register;
        }

        private Register GetSavedRegister(byte index, CompilationData cData)
        {
            if (cData.SavedRegisters.TryGetValue(index, out Register register))
            {
                return register;
            }

            register = new Register();
            cData.SavedRegisters.Add(index, register);

            return register;
        }

        private Register GetStaticRegister(byte index, CompilationData cData)
        {
            if (cData.SavedRegisters.TryGetValue(index, out Register register))
            {
                return register;
            }

            register = new Register();
            cData.SavedRegisters.Add(index, register);

            return register;
        }

        private ushort GetExtendedOpcode(byte[] instruction)
        {
            int opcode = instruction[OpCodeIndex];

            if (opcode >= 0xC)
            {
                byte extension = instruction[OpCodeIndex + 1];
                opcode = (opcode << 4) | extension;

                if (extension == 0xF)
                {
                    extension = instruction[OpCodeIndex + 2];
                    opcode = (opcode << 4) | extension;
                }
            }

            return (ushort)opcode;
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
