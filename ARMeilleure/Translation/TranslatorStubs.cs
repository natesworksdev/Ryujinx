using ARMeilleure.CodeGen;
using ARMeilleure.Instructions;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation.Cache;
using System;
using System.Reflection;
using System.Runtime.InteropServices;

using static ARMeilleure.IntermediateRepresentation.Operand.Factory;

namespace ARMeilleure.Translation
{
    /// <summary>
    /// Represents a stub manager.
    /// </summary>
    class TranslatorStubs : IDisposable
    {
        private static readonly Lazy<IntPtr> _slowDispatchStub = new(GenerateSlowDispatchStub, isThreadSafe: true);

        private bool _disposed;

        private readonly Translator _translator;
        private readonly Lazy<IntPtr> _dispatchStub;
        private readonly Lazy<DispatcherFunction> _dispatchLoop;

        /// <summary>
        /// Gets the dispatch stub.
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="TranslatorStubs"/> instance was disposed</exception>
        public IntPtr DispatchStub
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(null);
                }

                return _dispatchStub.Value;
            }
        }

        /// <summary>
        /// Gets the slow dispatch stub.
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="TranslatorStubs"/> instance was disposed</exception>
        public IntPtr SlowDispatchStub
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(null);
                }

                return _slowDispatchStub.Value;
            }
        }

        /// <summary>
        /// Gets the dispatch loop function.
        /// </summary>
        /// <exception cref="ObjectDisposedException"><see cref="TranslatorStubs"/> instance was disposed</exception>
        public DispatcherFunction DispatchLoop
        {
            get
            {
                if (_disposed)
                {
                    throw new ObjectDisposedException(null);
                }

                return _dispatchLoop.Value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslatorStubs"/> class with the specified
        /// <see cref="Translator"/> instance.
        /// </summary>
        /// <param name="translator"><see cref="Translator"/> instance to use</param>
        /// <exception cref="ArgumentNullException"><paramref name="translator"/> is null</exception>
        public TranslatorStubs(Translator translator)
        {
            _translator = translator ?? throw new ArgumentNullException(nameof(translator));
            _dispatchStub = new(GenerateDispatchStub, isThreadSafe: true);
            _dispatchLoop = new(GenerateDispatchLoop, isThreadSafe: true);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="TranslatorStubs"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all unmanaged and optionally managed resources used by the <see cref="TranslatorStubs"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to dispose managed resources also; otherwise just unmanaged resouces</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (_dispatchStub.IsValueCreated)
                {
                    JitCache.Unmap(_dispatchStub.Value);
                }

                if (_dispatchLoop.IsValueCreated)
                {
                    JitCache.Unmap(Marshal.GetFunctionPointerForDelegate(_dispatchLoop.Value));
                }

                _disposed = true;
            }
        }

        /// <summary>
        /// Frees resources used by the <see cref="TranslatorStubs"/> instance.
        /// </summary>
        ~TranslatorStubs()
        {
            Dispose(false);
        }

        /// <summary>
        /// Generates a <see cref="DispatchStub"/>.
        /// </summary>
        /// <returns>Generated <see cref="DispatchStub"/></returns>
        private IntPtr GenerateDispatchStub()
        {
            var retType = OperandType.I64;
            var argTypes = new[] { OperandType.I64 };

            using var context = new CompilerContext(argTypes, retType, CompilerOptions.HighCq);
            var emitter = new EmitterContext(context);

            Operand lblFallback = Label();
            Operand lblEnd = Label();

            // Load the target guest address from the native context.
            Operand nativeContext = emitter.LoadArgument(OperandType.I64, 0);
            Operand guestAddress = emitter.Load(OperandType.I64,
                emitter.Add(nativeContext, Const((ulong)NativeContext.GetDispatchAddressOffset())));

            // Check if guest address is within range of the AddressTable.
            Operand masked = emitter.BitwiseAnd(guestAddress, Const(~_translator.FunctionTable.Mask));
            emitter.BranchIfTrue(lblFallback, masked);

            Operand index = default;
            Operand page = Const((long)_translator.FunctionTable.Base);

            for (int i = 0; i < _translator.FunctionTable.Levels.Length; i++)
            {
                ref var level = ref _translator.FunctionTable.Levels[i];

                // level.Mask is not used directly because it is more often bigger than 32-bits, so it will not
                // be encoded as an immediate on x86's bitwise and operation.
                Operand mask = Const(level.Mask >> level.Index);

                index = emitter.BitwiseAnd(emitter.ShiftRightUI(guestAddress, Const(level.Index)), mask);

                if (i < _translator.FunctionTable.Levels.Length - 1)
                {
                    page = emitter.Load(OperandType.I64, emitter.Add(page, emitter.ShiftLeft(index, Const(3))));
                    emitter.BranchIfFalse(lblFallback, page);
                }
            }

            Operand hostAddress;
            Operand hostAddressAddr = emitter.Add(page, emitter.ShiftLeft(index, Const(3)));
            hostAddress = emitter.Load(OperandType.I64, hostAddressAddr);
            emitter.Tailcall(hostAddress, nativeContext);

            emitter.MarkLabel(lblFallback);
            hostAddress = emitter.Call(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetFunctionAddress)), guestAddress);
            emitter.Tailcall(hostAddress, nativeContext);

            emitter.GetControlFlowGraph();

            var func = Compiler.Compile(context).Map<GuestFunction>();

            return Marshal.GetFunctionPointerForDelegate(func);
        }

        /// <summary>
        /// Generates a <see cref="SlowDispatchStub"/>.
        /// </summary>
        /// <returns>Generated <see cref="SlowDispatchStub"/></returns>
        private static IntPtr GenerateSlowDispatchStub()
        {
            var retType = OperandType.I64;
            var argTypes = new[] { OperandType.I64 };

            using var context = new CompilerContext(argTypes, retType, CompilerOptions.HighCq);
            var emitter = new EmitterContext(context);

            // Load the target guest address from the native context.
            Operand nativeContext = emitter.LoadArgument(OperandType.I64, 0);
            Operand guestAddress = emitter.Load(OperandType.I64,
                emitter.Add(nativeContext, Const((ulong)NativeContext.GetDispatchAddressOffset())));

            MethodInfo getFuncAddress = typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetFunctionAddress));
            Operand hostAddress = emitter.Call(getFuncAddress, guestAddress);
            emitter.Tailcall(hostAddress, nativeContext);

            emitter.GetControlFlowGraph();

            var func = Compiler.Compile(context).Map<GuestFunction>();

            return Marshal.GetFunctionPointerForDelegate(func);
        }

        /// <summary>
        /// Generates a <see cref="DispatchLoop"/> function.
        /// </summary>
        /// <returns><see cref="DispatchLoop"/> function</returns>
        private DispatcherFunction GenerateDispatchLoop()
        {
            var retType = OperandType.None;
            var argTypes = new[] { OperandType.I64, OperandType.I64 };

            using var context = new CompilerContext(argTypes, retType, CompilerOptions.HighCq);
            var emitter = new EmitterContext(context);

            Operand beginLbl = Label();
            Operand endLbl = Label();

            Operand nativeContext = emitter.LoadArgument(OperandType.I64, 0);
            Operand guestAddress = emitter.Copy(
                emitter.AllocateLocal(OperandType.I64),
                emitter.LoadArgument(OperandType.I64, 1));

            Operand runningAddress = emitter.Add(nativeContext, Const((ulong)NativeContext.GetRunningOffset()));
            Operand dispatchAddress = emitter.Add(nativeContext, Const((ulong)NativeContext.GetDispatchAddressOffset()));

            emitter.MarkLabel(beginLbl);
            emitter.Store(dispatchAddress, guestAddress);
            emitter.Copy(guestAddress, emitter.Call(Const((ulong)DispatchStub), OperandType.I64, nativeContext));
            emitter.BranchIfFalse(endLbl, guestAddress);
            emitter.BranchIfFalse(endLbl, emitter.Load(OperandType.I32, runningAddress));
            emitter.Branch(beginLbl);

            emitter.MarkLabel(endLbl);
            emitter.Return();

            emitter.GetControlFlowGraph();

            return Compiler.Compile(context).Map<DispatcherFunction>();
        }
    }
}
