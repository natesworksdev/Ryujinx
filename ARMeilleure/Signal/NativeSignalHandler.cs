using ARMeilleure.Common;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using ARMeilleure.Translation.Cache;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Signal
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SignalHandlerRange
    {
        public int IsActive;
        public nuint RangeAddress;
        public nuint RangeEndAddress;
        public IntPtr ActionPointer;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct SignalHandlerConfig
    {
        /// <summary>
        /// The byte offset of the faulting address in the SigInfo or ExceptionRecord struct.
        /// </summary>
        public int StructAddressOffset;

        /// <summary>
        /// The byte offset of the write flag in the SigInfo or ExceptionRecord struct.
        /// </summary>
        public int StructWriteOffset;

        /// <summary>
        /// The sigaction handler that was registered before this one. (unix only)
        /// </summary>
        public nuint UnixOldSigaction;

        /// <summary>
        /// The type of the previous sigaction. True for the 3 argument variant. (unix only)
        /// </summary>
        public int UnixOldSigaction3Arg;

        public SignalHandlerRange Range0;
        public SignalHandlerRange Range1;
        public SignalHandlerRange Range2;
        public SignalHandlerRange Range3;
        public SignalHandlerRange Range4;
        public SignalHandlerRange Range5;
        public SignalHandlerRange Range6;
        public SignalHandlerRange Range7;
    }

    public unsafe class NativeSignalHandler : IDisposable
    {
        private delegate void UnixExceptionHandler(int sig, IntPtr info, IntPtr ucontext);
        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int VectoredExceptionHandler(IntPtr exceptionInfo);

        private const int MaxTrackedRanges = 8;

        private const int StructAddressOffset = 0;
        private const int StructWriteOffset = 4;
        private const int UnixOldSigaction = 8;
        private const int UnixOldSigaction3Arg = 16;
        private const int RangeOffset = 20;

        private const int EXCEPTION_CONTINUE_SEARCH = 0;
        private const int EXCEPTION_CONTINUE_EXECUTION = -1;

        private const uint EXCEPTION_ACCESS_VIOLATION = 0xc0000005;

        private const ulong PageSize = 0x1000;
        private const ulong PageMask = PageSize - 1;

        private static readonly SignalHandlerConfig* _handlerConfig;

        private bool _disposed = false;
        private readonly IntPtr _signalHandlerPtr;
        private readonly IntPtr _signalHandlerHandle;
        private readonly JitCache _jitCache;

        static NativeSignalHandler()
        {
            _handlerConfig = NativeAllocator.Instance.Allocate<SignalHandlerConfig>();
            *_handlerConfig = default;
        }

        internal NativeSignalHandler(JitCache jitCache)
        {
            _jitCache = jitCache;

            bool unix = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

            if (unix)
            {
                // Unix siginfo struct locations.
                // NOTE: These are incredibly likely to be different between kernel version and architectures.

                _handlerConfig->StructAddressOffset = 16; // si_addr
                _handlerConfig->StructWriteOffset = 8; // si_code

                _signalHandlerPtr = Marshal.GetFunctionPointerForDelegate(GenerateUnixSignalHandler(_handlerConfig));

                SigAction old = UnixSignalHandlerRegistration.RegisterExceptionHandler(_signalHandlerPtr);
                _handlerConfig->UnixOldSigaction = (nuint)(ulong)old.sa_handler;
                _handlerConfig->UnixOldSigaction3Arg = old.sa_flags & 4;
            }
            else
            {
                _handlerConfig->StructAddressOffset = 40; // ExceptionInformation1
                _handlerConfig->StructWriteOffset = 32; // ExceptionInformation0

                _signalHandlerPtr = Marshal.GetFunctionPointerForDelegate(GenerateWindowsSignalHandler(_handlerConfig));
                _signalHandlerHandle = WindowsSignalHandlerRegistration.RegisterExceptionHandler(_signalHandlerPtr);
            }
        }

        private static ref SignalHandlerConfig GetConfigRef()
        {
            return ref Unsafe.AsRef<SignalHandlerConfig>(_handlerConfig);
        }

        public static bool AddTrackedRegion(nuint address, nuint endAddress, IntPtr action)
        {
            var ranges = &_handlerConfig->Range0;

            for (int i = 0; i < MaxTrackedRanges; i++)
            {
                if (ranges[i].IsActive == 0)
                {
                    ranges[i].RangeAddress = address;
                    ranges[i].RangeEndAddress = endAddress;
                    ranges[i].ActionPointer = action;
                    ranges[i].IsActive = 1;

                    return true;
                }
            }

            return false;
        }

        public static bool RemoveTrackedRegion(nuint address)
        {
            var ranges = &_handlerConfig->Range0;

            for (int i = 0; i < MaxTrackedRanges; i++)
            {
                if (ranges[i].IsActive == 1 && ranges[i].RangeAddress == address)
                {
                    ranges[i].IsActive = 0;

                    return true;
                }
            }

            return false;
        }

        private static Operand EmitGenericRegionCheck(EmitterContext context, SignalHandlerConfig* handlerConfig, Operand faultAddress, Operand isWrite)
        {
            Operand inRegionLocal = context.AllocateLocal(OperandType.I32);
            context.Copy(inRegionLocal, Const(0));

            Operand endLabel = Label();

            for (int i = 0; i < MaxTrackedRanges; i++)
            {
                ulong rangeBaseOffset = (ulong)(RangeOffset + i * Unsafe.SizeOf<SignalHandlerRange>());

                Operand nextLabel = Label();

                Operand isActive = context.Load(OperandType.I32, Const((ulong)handlerConfig + rangeBaseOffset));

                context.BranchIfFalse(nextLabel, isActive);

                Operand rangeAddress = context.Load(OperandType.I64, Const((ulong)handlerConfig + rangeBaseOffset + 4));
                Operand rangeEndAddress = context.Load(OperandType.I64, Const((ulong)handlerConfig + rangeBaseOffset + 12));

                // Is the fault address within this tracked region?
                Operand inRange = context.BitwiseAnd(
                    context.ICompare(faultAddress, rangeAddress, Comparison.GreaterOrEqualUI),
                    context.ICompare(faultAddress, rangeEndAddress, Comparison.Less));

                // Only call tracking if in range.
                context.BranchIfFalse(nextLabel, inRange, BasicBlockFrequency.Cold);

                context.Copy(inRegionLocal, Const(1));
                Operand offset = context.BitwiseAnd(context.Subtract(faultAddress, rangeAddress), Const(~PageMask));

                // Call the tracking action, with the pointer's relative offset to the base address.
                Operand trackingActionPtr = context.Load(OperandType.I64, Const((ulong)handlerConfig + rangeBaseOffset + 20));
                context.Call(trackingActionPtr, OperandType.I32, offset, Const(PageSize), isWrite, Const(0));

                context.Branch(endLabel);

                context.MarkLabel(nextLabel);
            }

            context.MarkLabel(endLabel);

            return context.Copy(inRegionLocal);
        }

        private UnixExceptionHandler GenerateUnixSignalHandler(SignalHandlerConfig* handlerConfig)
        {
            EmitterContext context = new EmitterContext();

            // (int sig, SigInfo* sigInfo, void* ucontext)
            Operand sigInfoPtr = context.LoadArgument(OperandType.I64, 1);

            Operand structAddressOffset = context.Load(OperandType.I64, Const((ulong)handlerConfig + StructAddressOffset));
            Operand structWriteOffset = context.Load(OperandType.I64, Const((ulong)handlerConfig + StructWriteOffset));

            Operand faultAddress = context.Load(OperandType.I64, context.Add(sigInfoPtr, context.ZeroExtend32(OperandType.I64, structAddressOffset)));
            Operand writeFlag = context.Load(OperandType.I64, context.Add(sigInfoPtr, context.ZeroExtend32(OperandType.I64, structWriteOffset)));

            Operand isWrite = context.ICompareNotEqual(writeFlag, Const(0L)); // Normalize to 0/1.

            Operand isInRegion = EmitGenericRegionCheck(context, handlerConfig, faultAddress, isWrite);

            Operand endLabel = Label();

            context.BranchIfTrue(endLabel, isInRegion);

            Operand unixOldSigaction = context.Load(OperandType.I64, Const((ulong)handlerConfig + UnixOldSigaction));
            Operand unixOldSigaction3Arg = context.Load(OperandType.I64, Const((ulong)handlerConfig + UnixOldSigaction3Arg));
            Operand threeArgLabel = Label();

            context.BranchIfTrue(threeArgLabel, unixOldSigaction3Arg);

            context.Call(unixOldSigaction, OperandType.None, context.LoadArgument(OperandType.I32, 0));
            context.Branch(endLabel);

            context.MarkLabel(threeArgLabel);

            context.Call(unixOldSigaction,
                OperandType.None,
                context.LoadArgument(OperandType.I32, 0),
                sigInfoPtr,
                context.LoadArgument(OperandType.I64, 2));

            context.MarkLabel(endLabel);

            context.Return();

            ControlFlowGraph cfg = context.GetControlFlowGraph();

            var argTypes = new OperandType[] { OperandType.I32, OperandType.I64, OperandType.I64 };
            var retType = OperandType.None;

            return Compiler.Compile(cfg, argTypes, retType, CompilerOptions.HighCq).Map<UnixExceptionHandler>(_jitCache);
        }

        private VectoredExceptionHandler GenerateWindowsSignalHandler(SignalHandlerConfig* handlerConfig)
        {
            EmitterContext context = new EmitterContext();

            // (ExceptionPointers* exceptionInfo)
            Operand exceptionInfoPtr = context.LoadArgument(OperandType.I64, 0);
            Operand exceptionRecordPtr = context.Load(OperandType.I64, exceptionInfoPtr);

            // First thing's first - this catches a number of exceptions, but we only want access violations.
            Operand validExceptionLabel = Label();

            Operand exceptionCode = context.Load(OperandType.I32, exceptionRecordPtr);

            context.BranchIf(validExceptionLabel, exceptionCode, Const(EXCEPTION_ACCESS_VIOLATION), Comparison.Equal);

            context.Return(Const(EXCEPTION_CONTINUE_SEARCH)); // Don't handle this one.

            context.MarkLabel(validExceptionLabel);

            // Next, read the address of the invalid access, and whether it is a write or not.

            Operand structAddressOffset = context.Load(OperandType.I32, Const((ulong)handlerConfig + StructAddressOffset));
            Operand structWriteOffset = context.Load(OperandType.I32, Const((ulong)handlerConfig + StructWriteOffset));

            Operand faultAddress = context.Load(OperandType.I64, context.Add(exceptionRecordPtr, context.ZeroExtend32(OperandType.I64, structAddressOffset)));
            Operand writeFlag = context.Load(OperandType.I64, context.Add(exceptionRecordPtr, context.ZeroExtend32(OperandType.I64, structWriteOffset)));

            Operand isWrite = context.ICompareNotEqual(writeFlag, Const(0L)); // Normalize to 0/1.

            Operand isInRegion = EmitGenericRegionCheck(context, handlerConfig, faultAddress, isWrite);

            Operand endLabel = Label();

            // If the region check result is false, then run the next vectored exception handler.

            context.BranchIfTrue(endLabel, isInRegion);

            context.Return(Const(EXCEPTION_CONTINUE_SEARCH));

            context.MarkLabel(endLabel);

            // Otherwise, return to execution.

            context.Return(Const(EXCEPTION_CONTINUE_EXECUTION));

            // Compile and return the function.

            ControlFlowGraph cfg = context.GetControlFlowGraph();

            var argTypes = new OperandType[] { OperandType.I64 };
            var retType = OperandType.I32;

            return Compiler.Compile(cfg, argTypes, retType, CompilerOptions.HighCq).Map<VectoredExceptionHandler>(_jitCache);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            bool unix = RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            bool result = unix
                ? UnixSignalHandlerRegistration.RestoreExceptionHandler((IntPtr)(ulong)_handlerConfig->UnixOldSigaction, _handlerConfig->UnixOldSigaction3Arg)
                : WindowsSignalHandlerRegistration.RemoveExceptionHandler(_signalHandlerHandle);

            if (!result)
            {
                throw new InvalidOperationException("Failure uninstalling native signal handler.");
            }

            _jitCache.Unmap(_signalHandlerPtr);

            _disposed = true;
        }

        ~NativeSignalHandler()
        {
            Dispose(false);
        }
    }
}
