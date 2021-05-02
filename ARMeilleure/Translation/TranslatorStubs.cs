using ARMeilleure.Instructions;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.State;
using ARMeilleure.Translation.Cache;
using System;
using System.Runtime.InteropServices;
using static ARMeilleure.IntermediateRepresentation.OperandHelper;

namespace ARMeilleure.Translation
{
    /// <summary>
    /// Represents a stub manager.
    /// </summary>
    class TranslatorStubs : IDisposable
    {
        private bool _disposed;

        private readonly Lazy<IntPtr> _dispatchStub;
        private static readonly Lazy<IntPtr> _slowDispatchStub = new(GenerateSlowDispatchStub, isThreadSafe: true);

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
        /// Initializes a new instance of the <see cref="TranslatorStubs"/> class with the specified
        /// <see cref="Translator"/> instance.
        /// </summary>
        /// <param name="translator"><see cref="Translator"/> instance to use</param>
        /// <exception cref="ArgumentNullException"><paramref name="translator"/> is null</exception>
        public TranslatorStubs(Translator translator)
        {
            if (translator == null)
            {
                throw new ArgumentNullException(nameof(translator));
            }

            _dispatchStub = new Lazy<IntPtr>(() => GenerateDispatchStub(translator), isThreadSafe: true);
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
        /// Generates a <see cref="DispatchStub"/> for the specified <see cref="Translator"/> instance.
        /// </summary>
        /// <param name="translator"><see cref="Translator"/> instance to use</param>
        /// <returns>Generated <see cref="DispatchStub"/></returns>
        private static IntPtr GenerateDispatchStub(Translator translator)
        {
            var context = new EmitterContext();

            Operand lblFallback = Label();
            Operand lblEnd = Label();

            // Load the target guest address from the native context.
            Operand nativeContext = context.LoadArgument(OperandType.I64, 0);
            Operand guestAddress = context.Load(OperandType.I64,
                context.Add(nativeContext, Const((ulong)NativeContext.GetDispatchAddressOffset())));

            // Check if guest address is within range of the AddressTable.
            Operand masked = context.BitwiseAnd(guestAddress, Const(~translator.FunctionTable.Mask));
            context.BranchIfTrue(lblFallback, masked);

            Operand index = null;
            Operand page = Const((long)translator.FunctionTable.Base);

            for (int i = 0; i < translator.FunctionTable.Levels.Length; i++)
            {
                ref var level = ref translator.FunctionTable.Levels[i];

                // level.Mask is not used directly because it is more often bigger than 32-bits, so it will not
                // be encoded as an immediate on x86's bitwise and operation.
                Operand mask = Const(level.Mask >> level.Index);

                index = context.BitwiseAnd(context.ShiftRightUI(guestAddress, Const(level.Index)), mask);

                if (i < translator.FunctionTable.Levels.Length - 1)
                {
                    page = context.Load(OperandType.I64, context.Add(page, context.ShiftLeft(index, Const(3))));

                    context.BranchIfFalse(lblFallback, page);
                }
            }

            Operand hostAddress;

            Operand offsetAddr = context.Add(page, context.ShiftLeft(index, Const(2)));
            Operand offset = context.Load(OperandType.I32, offsetAddr);

            hostAddress = context.Add(Const((long)JitCache.Base), context.ZeroExtend32(OperandType.I64, offset));
            context.Tailcall(hostAddress, nativeContext);

            context.MarkLabel(lblFallback);
            hostAddress = context.Call(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetFunctionAddress)), guestAddress);
            context.Tailcall(hostAddress, nativeContext);

            var cfg = context.GetControlFlowGraph();
            var retType = OperandType.I64;
            var argTypes = new[] { OperandType.I64 };

            var func = Compiler.Compile<GuestFunction>(cfg, argTypes, retType, CompilerOptions.HighCq);

            return Marshal.GetFunctionPointerForDelegate(func);
        }

        /// <summary>
        /// Generates a <see cref="SlowDispatchStub"/>.
        /// </summary>
        /// <returns>Generated <see cref="SlowDispatchStub"/></returns>
        private static IntPtr GenerateSlowDispatchStub()
        {
            var context = new EmitterContext();

            // Load the target guest address from the native context.
            Operand nativeContext = context.LoadArgument(OperandType.I64, 0);
            Operand guestAddress = context.Load(OperandType.I64,
                context.Add(nativeContext, Const((ulong)NativeContext.GetDispatchAddressOffset())));

            Operand hostAddress = context.Call(typeof(NativeInterface).GetMethod(nameof(NativeInterface.GetFunctionAddress)), guestAddress);
            context.Tailcall(hostAddress, nativeContext);

            var cfg = context.GetControlFlowGraph();
            var retType = OperandType.I64;
            var argTypes = new[] { OperandType.I64 };

            var func = Compiler.Compile<GuestFunction>(cfg, argTypes, retType, CompilerOptions.HighCq);

            return Marshal.GetFunctionPointerForDelegate(func);
        }
    }
}
