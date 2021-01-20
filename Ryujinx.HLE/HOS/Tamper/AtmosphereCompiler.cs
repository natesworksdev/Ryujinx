using Ryujinx.Common.Logging;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.HOS.Tamper.Operations;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ryujinx.HLE.HOS.Tamper
{
    public class AtmosphereCompiler
    {
        const byte OpCodeStoreImmA   = 0;
        const byte OpCodeSet         = 4;
        const byte OpCodeLoad        = 5;
        const byte OpCodeStoreRegA   = 6;
        const byte OpCodeArithmetic1 = 7;
        const byte OpCodeArithmetic2 = 9;

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

        struct CompilationData
        {
            public List<IOperation> CurrentBlock;
            public Parameter<IVirtualMemoryManager> Memory;
            public Dictionary<byte, Register> Registers;
            public ulong ExeAddress;
            public ulong HeapAddress;

            public CompilationData(ulong exeAddress, ulong heapAddress)
            {
                CurrentBlock = new List<IOperation>();
                Memory = new Parameter<IVirtualMemoryManager>(null);
                Registers = new Dictionary<byte, Register>();
                ExeAddress = exeAddress;
                HeapAddress = heapAddress;
            }
        }

        // TODO: The rest of the opcodes
        // const byte OpCodeBeginIf = 1;
        // const byte OpCodeEndIf = 2;
        // const byte OpCodeLoop = 3;
        // ...

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

            // Parse the instructions.

            foreach (string rawInstruction in rawInstructions)
            {
                Logger.Debug?.Print(LogClass.TamperMachine, $"Compiling instruction {rawInstruction}");

                byte[] instruction = ParseRawInstruction(rawInstruction);
                byte opcode = instruction[OpCodeIndex];

                switch (opcode)
                {
                    case OpCodeStoreImmA:   EmitStoreImmA  (instruction, ref data); break;
                    case OpCodeSet:         EmitSet        (instruction, ref data); break;
                    case OpCodeLoad:        EmitLoad       (instruction, ref data); break;
                    case OpCodeStoreRegA:   EmitStoreRegA  (instruction, ref data); break;
                    case OpCodeArithmetic1: EmitArithmetic1(instruction, ref data); break;
                    case OpCodeArithmetic2: EmitArithmetic2(instruction, ref data); break;
                    default:
                        throw new TamperCompilationException($"Opcode {opcode} not implemented in Atmosphere cheat");
                }
            }

            // Initialize only the registers used.

            Value<ulong> zero = new Value<ulong>(0UL);
            int position = 0;

            foreach (Register register in data.Registers.Values)
            {
                data.CurrentBlock.Insert(position, new OpMov<ulong>(register, zero));
                position++;
            }

            return new TamperProgram(data.Memory, new Block(data.CurrentBlock));
        }

        private void EmitSet(byte[] instruction, ref CompilationData data)
        {
            Register srcReg = GetRegister(instruction[SetRegIndex], ref data);
            ulong value = GetImmediate(instruction, SetValueIndex, SetValueSize);
            Value<ulong> dstValue = new Value<ulong>(value);

            data.CurrentBlock.Add(new OpMov<ulong>(srcReg, dstValue));
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
            data.CurrentBlock.Add(new OpAdd<ulong>(storeAddr, offReg, offImmValue));

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
                    data.CurrentBlock.Add(new OpAdd<ulong>(storeAddr, srcReg, offsetReg));
                    break;
                default:
                    throw new TamperCompilationException($"Invalid offset mode {doIncrement} in Atmosphere cheat");
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
                    data.CurrentBlock.Add(new OpAdd<ulong>(srcReg, srcReg, increment));
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

        private void Emit(Type instruction, byte width, ref CompilationData data, params IOperand[] operands)
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

            data.CurrentBlock.Add((IOperation)Activator.CreateInstance(realType, operands));
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
