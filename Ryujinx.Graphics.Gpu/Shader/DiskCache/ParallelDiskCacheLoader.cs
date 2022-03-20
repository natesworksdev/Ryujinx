using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using static Ryujinx.Graphics.Gpu.Shader.ShaderCache;

namespace Ryujinx.Graphics.Gpu.Shader.DiskCache
{
    class ParallelDiskCacheLoader
    {
        private const int ThreadCount = 8;

        private readonly GpuContext _context;
        private readonly ShaderCacheHashTable _graphicsCache;
        private readonly ComputeShaderCacheHashTable _computeCache;
        private readonly DiskCacheHostStorage _hostStorage;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Indicates if the cache should be loaded.
        /// </summary>
        public bool Active => !_cancellationToken.IsCancellationRequested;

        private bool _needsHostRegen;

        /// <summary>
        /// Number of shaders that failed to compile from the cache.
        /// </summary>
        public int ErrorCount { get; private set; }

        /// <summary>
        /// Program validation entry.
        /// </summary>
        private struct ProgramEntry
        {
            /// <summary>
            /// Cached shader program.
            /// </summary>
            public readonly CachedShaderProgram CachedProgram;

            /// <summary>
            /// Host program.
            /// </summary>
            public readonly IProgram HostProgram;

            /// <summary>
            /// Program index.
            /// </summary>
            public readonly int ProgramIndex;

            /// <summary>
            /// Indicates if the program is a compute shader.
            /// </summary>
            public readonly bool IsCompute;

            /// <summary>
            /// Indicates if the program is a host binary shader.
            /// </summary>
            public readonly bool IsBinary;

            /// <summary>
            /// Creates a new program validation entry.
            /// </summary>
            /// <param name="cachedProgram">Cached shader program</param>
            /// <param name="hostProgram">Host program</param>
            /// <param name="programIndex">Program index</param>
            /// <param name="isCompute">Indicates if the program is a compute shader</param>
            /// <param name="isBinary">Indicates if the program is a host binary shader</param>
            public ProgramEntry(
                CachedShaderProgram cachedProgram,
                IProgram hostProgram,
                int programIndex,
                bool isCompute,
                bool isBinary)
            {
                CachedProgram = cachedProgram;
                HostProgram = hostProgram;
                ProgramIndex = programIndex;
                IsCompute = isCompute;
                IsBinary = isBinary;
            }
        }

        /// <summary>
        /// Translated shader compilation entry.
        /// </summary>
        private struct ProgramCompilation
        {
            /// <summary>
            /// Translated shader stages.
            /// </summary>
            public readonly ShaderProgram[] TranslatedStages;

            /// <summary>
            /// Cached shaders.
            /// </summary>
            public readonly CachedShaderStage[] Shaders;

            /// <summary>
            /// Specialization state.
            /// </summary>
            public readonly ShaderSpecializationState SpecializationState;

            /// <summary>
            /// Program index.
            /// </summary>
            public readonly int ProgramIndex;

            /// <summary>
            /// Indicates if the program is a compute shader.
            /// </summary>
            public readonly bool IsCompute;

            /// <summary>
            /// Creates a new translated shader compilation entry.
            /// </summary>
            /// <param name="translatedStages">Translated shader stages</param>
            /// <param name="shaders">Cached shaders</param>
            /// <param name="specState">Specialization state</param>
            /// <param name="programIndex">Program index</param>
            /// <param name="isCompute">Indicates if the program is a compute shader</param>
            public ProgramCompilation(
                ShaderProgram[] translatedStages,
                CachedShaderStage[] shaders,
                ShaderSpecializationState specState,
                int programIndex,
                bool isCompute)
            {
                TranslatedStages = translatedStages;
                Shaders = shaders;
                SpecializationState = specState;
                ProgramIndex = programIndex;
                IsCompute = isCompute;
            }
        }

        /// <summary>
        /// Program translation entry.
        /// </summary>
        private struct AsyncProgramTranslation
        {
            /// <summary>
            /// Cached shader stages.
            /// </summary>
            public readonly CachedShaderStage[] Shaders;

            /// <summary>
            /// Specialization state.
            /// </summary>
            public readonly ShaderSpecializationState SpecializationState;

            /// <summary>
            /// Program index.
            /// </summary>
            public readonly int ProgramIndex;

            /// <summary>
            /// Indicates if the program is a compute shader.
            /// </summary>
            public readonly bool IsCompute;

            /// <summary>
            /// Creates a new program translation entry.
            /// </summary>
            /// <param name="shaders">Cached shader stages</param>
            /// <param name="specState">Specialization state</param>
            /// <param name="programIndex">Program index</param>
            /// <param name="isCompute">Indicates if the program is a compute shader</param>
            public AsyncProgramTranslation(
                CachedShaderStage[] shaders,
                ShaderSpecializationState specState,
                int programIndex,
                bool isCompute)
            {
                Shaders = shaders;
                SpecializationState = specState;
                ProgramIndex = programIndex;
                IsCompute = isCompute;
            }
        }

        private readonly Queue<ProgramEntry> _validationQueue;
        private readonly ConcurrentQueue<ProgramCompilation> _compilationQueue;
        private readonly BlockingCollection<AsyncProgramTranslation> _asyncTranslationQueue;
        private readonly SortedList<int, CachedShaderProgram> _programList;

        private int _compiledCount;
        private int _totalCount;

        /// <summary>
        /// Shader cache state change event.
        /// </summary>
        public event Action<ShaderCacheState, int, int> ShaderCacheStateChanged;

        /// <summary>
        /// Creates a new parallel disk cache loader.
        /// </summary>
        /// <param name="context">GPU context</param>
        /// <param name="graphicsCache">Graphics shader cache</param>
        /// <param name="computeCache">Compute shader cache</param>
        /// <param name="hostStorage">Disk cache host storage</param>
        /// <param name="cancellationToken">Cancellation token</param>
        public ParallelDiskCacheLoader(
            GpuContext context,
            ShaderCacheHashTable graphicsCache,
            ComputeShaderCacheHashTable computeCache,
            DiskCacheHostStorage hostStorage,
            CancellationToken cancellationToken)
        {
            _context = context;
            _graphicsCache = graphicsCache;
            _computeCache = computeCache;
            _hostStorage = hostStorage;
            _cancellationToken = cancellationToken;
            _validationQueue = new Queue<ProgramEntry>();
            _compilationQueue = new ConcurrentQueue<ProgramCompilation>();
            _asyncTranslationQueue = new BlockingCollection<AsyncProgramTranslation>();
            _programList = new SortedList<int, CachedShaderProgram>();
        }

        /// <summary>
        /// Loads all shaders from the cache.
        /// </summary>
        public void LoadShaders()
        {
            Thread[] workThreads = new Thread[ThreadCount];

            for (int index = 0; index < ThreadCount; index++)
            {
                workThreads[index] = new Thread(ProcessAsyncQueue)
                {
                    Name = $"Gpu.AsyncTranslationThread.{index}"
                };
            }

            int programCount = _hostStorage.GetProgramCount();

            _compiledCount = 0;
            _totalCount = programCount;

            ShaderCacheStateChanged?.Invoke(ShaderCacheState.Start, 0, programCount);

            for (int index = 0; index < ThreadCount; index++)
            {
                workThreads[index].Start(_cancellationToken);
            }

            _hostStorage.LoadShaders(_context, this);
            _asyncTranslationQueue.CompleteAdding();

            for (int index = 0; index < ThreadCount; index++)
            {
                workThreads[index].Join();
            }

            CheckCompilationBlocking();

            if (_needsHostRegen)
            {
                // Rebuild both shared and host cache files.
                // Rebuilding shared is required because the shader information returned by the translator
                // might have changed, and so we have to reconstruct the file with the new information.
                _hostStorage.ClearSharedCache();
                _hostStorage.ClearHostCache(_context);

                foreach (var kv in _programList)
                {
                    if (!Active)
                    {
                        break;
                    }

                    CachedShaderProgram program = kv.Value;
                    _hostStorage.AddShader(_context, program, program.HostProgram.GetBinary());
                }
            }

            ShaderCacheStateChanged?.Invoke(ShaderCacheState.Loaded, programCount, programCount);
        }

        /// <summary>
        /// Enqueues a host program for compilation.
        /// </summary>
        /// <param name="cachedProgram">Cached program</param>
        /// <param name="hostProgram">Host program to be compiled</param>
        /// <param name="programIndex">Program index</param>
        /// <param name="isCompute">Indicates if the program is a compute shader</param>
        public void QueueHostProgram(CachedShaderProgram cachedProgram, IProgram hostProgram, int programIndex, bool isCompute)
        {
            _validationQueue.Enqueue(new ProgramEntry(cachedProgram, hostProgram, programIndex, isCompute, isBinary: true));
        }

        /// <summary>
        /// Enqueues a guest program for compilation.
        /// </summary>
        /// <param name="shaders">Cached shader stages</param>
        /// <param name="specState">Specialization state</param>
        /// <param name="programIndex">Program index</param>
        /// <param name="isCompute">Indicates if the program is a compute shader</param>
        public void QueueGuestProgram(CachedShaderStage[] shaders, ShaderSpecializationState specState, int programIndex, bool isCompute)
        {
            _asyncTranslationQueue.Add(new AsyncProgramTranslation(shaders, specState, programIndex, isCompute));
        }

        /// <summary>
        /// Check the state of programs that have already been compiled,
        /// and add to the cache if the compilation was successful.
        /// </summary>
        public void CheckCompilation()
        {
            ProcessCompilationQueue();

            // Process programs that already finished compiling.
            // If not yet compiled, do nothing. This avoids blocking to wait for shader compilation.
            while (_validationQueue.TryPeek(out ProgramEntry entry))
            {
                ProgramLinkStatus result = entry.HostProgram.CheckProgramLink(false);

                if (result != ProgramLinkStatus.Incomplete)
                {
                    ProcessCompiledProgram(ref entry, result);
                    _validationQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Waits until all programs finishes compiling, then adds the ones
        /// with successful compilation to the cache.
        /// </summary>
        private void CheckCompilationBlocking()
        {
            ProcessCompilationQueue();

            while (_validationQueue.TryDequeue(out ProgramEntry entry) && Active)
            {
                ProcessCompiledProgram(ref entry, entry.HostProgram.CheckProgramLink(true), asyncCompile: false);
            }
        }

        /// <summary>
        /// Process a compiled program result.
        /// </summary>
        /// <param name="entry">Compiled program entry</param>
        /// <param name="result">Compilation result</param>
        /// <param name="asyncCompile">For failed host compilations, indicates if a guest compilation should be done asynchronously</param>
        private void ProcessCompiledProgram(ref ProgramEntry entry, ProgramLinkStatus result, bool asyncCompile = true)
        {
            if (result == ProgramLinkStatus.Success)
            {
                // Compilation successful, add to memory cache.
                if (entry.IsCompute)
                {
                    _computeCache.Add(entry.CachedProgram);
                }
                else
                {
                    _graphicsCache.Add(entry.CachedProgram);
                }

                if (!entry.IsBinary)
                {
                    _needsHostRegen = true;
                }

                _programList.Add(entry.ProgramIndex, entry.CachedProgram);
                SignalCompiled();
            }
            else if (entry.IsBinary)
            {
                // If this is a host binary and compilation failed,
                // we still have a chance to recompile from the guest binary.
                CachedShaderProgram program = entry.CachedProgram;

                if (asyncCompile)
                {
                    QueueGuestProgram(program.Shaders, program.SpecializationState, entry.ProgramIndex, entry.IsCompute);
                }
                else
                {
                    RecompileFromGuestCode(program.Shaders, program.SpecializationState, entry.ProgramIndex, entry.IsCompute);
                    ProcessCompilationQueue();
                }
            }
            else
            {
                // Failed to compile from both host and guest binary.
                ErrorCount++;
                SignalCompiled();
            }
        }

        /// <summary>
        /// Processes the queue of translated guest programs that should be compiled on the host.
        /// </summary>
        private void ProcessCompilationQueue()
        {
            while (_compilationQueue.TryDequeue(out ProgramCompilation compilation) && Active)
            {
                IShader[] hostShaders = new IShader[compilation.TranslatedStages.Length];

                int fragmentOutputMap = -1;

                for (int index = 0; index < compilation.TranslatedStages.Length; index++)
                {
                    ShaderProgram shader = compilation.TranslatedStages[index];
                    hostShaders[index] = _context.Renderer.CompileShader(shader.Info.Stage, shader.Code);

                    if (shader.Info.Stage == ShaderStage.Fragment)
                    {
                        fragmentOutputMap = shader.Info.FragmentOutputMap;
                    }
                }

                IProgram hostProgram = _context.Renderer.CreateProgram(hostShaders, new ShaderInfo(fragmentOutputMap));
                CachedShaderProgram program = new CachedShaderProgram(hostProgram, compilation.SpecializationState, compilation.Shaders);

                _validationQueue.Enqueue(new ProgramEntry(program, hostProgram, compilation.ProgramIndex, compilation.IsCompute, isBinary: false));
            }
        }

        /// <summary>
        /// Processses the queue of programs that should be translated from guest code.
        /// </summary>
        /// <param name="state">Cancellation token</param>
        private void ProcessAsyncQueue(object state)
        {
            CancellationToken ct = (CancellationToken)state;

            try
            {
                foreach (AsyncProgramTranslation asyncCompilation in _asyncTranslationQueue.GetConsumingEnumerable(ct))
                {
                    RecompileFromGuestCode(
                        asyncCompilation.Shaders,
                        asyncCompilation.SpecializationState,
                        asyncCompilation.ProgramIndex,
                        asyncCompilation.IsCompute);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }

        /// <summary>
        /// Recompiles a program from guest code.
        /// </summary>
        /// <param name="shaders">Shader stages</param>
        /// <param name="specState">Specialization state</param>
        /// <param name="programIndex">Program index</param>
        /// <param name="isCompute">Indicates if the program is a compute shader</param>
        private void RecompileFromGuestCode(CachedShaderStage[] shaders, ShaderSpecializationState specState, int programIndex, bool isCompute)
        {
            if (isCompute)
            {
                RecompileComputeFromGuestCode(shaders, specState, programIndex);
            }
            else
            {
                RecompileGraphicsFromGuestCode(shaders, specState, programIndex);
            }
        }

        /// <summary>
        /// Recompiles a graphics program from guest code.
        /// </summary>
        /// <param name="shaders">Shader stages</param>
        /// <param name="specState">Specialization state</param>
        /// <param name="programIndex">Program index</param>
        private void RecompileGraphicsFromGuestCode(CachedShaderStage[] shaders, ShaderSpecializationState specState, int programIndex)
        {
            ResourceCounts counts = new ResourceCounts();
            List<ShaderProgram> translatedStages = new List<ShaderProgram>();
            TranslatorContext nextStage = null;

            for (int stageIndex = Constants.ShaderStages - 1; stageIndex >= 0; stageIndex--)
            {
                CachedShaderStage shader = shaders[stageIndex + 1];

                if (shader == null)
                {
                    continue;
                }

                var guestCode = shader.Code;
                var cb1Data = shader.Cb1Data;

                DiskCacheGpuAccessor gpuAccessor = new DiskCacheGpuAccessor(_context, guestCode, cb1Data, specState, counts, stageIndex);
                TranslatorContext currentStage = DecodeGraphicsShader(gpuAccessor, DefaultFlags, 0);

                ShaderProgram program;

                if (stageIndex == 0 && shaders[0] != null)
                {
                    DiskCacheGpuAccessor gpuAccessorA = new DiskCacheGpuAccessor(_context, shaders[0].Code, shaders[0].Cb1Data, specState, counts, 0);
                    TranslatorContext vertexA = DecodeGraphicsShader(gpuAccessorA, DefaultFlags | TranslationFlags.VertexA, 0);

                    program = currentStage.Translate(nextStage, vertexA);

                    shaders[0] = new CachedShaderStage(null, shaders[0].Code, shaders[0].Cb1Data);
                    shaders[1] = new CachedShaderStage(program.Info, guestCode, cb1Data);
                }
                else
                {
                    program = currentStage.Translate(nextStage, null);

                    shaders[stageIndex + 1] = new CachedShaderStage(program.Info, guestCode, cb1Data);
                }

                if (program != null)
                {
                    translatedStages.Add(program);
                }

                nextStage = currentStage;
            }

            _compilationQueue.Enqueue(new ProgramCompilation(translatedStages.ToArray(), shaders, specState, programIndex, isCompute: false));
        }

        /// <summary>
        /// Recompiles a compute program from guest code.
        /// </summary>
        /// <param name="shaders">Shader stages</param>
        /// <param name="specState">Specialization state</param>
        /// <param name="programIndex">Program index</param>
        private void RecompileComputeFromGuestCode(CachedShaderStage[] shaders, ShaderSpecializationState specState, int programIndex)
        {
            CachedShaderStage shader = shaders[0];
            ResourceCounts counts = new ResourceCounts();
            DiskCacheGpuAccessor gpuAccessor = new DiskCacheGpuAccessor(_context, shader.Code, shader.Cb1Data, specState, counts, 0);

            TranslatorContext translatorContext = DecodeComputeShader(gpuAccessor, 0);

            ShaderProgram program = translatorContext.Translate();

            shaders[0] = new CachedShaderStage(program.Info, shader.Code, shader.Cb1Data);

            _compilationQueue.Enqueue(new ProgramCompilation(new[] { program }, shaders, specState, programIndex, isCompute: true));
        }

        /// <summary>
        /// Signals that compilation of a program has been finished successfully,
        /// or that it failed and guest recompilation has also been attempted.
        /// </summary>
        private void SignalCompiled()
        {
            ShaderCacheStateChanged?.Invoke(ShaderCacheState.Loading, ++_compiledCount, _totalCount);
        }
    }
}