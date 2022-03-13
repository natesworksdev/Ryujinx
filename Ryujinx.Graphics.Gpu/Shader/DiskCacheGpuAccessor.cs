using Ryujinx.Common.Logging;
using Ryujinx.Graphics.Gpu.Image;
using Ryujinx.Graphics.Shader;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Represents a GPU state and memory accessor.
    /// </summary>
    class DiskCacheGpuAccessor : GpuAccessorBase, IGpuAccessor
    {
        private readonly ReadOnlyMemory<byte> _data;
        private readonly ReadOnlyMemory<byte> _cb1Data;
        private readonly ShaderSpecializationState _specState;
        private readonly int _stageIndex;
        private ResourceCounts _resourceCounts;

        /// <summary>
        /// Creates a new instance of the cached GPU state accessor for shader translation.
        /// </summary>
        /// <param name="context">GPU context</param>
        /// <param name="data">The data of the shader</param>
        /// <param name="cb1Data">The constant buffer 1 data of the shader</param>
        /// <param name="specState">Shader specialization state</param>
        /// <param name="stageIndex">Shader stage index</param>
        public DiskCacheGpuAccessor(
            GpuContext context,
            ReadOnlyMemory<byte> data,
            ReadOnlyMemory<byte> cb1Data,
            ShaderSpecializationState specState,
            ResourceCounts counts,
            int stageIndex) : base(context)
        {
            _data = data;
            _cb1Data = cb1Data;
            _specState = specState;
            _stageIndex = stageIndex;
            _resourceCounts = counts;
        }

        /// <summary>
        /// Reads data from the constant buffer 1.
        /// </summary>
        /// <param name="offset">Offset in bytes to read from</param>
        /// <returns>Value at the given offset</returns>
        public uint ConstantBuffer1Read(int offset)
        {
            return MemoryMarshal.Cast<byte, uint>(_cb1Data.Span.Slice(offset))[0];
        }

        /// <summary>
        /// Prints a log message.
        /// </summary>
        /// <param name="message">Message to print</param>
        public void Log(string message)
        {
            Logger.Warning?.Print(LogClass.Gpu, $"Shader translator: {message}");
        }

        /// <summary>
        /// Gets a span of the specified memory location, containing shader code.
        /// </summary>
        /// <param name="address">GPU virtual address of the data</param>
        /// <param name="minimumSize">Minimum size that the returned span may have</param>
        /// <returns>Span of the memory location</returns>
        public ReadOnlySpan<ulong> GetCode(ulong address, int minimumSize)
        {
            return MemoryMarshal.Cast<byte, ulong>(_data.Span.Slice((int)address));
        }

        public int QueryBindingConstantBuffer(int index)
        {
            return _resourceCounts.UniformBuffersCount++;
        }

        public int QueryBindingStorageBuffer(int index)
        {
            return _resourceCounts.StorageBuffersCount++;
        }

        public int QueryBindingTexture(int index)
        {
            return _resourceCounts.TexturesCount++;
        }

        public int QueryBindingImage(int index)
        {
            return _resourceCounts.ImagesCount++;
        }

        /// <summary>
        /// Queries Local Size X for compute shaders.
        /// </summary>
        /// <returns>Local Size X</returns>
        public int QueryComputeLocalSizeX() => _specState.ComputeState.LocalSizeX;

        /// <summary>
        /// Queries Local Size Y for compute shaders.
        /// </summary>
        /// <returns>Local Size Y</returns>
        public int QueryComputeLocalSizeY() => _specState.ComputeState.LocalSizeY;

        /// <summary>
        /// Queries Local Size Z for compute shaders.
        /// </summary>
        /// <returns>Local Size Z</returns>
        public int QueryComputeLocalSizeZ() => _specState.ComputeState.LocalSizeZ;

        /// <summary>
        /// Queries Local Memory size in bytes for compute shaders.
        /// </summary>
        /// <returns>Local Memory size in bytes</returns>
        public int QueryComputeLocalMemorySize() => _specState.ComputeState.LocalMemorySize;

        /// <summary>
        /// Queries Shared Memory size in bytes for compute shaders.
        /// </summary>
        /// <returns>Shared Memory size in bytes</returns>
        public int QueryComputeSharedMemorySize() => _specState.ComputeState.SharedMemorySize;

        /// <summary>
        /// Queries Constant Buffer usage information.
        /// </summary>
        /// <returns>A mask where each bit set indicates a bound constant buffer</returns>
        public uint QueryConstantBufferUse() => _specState.ConstantBufferUse;

        /// <summary>
        /// Queries current primitive topology for geometry shaders.
        /// </summary>
        /// <returns>Current primitive topology</returns>
        public InputTopology QueryPrimitiveTopology() => ConvertToInputTopology(_specState.GraphicsState.Topology, _specState.GraphicsState.TessellationMode);

        /// <summary>
        /// Queries the tessellation evaluation shader primitive winding order.
        /// </summary>
        /// <returns>True if the primitive winding order is clockwise, false if counter-clockwise</returns>
        public bool QueryTessCw() => _specState.GraphicsState.TessellationMode.UnpackCw();

        /// <summary>
        /// Queries the tessellation evaluation shader abstract patch type.
        /// </summary>
        /// <returns>Abstract patch type</returns>
        public TessPatchType QueryTessPatchType() => _specState.GraphicsState.TessellationMode.UnpackPatchType();

        /// <summary>
        /// Queries the tessellation evaluation shader spacing between tessellated vertices of the patch.
        /// </summary>
        /// <returns>Spacing between tessellated vertices of the patch</returns>
        public TessSpacing QueryTessSpacing() => _specState.GraphicsState.TessellationMode.UnpackSpacing();

        /// <summary>
        /// Queries texture format information, for shaders using image load or store.
        /// </summary>
        /// <remarks>
        /// This only returns non-compressed color formats.
        /// If the format of the texture is a compressed, depth or unsupported format, then a default value is returned.
        /// </remarks>
        /// <param name="handle">Texture handle</param>
        /// <param name="cbufSlot">Constant buffer slot for the texture handle</param>
        /// <returns>Color format of the non-compressed texture</returns>
        public TextureFormat QueryTextureFormat(int handle, int cbufSlot)
        {
            (uint format, bool formatSrgb) = _specState.GetFormat(_stageIndex, handle, cbufSlot);
            return ConvertToTextureFormat(format, formatSrgb);
        }

        /// <summary>
        /// Queries sampler type information.
        /// </summary>
        /// <param name="handle">Texture handle</param>
        /// <param name="cbufSlot">Constant buffer slot for the texture handle</param>
        /// <returns>The sampler type value for the given handle</returns>
        public SamplerType QuerySamplerType(int handle, int cbufSlot)
        {
            return _specState.GetTextureTarget(_stageIndex, handle, cbufSlot).ConvertSamplerType();
        }

        public bool QueryIsTextureRectangle(int handle, int cbufSlot)
        {
            return !_specState.GetCoordNormalized(_stageIndex, handle, cbufSlot);
        }

        /// <summary>
        /// Queries transform feedback enable state.
        /// </summary>
        /// <returns>True if the shader uses transform feedback, false otherwise</returns>
        public bool QueryTransformFeedbackEnabled()
        {
            return _specState.TransformFeedbackDescriptors != null;
        }

        /// <summary>
        /// Queries the varying locations that should be written to the transform feedback buffer.
        /// </summary>
        /// <param name="bufferIndex">Index of the transform feedback buffer</param>
        /// <returns>Varying locations for the specified buffer</returns>
        public ReadOnlySpan<byte> QueryTransformFeedbackVaryingLocations(int bufferIndex)
        {
            return _specState.TransformFeedbackDescriptors[bufferIndex].AsSpan();
        }

        /// <summary>
        /// Queries the stride (in bytes) of the per vertex data written into the transform feedback buffer.
        /// </summary>
        /// <param name="bufferIndex">Index of the transform feedback buffer</param>
        /// <returns>Stride for the specified buffer</returns>
        public int QueryTransformFeedbackStride(int bufferIndex)
        {
            return _specState.TransformFeedbackDescriptors[bufferIndex].Stride;
        }

        /// <summary>
        /// Queries if host state forces early depth testing.
        /// </summary>
        /// <returns>True if early depth testing is forced</returns>
        public bool QueryEarlyZForce()
        {
            return _specState.GraphicsState.EarlyZForce;
        }
    }
}
