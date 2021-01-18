using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Tamper.Operations;
using Ryujinx.Memory;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Ryujinx.HLE.HOS.Tamper
{
    public class AtmosphereCompiler
    {
        const byte OpCodeLoad      = 5;
        const byte OpCodeStoreRegA = 6;
        const byte OpCodeArithmetic = 7;

        const int OpCodeIndex  = 0;

        const int LdWidthIndex = 1;
        const int LdMemIndex   = 2;
        const int LdRegIndex   = 3;
        const int LdAddrIndex  = 6;

        const int LdMemExe  = 0;
        const int LdMemHeap = 1;

        const int LdAddrSize  = 10;

        const int StWidthIndex   = 1;
        const int StAddrRegIndex = 3;
        const int StDoIncIndex   = 4;
        const int StDoOffIndex   = 5;
        const int StOffRegIndex  = 6;
        const int StValueIndex   = 8;

        const int StValueSize  = 16;

        const int ArWidthIndex   = 1;
        const int ArDstRegIndex  = 3;
        const int ArOpTypeIndex  = 4;
        const int ArValueIndex   = 8;

        const int ArValueSize = 8;

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
        // const byte OpCodeStoreImmA = 0;
        // const byte OpCodeArithmetic2 = 9;
        // const byte OpCodeBeginIf = 1;
        // const byte OpCodeEndIf = 2;
        // const byte OpCodeLoop = 3;
        // const byte OpCodeSet = 4;
        // ...

        public TamperProgram Compile(IEnumerable<string> rawInstructions, ulong exeAddress, ulong heapAddress)
        {
            try
            {
                return CompileImpl(rawInstructions, exeAddress, heapAddress);
            }
            catch(Exception)
            {
                Logger.Error?.Print(LogClass.TamperMachine, $"The Atmosphere cheat compiler crashed while compiling the cheat");

                return null;
            }
        }

        private TamperProgram CompileImpl(IEnumerable<string> rawInstructions, ulong exeAddress, ulong heapAddress)
        {
            CompilationData data = new CompilationData(exeAddress, heapAddress);

            // Parse the instructions.

            foreach (string rawInstruction in rawInstructions)
            {
                if (!TryParseRawInstruction(rawInstruction, out byte[] instruction))
                {
                    return null;
                }

                byte opcode = instruction[OpCodeIndex];

                switch (opcode)
                {
                    case OpCodeLoad:       if (!EmitLoad      (instruction, ref data)) return null; break;
                    case OpCodeStoreRegA:  if (!EmitStore     (instruction, ref data)) return null; break;
                    case OpCodeArithmetic: if (!EmitArithmetic(instruction, ref data)) return null; break;
                    default:
                        Logger.Error?.Print(LogClass.TamperMachine, $"Opcode {opcode} not implemented in Atmosphere cheat");
                        return null;
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

        private bool EmitLoad(byte[] instruction, ref CompilationData data)
        {
            if (instruction.Length != 16)
            {
                Logger.Error?.Print(LogClass.TamperMachine, $"Invalid instruction length {instruction.Length} in Atmosphere cheat");

                return false;
            }

            byte width = instruction[LdWidthIndex];
            byte source = instruction[LdMemIndex];
            Register dstReg = GetRegister(instruction[LdRegIndex], ref data);
            ulong address = GetImmediate(instruction, LdAddrIndex, LdAddrSize);

            switch (source)
            {
                case LdMemExe:
                    // Load is relative to the code start address.
                    address += data.ExeAddress;
                    break;
                case LdMemHeap:
                    // Load is relative to the heap address.
                    address += data.HeapAddress;
                    break;
                default:
                    Logger.Error?.Print(LogClass.TamperMachine, $"Invalid memory source {source} in Atmosphere cheat");
                    return false;
            }

            Value<ulong> loadAddr = new Value<ulong>(address);
            Pointer srcMem = new Pointer(loadAddr, data.Memory);

            return Emit(typeof(OpMov<>), width, ref data, dstReg, srcMem);
        }

        private bool EmitStore(byte[] instruction, ref CompilationData data)
        {
            // TODO: Support type 0.

            if (instruction.Length != 24)
            {
                Logger.Error?.Print(LogClass.TamperMachine, $"Invalid instruction length {instruction.Length} in Atmosphere cheat");

                return false;
            }

            byte width = instruction[StWidthIndex];
            IOperand srcReg = GetRegister(instruction[StAddrRegIndex], ref data);
            IOperand storeAddr = srcReg;
            byte doIncrement = instruction[StDoIncIndex];
            byte doOffset = instruction[StDoOffIndex];
            ulong value = GetImmediate(instruction, StValueIndex, StValueSize); // TODO: Optimize to 'width'?
            Value<ulong> storeValue = new Value<ulong>(value);

            switch (doOffset)
            {
                case 0:
                    // Don't offset the address register by another register.
                    break;
                case 1:
                    // Replace the source address by the sum of the base and offset registers.
                    storeAddr = new Value<ulong>(0);
                    IOperand offsetReg = GetRegister(instruction[StOffRegIndex], ref data);
                    data.CurrentBlock.Add(new OpAdd<ulong>(storeAddr, srcReg, offsetReg));
                    break;
                default:
                    Logger.Error?.Print(LogClass.TamperMachine, $"Invalid increment mode {doIncrement} in Atmosphere cheat");
                    return false;
            }

            Pointer dstMem = new Pointer(storeAddr, data.Memory);

            if (!Emit(typeof(OpMov<>), width, ref data, dstMem, storeValue))
            {
                return false;
            }

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
                    Logger.Error?.Print(LogClass.TamperMachine, $"Invalid increment mode {doIncrement} in Atmosphere cheat");
                    return false;
            }

            return true;
        }

        private bool EmitArithmetic(byte[] instruction, ref CompilationData data)
        {
            // TODO: Implement arithmetic type 9.

            if (instruction.Length != 16)
            {
                Logger.Error?.Print(LogClass.TamperMachine, $"Invalid instruction length {instruction.Length} in Atmosphere cheat");

                return false;
            }

            byte width = instruction[ArWidthIndex];
            Register register = GetRegister(instruction[ArDstRegIndex], ref data);
            byte operation = instruction[ArOpTypeIndex];
            ulong value = GetImmediate(instruction, ArValueIndex, ArValueSize); // TODO: Optimize to 'width'?
            Value<ulong> opValue = new Value<ulong>(value);

            switch (operation)
            {
                case ArOpAdd: if (!Emit(typeof(OpAdd<>), width, ref data, register, register, opValue)) return false; break;
                case ArOpSub: if (!Emit(typeof(OpSub<>), width, ref data, register, register, opValue)) return false; break;
                case ArOpMul: if (!Emit(typeof(OpMul<>), width, ref data, register, register, opValue)) return false; break;
                case ArOpLsh: if (!Emit(typeof(OpLsh<>), width, ref data, register, register, opValue)) return false; break;
                case ArOpRsh: if (!Emit(typeof(OpRsh<>), width, ref data, register, register, opValue)) return false; break;
                default:
                    Logger.Error?.Print(LogClass.TamperMachine, $"Invalid arithmetic operation {operation} in Atmosphere cheat");
                    return false;
            }

            return true;
        }

        private bool Emit(Type instruction, byte width, ref CompilationData data, params IOperand[] operands)
        {
            Type realType;

            switch (width)
            {
                case 1: realType = instruction.MakeGenericType(typeof(byte  )); break;
                case 2: realType = instruction.MakeGenericType(typeof(ushort)); break;
                case 4: realType = instruction.MakeGenericType(typeof(uint  )); break;
                case 8: realType = instruction.MakeGenericType(typeof(ulong )); break;
                default:
                    Logger.Error?.Print(LogClass.TamperMachine, $"Invalid instruction width {width} in Atmosphere cheat");
                    return false;
            }

            data.CurrentBlock.Add((IOperation)Activator.CreateInstance(realType, operands));

            return true;
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

        private bool TryParseRawInstruction(string rawInstruction, out byte[] instruction)
        {
            const int wordSize = 2 * sizeof(uint);

            // Instructions are multi-word, with 32bit words. Split the raw instruction
            // and parse each word into individual quartets of bits.

            var words = rawInstruction.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);

            instruction = new byte[wordSize * words.Length];

            for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
            {
                string word = words[wordIndex];

                if (word.Length != wordSize)
                {
                    Logger.Error?.Print(LogClass.TamperMachine, $"Invalid word length for {word} in Atmosphere cheat");

                    return false;
                }

                for (int quartetIndex = 0; quartetIndex < wordSize; quartetIndex++)
                {
                    int index = wordIndex * wordSize + quartetIndex;
                    string byteData = word.Substring(quartetIndex, 1);

                    if (!byte.TryParse(byteData, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out instruction[index]))
                    {
                        Logger.Error?.Print(LogClass.TamperMachine, $"Failed to parse word {word} in Atmosphere cheat");

                        return false;
                    }
                }

            }

            return true;
        }
    }
}
