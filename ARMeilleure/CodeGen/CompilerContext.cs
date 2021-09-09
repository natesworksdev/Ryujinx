using ARMeilleure.IntermediateRepresentation;
using System;

namespace ARMeilleure.CodeGen
{
    /// <summary>
    /// Represents a compiler context for a function.
    /// </summary>
    class CompilerContext : IDisposable
    {
        [ThreadStatic]
        private static CompilerAllocators _allocators;

        /// <summary>
        /// Gets the <see cref="CompilerAllocators"/> associated the current thread.
        /// </summary>
        public static CompilerAllocators Allocators
        {
            get
            {
                if (_allocators == null)
                {
                    _allocators = new CompilerAllocators();
                }

                return _allocators;
            }
        }

        private bool _disposed;

        /// <summary>
        /// Gets the <see cref="ControlFlowGraph"/> of the function to compile if its set; otherwise
        /// <see langword="null"/>.
        /// </summary>
        public ControlFlowGraph Cfg { get; internal set; }

        /// <summary>
        /// Gets the argument types of the function to compile.
        /// </summary>
        public OperandType[] ArgumentTypes { get; }

        /// <summary>
        /// Gets the return type of the function to compile.
        /// </summary>
        public OperandType ReturnType { get; }

        /// <summary>
        /// Gets the compiler option of the function to compile.
        /// </summary>
        public CompilerOptions Options { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompilerContext"/> class with the spcified argument types,
        /// return type and compiler options.
        /// </summary>
        /// <param name="argTypes">Argument types to use</param>
        /// <param name="retType">Return type to use</param>
        /// <param name="options">Compiler options to use</param>
        public CompilerContext(
            OperandType[] argTypes,
            OperandType retType,
            CompilerOptions options)
        {
            ArgumentTypes = argTypes;
            ReturnType = retType;
            Options = options;

            Allocators.IncrementReferenceCount();
        }

        /// <summary>
        /// Releases all resources used by the <see cref="CompilerContext"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all unmanaged and optionally managed resources used by the <see cref="CompilerContext"/> instance.
        /// </summary>
        /// <param name="disposing"><see langword="true"/> to dispose managed resources also; otherwise just unmanaged resouces</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                Allocators.DecrementReferenceCount();

                _disposed = true;
            }
        }

        /// <summary>
        /// Frees resources used by the <see cref="CompilerContext"/> instance.
        /// </summary>
        ~CompilerContext()
        {
            Dispose(false);
        }
    }
}