using ARMeilleure.CodeGen.X86;
using ARMeilleure.Diagnostics;
using ARMeilleure.IntermediateRepresentation;
using ARMeilleure.Translation;
using System;
using System.Runtime.CompilerServices;

namespace ARMeilleure.CodeGen
{
    /// <summary>
    /// Represents a compiler context for a function.
    /// </summary>
    class Compiler : IDisposable
    {
        [ThreadStatic]
        private static CompilerAllocators _allocators;

        /// <summary>
        /// Allocate and initialize the arena allocators. This is the unlikely path to <see cref="Allocators"/>.
        /// </summary>
        /// <returns>Initialized allocators</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CompilerAllocators InitializeAllocators() => _allocators = new CompilerAllocators();

        /// <summary>
        /// Gets the <see cref="CompilerAllocators"/> associated the current thread.
        /// </summary>
        public static CompilerAllocators Allocators
        {
            get
            {
                if (_allocators != null)
                {
                    return _allocators;
                }

                return InitializeAllocators();
            }
        }

        private bool _disposed;

        /// <summary>
        /// Gets the <see cref="ControlFlowGraph"/> of the function to compile if its set; otherwise
        /// <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// This is set using <see cref="EmitterContext.GetControlFlowGraph"/>.
        /// </remarks>
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
        /// Initializes a new instance of the <see cref="Compiler"/> class with the specified argument types,
        /// return type and compiler options.
        /// </summary>
        /// <param name="argTypes">Argument types to use</param>
        /// <param name="retType">Return type to use</param>
        /// <param name="options">Compiler options to use</param>
        public Compiler(OperandType[] argTypes, OperandType retType, CompilerOptions options)
        {
            ArgumentTypes = argTypes ?? Array.Empty<OperandType>();
            ReturnType = retType;
            Options = options;

            Allocators.IncrementReferenceCount();
        }

        /// <summary>
        /// Compiles the function and returns the <see cref="CompiledFunction"/>.
        /// </summary>
        /// <returns>The <see cref="CompiledFunction"/></returns>
        /// <exception cref="InvalidOperationException"><see cref="Cfg"/> not set</exception>
        public CompiledFunction Compile()
        {
            if (Cfg == null)
            {
                throw new InvalidOperationException("ControlFlowGraph not set.");
            }

            if (Options.HasFlag(CompilerOptions.SsaForm))
            {
                Logger.StartPass(PassName.Dominance);

                Dominance.FindDominators(Cfg);
                Dominance.FindDominanceFrontiers(Cfg);

                Logger.EndPass(PassName.Dominance);

                Logger.StartPass(PassName.SsaConstruction);

                Ssa.Construct(Cfg);

                Logger.EndPass(PassName.SsaConstruction, Cfg);
            }
            else
            {
                Logger.StartPass(PassName.RegisterToLocal);

                RegisterToLocal.Rename(Cfg);

                Logger.EndPass(PassName.RegisterToLocal, Cfg);
            }

            return CodeGenerator.Generate(this);
        }

        /// <summary>
        /// Releases all resources used by the <see cref="Compiler"/> instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases all unmanaged and optionally managed resources used by the <see cref="Compiler"/> instance.
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
        /// Frees resources used by the <see cref="Compiler"/> instance.
        /// </summary>
        ~Compiler()
        {
            Dispose(false);
        }
    }
}