using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using static Ryujinx.Graphics.Gpu.Shader.ShaderCache;

namespace Ryujinx.Graphics.Gpu.Shader
{
    class ParallelDiskCacheLoader
    {
        private const int ThreadCount = 8;

        private readonly GpuContext _context;
        private readonly ShaderCacheHashTable _graphicsCache;
        private readonly ComputeShaderCacheHashTable _computeCache;
        private readonly DiskCacheHostStorage _hostStorage;
        private readonly CancellationToken _cancellationToken;

        public bool Active => !_cancellationToken.IsCancellationRequested;

        private bool _needsHostRegen;

        public int ErrorCount { get; private set; }

        private struct ProgramEntry
        {
            public readonly CachedShaderProgram CachedProgram;
            public readonly IProgram HostProgram;
            public readonly int ProgramIndex;
            public readonly bool IsCompute;
            public readonly bool IsBinary;

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

        private struct ProgramCompilation
        {
            public readonly ShaderProgram[] TranslatedStages;
            public readonly CachedShaderStage[] Shaders;
            public readonly ShaderSpecializationState SpecializationState;
            public readonly int ProgramIndex;
            public readonly bool IsCompute;

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

        private struct AsyncProgramTranslation
        {
            public readonly CachedShaderStage[] Shaders;
            public readonly ShaderSpecializationState SpecializationState;
            public readonly int ProgramIndex;
            public readonly bool IsCompute;

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
        private readonly SortedList<int, IProgram> _programList;

        private int _compiledCount;
        private int _totalCount;

        public event Action<ShaderCacheState, int, int> ShaderCacheStateChanged;

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
            _programList = new SortedList<int, IProgram>();
        }

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

            _hostStorage.LoadShaders(_context, _graphicsCache, _computeCache, this);
            _asyncTranslationQueue.CompleteAdding();

            for (int index = 0; index < ThreadCount; index++)
            {
                workThreads[index].Join();
            }

            CheckCompilationBlocking();

            if (_needsHostRegen)
            {
                _hostStorage.ClearHostCache(_context);

                foreach (var kv in _programList)
                {
                    if (!Active)
                    {
                        break;
                    }

                    int programIndex = kv.Key;
                    IProgram hostProgram = kv.Value;

                    if (hostProgram != null)
                    {
                        _hostStorage.AddHostShader(_context, hostProgram.GetBinary(), programIndex);
                    }
                    else
                    {
                        _hostStorage.AddHostShader(_context, ReadOnlySpan<byte>.Empty, programIndex);
                    }
                }
            }

            ShaderCacheStateChanged?.Invoke(ShaderCacheState.Loaded, programCount, programCount);
        }

        public void QueueHostProgram(CachedShaderProgram cachedProgram, IProgram hostProgram, int programIndex, bool isCompute)
        {
            _validationQueue.Enqueue(new ProgramEntry(cachedProgram, hostProgram, programIndex, isCompute, isBinary: true));
        }

        public void QueueGuestProgram(CachedShaderStage[] shaders, ShaderSpecializationState specState, int programIndex, bool isCompute)
        {
            _asyncTranslationQueue.Add(new AsyncProgramTranslation(shaders, specState, programIndex, isCompute));
        }

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

        private void CheckCompilationBlocking()
        {
            ProcessCompilationQueue();

            while (_validationQueue.TryDequeue(out ProgramEntry entry) && Active)
            {
                ProcessCompiledProgram(ref entry, entry.HostProgram.CheckProgramLink(true), asyncCompile: false);
            }
        }

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

                _programList.Add(entry.ProgramIndex, entry.HostProgram);
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
                _programList.Add(entry.ProgramIndex, null);
                SignalCompiled();
            }
        }

        private void ProcessCompilationQueue()
        {
            while (_compilationQueue.TryDequeue(out ProgramCompilation compilation) && Active)
            {
                IShader[] hostShaders = new IShader[compilation.TranslatedStages.Length];

                for (int index = 0; index < compilation.TranslatedStages.Length; index++)
                {
                    ShaderProgram shader = compilation.TranslatedStages[index];
                    hostShaders[index] = _context.Renderer.CompileShader(shader.Info.Stage, shader.Code);
                }

                int fragmentOutputMap = compilation.TranslatedStages[5]?.Info.FragmentOutputMap ?? -1;
                IProgram hostProgram = _context.Renderer.CreateProgram(hostShaders, new ShaderInfo(fragmentOutputMap));
                CachedShaderProgram program = new CachedShaderProgram(hostProgram, compilation.SpecializationState, compilation.Shaders);

                _validationQueue.Enqueue(new ProgramEntry(program, hostProgram, compilation.ProgramIndex, compilation.IsCompute, isBinary: false));
            }
        }

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

        private void SignalCompiled()
        {
            ShaderCacheStateChanged?.Invoke(ShaderCacheState.Loading, ++_compiledCount, _totalCount);
        }
    }
}