using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Engine.Threed;
using Ryujinx.Graphics.Gpu.Memory;
using Ryujinx.Graphics.Gpu.Shader.Cache;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Memory cache of shader code.
    /// </summary>
    class ShaderCache : IDisposable
    {
        public const TranslationFlags DefaultFlags = TranslationFlags.DebugMode;

        private struct TranslatedShader
        {
            public readonly CachedShaderStage Shader;
            public readonly ShaderProgram Program;

            public TranslatedShader(CachedShaderStage shader, ShaderProgram program)
            {
                Shader = shader;
                Program = program;
            }
        }

        private struct TranslatedShaderVertexPair
        {
            public readonly CachedShaderStage VertexA;
            public readonly CachedShaderStage VertexB;
            public readonly ShaderProgram Program;

            public TranslatedShaderVertexPair(CachedShaderStage vertexA, CachedShaderStage vertexB, ShaderProgram program)
            {
                VertexA = vertexA;
                VertexB = vertexB;
                Program = program;
            }
        }

        private readonly GpuContext _context;

        private readonly ShaderDumper _dumper;

        private readonly Dictionary<ulong, CachedShaderProgram> _cpPrograms;
        private readonly Dictionary<ShaderAddresses, CachedShaderProgram> _gpPrograms;

        private struct ProgramToSave
        {
            public readonly CachedShaderProgram CachedProgram;
            public readonly IProgram HostProgram;

            public ProgramToSave(CachedShaderProgram cachedProgram, IProgram hostProgram)
            {
                CachedProgram = cachedProgram;
                HostProgram = hostProgram;
            }
        }

        private Queue<ProgramToSave> _programsToSaveQueue;

        private readonly ComputeShaderCacheHashTable _computeShaderCache;
        private readonly ShaderCacheHashTable _graphicsShaderCache;
        private readonly DiskCacheHostStorage _diskCacheHostStorage;
        private readonly BackgroundDiskCacheWriter _cacheWriter;

        /// <summary>
        /// Event for signalling shader cache loading progress.
        /// </summary>
        public event Action<ShaderCacheState, int, int> ShaderCacheStateChanged;

        /// <summary>
        /// Creates a new instance of the shader cache.
        /// </summary>
        /// <param name="context">GPU context that the shader cache belongs to</param>
        public ShaderCache(GpuContext context)
        {
            _context = context;

            _dumper = new ShaderDumper();

            _cpPrograms = new Dictionary<ulong, CachedShaderProgram>();
            _gpPrograms = new Dictionary<ShaderAddresses, CachedShaderProgram>();

            _programsToSaveQueue = new Queue<ProgramToSave>();

            _computeShaderCache = new ComputeShaderCacheHashTable();
            _graphicsShaderCache = new ShaderCacheHashTable();
            _diskCacheHostStorage = new DiskCacheHostStorage(GraphicsConfig.EnableShaderCache ? CacheHelper.GetBaseCacheDirectory(GraphicsConfig.TitleId) : null);

            if (_diskCacheHostStorage.CacheEnabled)
            {
                _cacheWriter = new BackgroundDiskCacheWriter(context, _diskCacheHostStorage);
            }
        }

        /// <summary>
        /// Processes the queue of shaders that must save their binaries to the disk cache.
        /// </summary>
        public void ProcessShaderCacheQueue()
        {
            // Check to see if the binaries for previously compiled shaders are ready, and save them out.

            while (_programsToSaveQueue.TryPeek(out ProgramToSave programToSave))
            {
                ProgramLinkStatus result = programToSave.HostProgram.CheckProgramLink(false);

                if (result != ProgramLinkStatus.Incomplete)
                {
                    if (result == ProgramLinkStatus.Success)
                    {
                        _cacheWriter.AddShader(programToSave.CachedProgram, programToSave.HostProgram.GetBinary());
                    }

                    _programsToSaveQueue.Dequeue();
                }
                else
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Initialize the cache.
        /// </summary>
        internal void Initialize(CancellationToken cancellationToken)
        {
            if (_diskCacheHostStorage.CacheEnabled)
            {
                ParallelDiskCacheLoader loader = new ParallelDiskCacheLoader(
                    _context,
                    _graphicsShaderCache,
                    _computeShaderCache,
                    _diskCacheHostStorage,
                    cancellationToken);

                loader.ShaderCacheStateChanged += ShaderCacheStateUpdate;
                loader.LoadShaders();
                loader.ShaderCacheStateChanged -= ShaderCacheStateUpdate;

                int errorCount = loader.ErrorCount;
                if (errorCount != 0)
                {
                    Logger.Warning?.Print(LogClass.Gpu, $"Failed to load {errorCount} shaders from the disk cache.");
                }
            }
        }

        /// <summary>
        /// Shader cache state update handler.
        /// </summary>
        /// <param name="state">Current state of the shader cache load process</param>
        /// <param name="current">Number of the current shader being processed</param>
        /// <param name="total">Total number of shaders to process</param>
        private void ShaderCacheStateUpdate(ShaderCacheState state, int current, int total)
        {
            ShaderCacheStateChanged?.Invoke(state, current, total);
        }

        /// <summary>
        /// Gets a compute shader from the cache.
        /// </summary>
        /// <remarks>
        /// This automatically translates, compiles and adds the code to the cache if not present.
        /// </remarks>
        /// <param name="channel">GPU channel</param>
        /// <param name="gcs">GPU channel state</param>
        /// <param name="gpuVa">GPU virtual address of the binary shader code</param>
        /// <param name="localSizeX">Local group size X of the computer shader</param>
        /// <param name="localSizeY">Local group size Y of the computer shader</param>
        /// <param name="localSizeZ">Local group size Z of the computer shader</param>
        /// <param name="localMemorySize">Local memory size of the compute shader</param>
        /// <param name="sharedMemorySize">Shared memory size of the compute shader</param>
        /// <returns>Compiled compute shader code</returns>
        public CachedShaderProgram GetComputeShader(
            GpuChannel channel,
            GpuChannelPoolState poolState,
            GpuChannelComputeState computeState,
            ulong gpuVa)
        {
            if (_cpPrograms.TryGetValue(gpuVa, out var cpShader) && IsShaderEqual(channel, poolState, cpShader, gpuVa))
            {
                return cpShader;
            }

            if (_computeShaderCache.TryFind(channel, poolState, gpuVa, out cpShader, out byte[] cachedGuestCode))
            {
                _cpPrograms[gpuVa] = cpShader;
                return cpShader;
            }

            ShaderSpecializationState specState = new ShaderSpecializationState(computeState);
            GpuAccessorState gpuAccessorState = new GpuAccessorState(poolState, computeState, default, specState);
            GpuAccessor gpuAccessor = new GpuAccessor(_context, channel, gpuAccessorState);

            TranslatorContext translatorContext = DecodeComputeShader(gpuAccessor, gpuVa);

            TranslatedShader translatedShader = TranslateShader(_dumper, channel, translatorContext, null, cachedGuestCode);

            IShader hostShader = _context.Renderer.CompileShader(ShaderStage.Compute, translatedShader.Program.Code);

            IProgram hostProgram = _context.Renderer.CreateProgram(new IShader[] { hostShader }, new ShaderInfo(-1));

            cpShader = new CachedShaderProgram(hostProgram, specState, translatedShader.Shader);

            _computeShaderCache.Add(cpShader);
            EnqueueProgramToSave(new ProgramToSave(cpShader, hostProgram));
            _cpPrograms[gpuVa] = cpShader;

            return cpShader;
        }

        /// <summary>
        /// Gets a graphics shader program from the shader cache.
        /// This includes all the specified shader stages.
        /// </summary>
        /// <remarks>
        /// This automatically translates, compiles and adds the code to the cache if not present.
        /// </remarks>
        /// <param name="state">GPU state</param>
        /// <param name="channel">GPU channel</param>
        /// <param name="graphicsState">GPU channel state</param>
        /// <param name="addresses">Addresses of the shaders for each stage</param>
        /// <returns>Compiled graphics shader code</returns>
        public CachedShaderProgram GetGraphicsShader(
            ref ThreedClassState state,
            GpuChannel channel,
            GpuChannelPoolState poolState,
            GpuChannelGraphicsState graphicsState,
            ShaderAddresses addresses)
        {
            if (_gpPrograms.TryGetValue(addresses, out var gpShaders) && IsShaderEqual(channel, poolState, gpShaders, addresses))
            {
                return gpShaders;
            }

            if (_graphicsShaderCache.TryFind(channel, poolState, addresses, out gpShaders, out var cachedGuestCode))
            {
                _gpPrograms[addresses] = gpShaders;
                return gpShaders;
            }

            TransformFeedbackDescriptor[] transformFeedbackDescriptors = GetTransformFeedbackDescriptors(ref state);

            ShaderSpecializationState specState = new ShaderSpecializationState(graphicsState, transformFeedbackDescriptors);
            GpuAccessorState gpuAccessorState = new GpuAccessorState(poolState, default, graphicsState, specState, transformFeedbackDescriptors);

            ReadOnlySpan<ulong> addressesSpan = addresses.AsSpan();

            CachedShaderStage[] shaders = new CachedShaderStage[Constants.ShaderStages + 1];
            List<IShader> hostShaders = new List<IShader>();
            TranslatorContext nextStage = null;

            for (int stageIndex = Constants.ShaderStages - 1; stageIndex >= 0; stageIndex--)
            {
                ulong gpuVa = addressesSpan[stageIndex + 1];
                if (gpuVa == 0)
                {
                    continue;
                }

                GpuAccessor gpuAccessor = new GpuAccessor(_context, channel, gpuAccessorState, stageIndex);
                TranslatorContext currentStage = DecodeGraphicsShader(gpuAccessor, DefaultFlags, gpuVa);

                ShaderProgram program;

                if (stageIndex == 0 && addresses.VertexA != 0)
                {
                    TranslatedShaderVertexPair translatedShader = TranslateShader(
                        _dumper,
                        channel,
                        currentStage,
                        nextStage,
                        DecodeGraphicsShader(gpuAccessor, DefaultFlags | TranslationFlags.VertexA, addresses.VertexA),
                        cachedGuestCode.VertexACode,
                        cachedGuestCode.VertexBCode);

                    shaders[0] = translatedShader.VertexA;
                    shaders[1] = translatedShader.VertexB;
                    program = translatedShader.Program;
                }
                else
                {
                    TranslatedShader translatedShader = TranslateShader(
                        _dumper,
                        channel,
                        currentStage,
                        nextStage,
                        cachedGuestCode.GetByIndex(stageIndex));

                    shaders[stageIndex + 1] = translatedShader.Shader;
                    program = translatedShader.Program;
                }

                if (program != null)
                {
                    hostShaders.Add(_context.Renderer.CompileShader(program.Info.Stage, program.Code));
                }

                nextStage = currentStage;
            }

            int fragmentOutputMap = shaders[5]?.Info.FragmentOutputMap ?? -1;
            IProgram hostProgram = _context.Renderer.CreateProgram(hostShaders.ToArray(), new ShaderInfo(fragmentOutputMap));

            gpShaders = new CachedShaderProgram(hostProgram, specState, shaders);

            _graphicsShaderCache.Add(gpShaders);
            EnqueueProgramToSave(new ProgramToSave(gpShaders, hostProgram));
            _gpPrograms[addresses] = gpShaders;

            return gpShaders;
        }

        private void EnqueueProgramToSave(ProgramToSave programToSave)
        {
            if (_diskCacheHostStorage.CacheEnabled)
            {
                _programsToSaveQueue.Enqueue(programToSave);
            }
        }

        /// <summary>
        /// Gets transform feedback state from the current GPU state.
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <returns>Four transform feedback descriptors for the enabled TFBs, or null if TFB is disabled</returns>
        private static TransformFeedbackDescriptor[] GetTransformFeedbackDescriptors(ref ThreedClassState state)
        {
            bool tfEnable = state.TfEnable;
            if (!tfEnable)
            {
                return null;
            }

            TransformFeedbackDescriptor[] descs = new TransformFeedbackDescriptor[Constants.TotalTransformFeedbackBuffers];

            for (int i = 0; i < Constants.TotalTransformFeedbackBuffers; i++)
            {
                var tf = state.TfState[i];

                descs[i] = new TransformFeedbackDescriptor(tf.BufferIndex, tf.Stride, ref state.TfVaryingLocations[i]);
            }

            return descs;
        }

        /// <summary>
        /// Checks if compute shader code in memory is equal to the cached shader.
        /// </summary>
        /// <param name="channel">GPU channel using the shader</param>
        /// <param name="poolState">GPU channel state to verify shader compatibility</param>
        /// <param name="cpShader">Cached compute shader</param>
        /// <param name="gpuVa">GPU virtual address of the shader code in memory</param>
        /// <returns>True if the code is different, false otherwise</returns>
        private static bool IsShaderEqual(
            GpuChannel channel,
            GpuChannelPoolState poolState,
            CachedShaderProgram cpShader,
            ulong gpuVa)
        {
            if (IsShaderEqual(channel.MemoryManager, cpShader.Shaders[0], gpuVa))
            {
                return cpShader.SpecializationState.MatchesCompute(channel, poolState);
            }

            return false;
        }

        /// <summary>
        /// Checks if graphics shader code from all stages in memory are equal to the cached shaders.
        /// </summary>
        /// <param name="channel">GPU channel using the shader</param>
        /// <param name="poolState">GPU channel state to verify shader compatibility</param>
        /// <param name="gpShaders">Cached graphics shaders</param>
        /// <param name="addresses">GPU virtual addresses of all enabled shader stages</param>
        /// <returns>True if the code is different, false otherwise</returns>
        private static bool IsShaderEqual(
            GpuChannel channel,
            GpuChannelPoolState poolState,
            CachedShaderProgram gpShaders,
            ShaderAddresses addresses)
        {
            ReadOnlySpan<ulong> addressesSpan = addresses.AsSpan();

            for (int stageIndex = 0; stageIndex < gpShaders.Shaders.Length; stageIndex++)
            {
                CachedShaderStage shader = gpShaders.Shaders[stageIndex];

                ulong gpuVa = addressesSpan[stageIndex];

                if (!IsShaderEqual(channel.MemoryManager, shader, gpuVa))
                {
                    return false;
                }
            }

            return gpShaders.SpecializationState.MatchesGraphics(channel, poolState);
        }

        /// <summary>
        /// Checks if the code of the specified cached shader is different from the code in memory.
        /// </summary>
        /// <param name="memoryManager">Memory manager used to access the GPU memory where the shader is located</param>
        /// <param name="shader">Cached shader to compare with</param>
        /// <param name="gpuVa">GPU virtual address of the binary shader code</param>
        /// <returns>True if the code is different, false otherwise</returns>
        private static bool IsShaderEqual(MemoryManager memoryManager, CachedShaderStage shader, ulong gpuVa)
        {
            if (shader == null)
            {
                return true;
            }

            ReadOnlySpan<byte> memoryCode = memoryManager.GetSpan(gpuVa, shader.Code.Length);

            return memoryCode.SequenceEqual(shader.Code);
        }

        /// <summary>
        /// Decode the binary Maxwell shader code to a translator context.
        /// </summary>
        /// <param name="channel">GPU channel</param>
        /// <param name="gas">GPU accessor state</param>
        /// <param name="gpuVa">GPU virtual address of the binary shader code</param>
        /// <param name="localSizeX">Local group size X of the computer shader</param>
        /// <param name="localSizeY">Local group size Y of the computer shader</param>
        /// <param name="localSizeZ">Local group size Z of the computer shader</param>
        /// <param name="localMemorySize">Local memory size of the compute shader</param>
        /// <param name="sharedMemorySize">Shared memory size of the compute shader</param>
        /// <returns>The generated translator context</returns>
        public static TranslatorContext DecodeComputeShader(IGpuAccessor gpuAccessor, ulong gpuVa)
        {
            var options = new TranslationOptions(TargetLanguage.Glsl, TargetApi.OpenGL, DefaultFlags | TranslationFlags.Compute);
            return Translator.CreateContext(gpuVa, gpuAccessor, options);
        }

        /// <summary>
        /// Decode the binary Maxwell shader code to a translator context.
        /// </summary>
        /// <remarks>
        /// This will combine the "Vertex A" and "Vertex B" shader stages, if specified, into one shader.
        /// </remarks>
        /// <param name="channel">GPU channel</param>
        /// <param name="gas">GPU accessor state</param>
        /// <param name="flags">Flags that controls shader translation</param>
        /// <param name="stage">Shader stage</param>
        /// <param name="gpuVa">GPU virtual address of the shader code</param>
        /// <returns>The generated translator context</returns>
        public static TranslatorContext DecodeGraphicsShader(IGpuAccessor gpuAccessor, TranslationFlags flags, ulong gpuVa)
        {
            var options = new TranslationOptions(TargetLanguage.Glsl, TargetApi.OpenGL, flags);
            return Translator.CreateContext(gpuVa, gpuAccessor, options);
        }

        /// <summary>
        /// Translates a previously generated translator context to something that the host API accepts.
        /// </summary>
        /// <param name="dumper">Optional shader code dumper</param>
        /// <param name="memoryManager">Memory manager used to access the GPU memory where the shader is located</param>
        /// <param name="currentStage">Translator context of the stage to be translated</param>
        /// <param name="nextStage">Translator context of the next active stage, if existent</param>
        /// <param name="vertexA">Optional translator context of the shader that should be combined</param>
        /// <returns>Compiled graphics shader code</returns>
        private static TranslatedShaderVertexPair TranslateShader(
            ShaderDumper dumper,
            GpuChannel channel,
            TranslatorContext currentStage,
            TranslatorContext nextStage,
            TranslatorContext vertexA,
            byte[] codeA,
            byte[] codeB)
        {
            ulong cb1DataAddress = channel.BufferManager.GetGraphicsUniformBufferAddress(0, 1);

            var memoryManager = channel.MemoryManager;

            codeA ??= memoryManager.GetSpan(vertexA.Address, vertexA.Size).ToArray();
            codeB ??= memoryManager.GetSpan(currentStage.Address, currentStage.Size).ToArray();
            byte[] cb1DataA = memoryManager.Physical.GetSpan(cb1DataAddress, vertexA.Cb1DataSize).ToArray();
            byte[] cb1DataB = memoryManager.Physical.GetSpan(cb1DataAddress, currentStage.Cb1DataSize).ToArray();

            ShaderDumpPaths pathsA = default;
            ShaderDumpPaths pathsB = default;

            if (dumper != null)
            {
                pathsA = dumper.Dump(codeA, compute: false);
                pathsB = dumper.Dump(codeB, compute: false);
            }

            ShaderProgram program = currentStage.Translate(nextStage, vertexA);

            pathsB.Prepend(program);
            pathsA.Prepend(program);

            CachedShaderStage vertexAStage = new CachedShaderStage(null, codeA, cb1DataA);
            CachedShaderStage vertexBStage = new CachedShaderStage(program.Info, codeB, cb1DataB);

            return new TranslatedShaderVertexPair(vertexAStage, vertexBStage, program);
        }

        /// <summary>
        /// Translates a previously generated translator context to something that the host API accepts.
        /// </summary>
        /// <param name="dumper">Optional shader code dumper</param>
        /// <param name="memoryManager">Memory manager used to access the GPU memory where the shader is located</param>
        /// <param name="currentStage">Translator context of the stage to be translated</param>
        /// <param name="nextStage">Translator context of the next active stage, if existent</param>
        /// <param name="vertexA">Optional translator context of the shader that should be combined</param>
        /// <returns>Compiled graphics shader code</returns>
        private static TranslatedShader TranslateShader(
            ShaderDumper dumper,
            GpuChannel channel,
            TranslatorContext currentStage,
            TranslatorContext nextStage,
            byte[] code)
        {
            if (currentStage == null)
            {
                return new TranslatedShader(null, null);
            }

            var memoryManager = channel.MemoryManager;

            ulong cb1DataAddress = currentStage.Stage == ShaderStage.Compute
                ? channel.BufferManager.GetComputeUniformBufferAddress(1)
                : channel.BufferManager.GetGraphicsUniformBufferAddress(StageToStageIndex(currentStage.Stage), 1);

            byte[] cb1Data = memoryManager.Physical.GetSpan(cb1DataAddress, currentStage.Cb1DataSize).ToArray();
            code ??= memoryManager.GetSpan(currentStage.Address, currentStage.Size).ToArray();

            ShaderDumpPaths paths = dumper?.Dump(code, currentStage.Stage == ShaderStage.Compute) ?? default;
            ShaderProgram program = currentStage.Translate(nextStage);

            paths.Prepend(program);

            return new TranslatedShader(new CachedShaderStage(program.Info, code, cb1Data), program);
        }

        private static int StageToStageIndex(ShaderStage stage)
        {
            return stage switch
            {
                ShaderStage.TessellationControl => 1,
                ShaderStage.TessellationEvaluation => 2,
                ShaderStage.Geometry => 3,
                ShaderStage.Fragment => 4,
                _ => 0
            };
        }

        private static ShaderStage StageIndexToStage(int stageIndex)
        {
            return stageIndex switch
            {
                1 => ShaderStage.TessellationControl,
                2 => ShaderStage.TessellationEvaluation,
                3 => ShaderStage.Geometry,
                4 => ShaderStage.Fragment,
                _ => ShaderStage.Vertex
            };
        }

        /// <summary>
        /// Disposes the shader cache, deleting all the cached shaders.
        /// It's an error to use the shader cache after disposal.
        /// </summary>
        public void Dispose()
        {
            foreach (CachedShaderProgram program in _cpPrograms.Values)
            {
                program.Dispose();
            }

            foreach (CachedShaderProgram program in _gpPrograms.Values)
            {
                program.Dispose();
            }

            _cacheWriter?.Dispose();
        }
    }
}
