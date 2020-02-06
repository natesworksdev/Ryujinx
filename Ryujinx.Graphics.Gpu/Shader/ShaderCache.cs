using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Image;
using Ryujinx.Graphics.Gpu.State;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Shader
{
    using TextureDescriptor = Image.TextureDescriptor;

    /// <summary>
    /// Memory cache of shader code.
    /// </summary>
    class ShaderCache : IDisposable
    {
        private const int ShaderHeaderSize = 0x50;

        private const TranslationFlags DefaultFlags = TranslationFlags.DebugMode;

        private readonly GpuContext _context;

        private readonly ShaderMap<ComputeShader> _cpPrograms;
        private readonly ShaderMap<GraphicsShader> _gpPrograms;

        private readonly ShaderDumper _dumper;

        /// <summary>
        /// Creates a new instance of the shader cache.
        /// </summary>
        /// <param name="context">GPU context that the shader cache belongs to</param>
        public ShaderCache(GpuContext context)
        {
            _context = context;

            _cpPrograms = new ShaderMap<ComputeShader>();
            _gpPrograms = new ShaderMap<GraphicsShader>();

            _dumper = new ShaderDumper();            
        }

        /// <summary>
        /// Gets a compute shader from the cache.
        /// </summary>
        /// <remarks>
        /// This automatically translates, compiles and adds the code to the cache if not present.
        /// </remarks>
        /// <param name="gpuVa">GPU virtual address of the binary shader code</param>
        /// <param name="localSizeX">Local group size X of the computer shader</param>
        /// <param name="localSizeY">Local group size Y of the computer shader</param>
        /// <param name="localSizeZ">Local group size Z of the computer shader</param>
        /// <param name="localMemorySize">Local memory size of the compute shader</param>
        /// <param name="sharedMemorySize">Shared memory size of the compute shader</param>
        /// <returns>Compiled compute shader code</returns>
        public ComputeShader GetComputeShader(
            ulong gpuVa,
            int localSizeX,
            int localSizeY,
            int localSizeZ,
            int localMemorySize,
            int sharedMemorySize)
        {
            if (gpuVa == 0)
            {
                return null;
            }

            ShaderPack pack = new ShaderPack();

            ReadOnlySpan<byte> code = GetShaderCodeHeaderless(gpuVa);

            pack.Add(code);

            ComputeShader cs = _cpPrograms.Get(pack, out int hash);

            if (cs != null)
            {
                return cs;
            }

            int QueryInfo(QueryInfoName info, int index)
            {
                return info switch
                {
                    QueryInfoName.ComputeLocalSizeX => localSizeX,
                    QueryInfoName.ComputeLocalSizeY => localSizeY,
                    QueryInfoName.ComputeLocalSizeZ => localSizeZ,
                    QueryInfoName.ComputeLocalMemorySize => localMemorySize,
                    QueryInfoName.ComputeSharedMemorySize => sharedMemorySize,
                    _ => QueryInfoCommon(info)
                };
            }

            TranslatorCallbacks callbacks = new TranslatorCallbacks(QueryInfo, PrintLog);

            ShaderProgram program;

            _dumper.Dump(code, compute: true, out string fullPath, out string codePath);

            program = Translator.Translate(code, callbacks, DefaultFlags | TranslationFlags.Compute);

            if (fullPath != null && codePath != null)
            {
                program.Prepend("// " + codePath);
                program.Prepend("// " + fullPath);
            }

            IShader shader = _context.Renderer.CompileShader(program);

            cs = new ComputeShader(_context.Renderer.CreateProgram(new IShader[] { shader }), program);

            _cpPrograms.Add(hash, cs, pack);

            return cs;
        }

        /// <summary>
        /// Gets a graphics shader program from the shader cache.
        /// This includes all the specified shader stages.
        /// </summary>
        /// <remarks>
        /// This automatically translates, compiles and adds the code to the cache if not present.
        /// </remarks>
        /// <param name="state">Current GPU state</param>
        /// <param name="addresses">Addresses of the shaders for each stage</param>
        /// <returns>Compiled graphics shader code</returns>
        public GraphicsShader GetGraphicsShader(GpuState state, ShaderAddresses addresses)
        {
            ShaderPack pack = new ShaderPack();

            if (addresses.Vertex != 0)
            {
                pack.Add(GetShaderCode(addresses.Vertex));
            }

            if (addresses.TessControl != 0)
            {
                pack.Add(GetShaderCode(addresses.TessControl));
            }

            if (addresses.TessEvaluation != 0)
            {
                pack.Add(GetShaderCode(addresses.TessEvaluation));
            }

            if (addresses.Geometry != 0)
            {
                pack.Add(GetShaderCode(addresses.Geometry));
            }

            if (addresses.Fragment != 0)
            {
                pack.Add(GetShaderCode(addresses.Fragment));
            }

            GraphicsShader gs = _gpPrograms.Get(pack, out int hash);

            if (gs != null)
            {
                return gs;
            }

            gs = new GraphicsShader();

            if (addresses.VertexA != 0)
            {
                gs.Shaders[0] = TranslateGraphicsShader(state, ShaderStage.Vertex, addresses.Vertex, addresses.VertexA);
            }
            else
            {
                gs.Shaders[0] = TranslateGraphicsShader(state, ShaderStage.Vertex, addresses.Vertex);
            }

            gs.Shaders[1] = TranslateGraphicsShader(state, ShaderStage.TessellationControl,    addresses.TessControl);
            gs.Shaders[2] = TranslateGraphicsShader(state, ShaderStage.TessellationEvaluation, addresses.TessEvaluation);
            gs.Shaders[3] = TranslateGraphicsShader(state, ShaderStage.Geometry,               addresses.Geometry);
            gs.Shaders[4] = TranslateGraphicsShader(state, ShaderStage.Fragment,               addresses.Fragment);

            BackpropQualifiers(gs);

            List<IShader> shaders = new List<IShader>();

            for (int stage = 0; stage < gs.Shaders.Length; stage++)
            {
                ShaderProgram program = gs.Shaders[stage];

                if (program == null)
                {
                    continue;
                }

                shaders.Add(_context.Renderer.CompileShader(program));
            }

            gs.HostProgram = _context.Renderer.CreateProgram(shaders.ToArray());

            _gpPrograms.Add(hash, gs, pack);

            return gs;
        }

        /// <summary>
        /// Translates the binary Maxwell shader code to something that the host API accepts.
        /// </summary>
        /// <remarks>
        /// This will combine the "Vertex A" and "Vertex B" shader stages, if specified, into one shader.
        /// </remarks>
        /// <param name="state">Current GPU state</param>
        /// <param name="stage">Shader stage</param>
        /// <param name="gpuVa">GPU virtual address of the shader code</param>
        /// <param name="gpuVaA">Optional GPU virtual address of the "Vertex A" shader code</param>
        /// <returns>Compiled graphics shader code</returns>
        private ShaderProgram TranslateGraphicsShader(GpuState state, ShaderStage stage, ulong gpuVa, ulong gpuVaA = 0)
        {
            if (gpuVa == 0)
            {
                return null;
            }

            int QueryInfo(QueryInfoName info, int index)
            {
                return info switch
                {
                    QueryInfoName.IsTextureBuffer => Convert.ToInt32(QueryIsTextureBuffer(state, (int)stage - 1, index)),
                    QueryInfoName.IsTextureRectangle => Convert.ToInt32(QueryIsTextureRectangle(state, (int)stage - 1, index)),
                    QueryInfoName.PrimitiveTopology => (int)GetPrimitiveTopology(),
                    _ => QueryInfoCommon(info)
                };
            }

            TranslatorCallbacks callbacks = new TranslatorCallbacks(QueryInfo, PrintLog);

            ShaderProgram program;

            if (gpuVaA != 0)
            {
                // TODO.
                program = null;
            }
            else
            {
                ReadOnlySpan<byte> code = GetShaderCode(gpuVa);

                program = Translator.Translate(code, callbacks, DefaultFlags);

                _dumper.Dump(code, compute: false, out string fullPath, out string codePath);

                if (fullPath != null && codePath != null)
                {
                    program.Prepend("// " + codePath);
                    program.Prepend("// " + fullPath);
                }
            }

            return program;
        }

        private ReadOnlySpan<byte> GetShaderCode(ulong gpuVa)
        {
            return GetShaderCodeImpl(gpuVa, ShaderHeaderSize);
        }

        private ReadOnlySpan<byte> GetShaderCodeHeaderless(ulong gpuVa)
        {
            return GetShaderCodeImpl(gpuVa, 0);
        }

        private ReadOnlySpan<byte> GetShaderCodeImpl(ulong gpuVa, ulong size)
        {
            while (true)
            {
                ulong op = _context.MemoryAccessor.Read<ulong>(gpuVa + size);

                size += sizeof(ulong);

                if (op == 0x50b0000000070f00 || op == 0)
                {
                    break;
                }
            }

            return _context.MemoryAccessor.GetSpan(gpuVa, size);
        }

        /// <summary>
        /// Performs backwards propagation of interpolation qualifiers or later shader stages input,
        /// to ealier shader stages output.
        /// This is required by older versions of OpenGL (pre-4.3).
        /// </summary>
        /// <param name="gs">Graphics shader cached code</param>
        private void BackpropQualifiers(GraphicsShader gs)
        {
            ShaderProgram fragmentShader = gs.Shaders[4];

            bool isFirst = true;

            for (int stage = 3; stage >= 0; stage--)
            {
                if (gs.Shaders[stage] == null)
                {
                    continue;
                }

                // We need to iterate backwards, since we do name replacement,
                // and it would otherwise replace a subset of the longer names.
                for (int attr = 31; attr >= 0; attr--)
                {
                    string iq = fragmentShader?.Info.InterpolationQualifiers[attr].ToGlslQualifier() ?? string.Empty;

                    if (isFirst && !string.IsNullOrEmpty(iq))
                    {
                        gs.Shaders[stage].Replace($"{DefineNames.OutQualifierPrefixName}{attr}", iq);
                    }
                    else
                    {
                        gs.Shaders[stage].Replace($"{DefineNames.OutQualifierPrefixName}{attr} ", string.Empty);
                    }
                }

                isFirst = false;
            }
        }

        /// <summary>
        /// Gets the primitive topology for the current draw.
        /// This is required by geometry shaders.
        /// </summary>
        /// <returns>Primitive topology</returns>
        private InputTopology GetPrimitiveTopology()
        {
            switch (_context.Methods.PrimitiveType)
            {
                case PrimitiveType.Points:
                    return InputTopology.Points;
                case PrimitiveType.Lines:
                case PrimitiveType.LineLoop:
                case PrimitiveType.LineStrip:
                    return InputTopology.Lines;
                case PrimitiveType.LinesAdjacency:
                case PrimitiveType.LineStripAdjacency:
                    return InputTopology.LinesAdjacency;
                case PrimitiveType.Triangles:
                case PrimitiveType.TriangleStrip:
                case PrimitiveType.TriangleFan:
                    return InputTopology.Triangles;
                case PrimitiveType.TrianglesAdjacency:
                case PrimitiveType.TriangleStripAdjacency:
                    return InputTopology.TrianglesAdjacency;
            }

            return InputTopology.Points;
        }

        /// <summary>
        /// Check if the target of a given texture is texture buffer.
        /// This is required as 1D textures and buffer textures shares the same sampler type on binary shader code,
        /// but not on GLSL.
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <param name="stageIndex">Index of the shader stage</param>
        /// <param name="index">Index of the texture (this is the shader "fake" handle)</param>
        /// <returns>True if the texture is a buffer texture, false otherwise</returns>
        private bool QueryIsTextureBuffer(GpuState state, int stageIndex, int index)
        {
            return GetTextureDescriptor(state, stageIndex, index).UnpackTextureTarget() == TextureTarget.TextureBuffer;
        }

        /// <summary>
        /// Check if the target of a given texture is texture rectangle.
        /// This is required as 2D textures and rectangle textures shares the same sampler type on binary shader code,
        /// but not on GLSL.
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <param name="stageIndex">Index of the shader stage</param>
        /// <param name="index">Index of the texture (this is the shader "fake" handle)</param>
        /// <returns>True if the texture is a rectangle texture, false otherwise</returns>
        private bool QueryIsTextureRectangle(GpuState state, int stageIndex, int index)
        {
            var descriptor = GetTextureDescriptor(state, stageIndex, index);

            TextureTarget target = descriptor.UnpackTextureTarget();

            bool is2DTexture = target == TextureTarget.Texture2D || target == TextureTarget.Texture2DRect;

            return !descriptor.UnpackTextureCoordNormalized() && is2DTexture;
        }

        /// <summary>
        /// Gets the texture descriptor for a given texture on the pool.
        /// </summary>
        /// <param name="state">Current GPU state</param>
        /// <param name="stageIndex">Index of the shader stage</param>
        /// <param name="index">Index of the texture (this is the shader "fake" handle)</param>
        /// <returns>Texture descriptor</returns>
        private TextureDescriptor GetTextureDescriptor(GpuState state, int stageIndex, int index)
        {
            return _context.Methods.TextureManager.GetGraphicsTextureDescriptor(state, stageIndex, index);
        }

        /// <summary>
        /// Returns information required by both compute and graphics shader compilation.
        /// </summary>
        /// <param name="info">Information queried</param>
        /// <returns>Requested information</returns>
        private int QueryInfoCommon(QueryInfoName info)
        {
            return info switch
            {
                QueryInfoName.StorageBufferOffsetAlignment => _context.Capabilities.StorageBufferOffsetAlignment,
                QueryInfoName.SupportsNonConstantTextureOffset => Convert.ToInt32(_context.Capabilities.SupportsNonConstantTextureOffset),
                _ => 0
            };
        }

        /// <summary>
        /// Prints a warning from the shader code translator.
        /// </summary>
        /// <param name="message">Warning message</param>
        private static void PrintLog(string message)
        {
            Logger.PrintWarning(LogClass.Gpu, $"Shader translator: {message}");
        }

        /// <summary>
        /// Disposes the shader cache, deleting all the cached shaders.
        /// It's an error to use the shader cache after disposal.
        /// </summary>
        public void Dispose()
        {
            foreach (ComputeShader shader in _cpPrograms)
            {
                shader.HostProgram.Dispose();
            }
 
            foreach (GraphicsShader shader in _gpPrograms)
            {
                shader.HostProgram.Dispose();
            }
        }
    }
}