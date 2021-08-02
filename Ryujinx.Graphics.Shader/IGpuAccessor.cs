using System;

namespace Ryujinx.Graphics.Shader
{
    public interface IGpuAccessor
    {
        void Log(string message)
        {
            // No default log output.
        }

        T MemoryRead<T>(ulong address) where T : unmanaged;

        bool MemoryMapped(ulong address)
        {
            return true;
        }

        AttributeType QueryAttributeType(int location)
        {
            return AttributeType.Float;
        }

        int QueryComputeLocalSizeX()
        {
            return 1;
        }

        int QueryComputeLocalSizeY()
        {
            return 1;
        }

        int QueryComputeLocalSizeZ()
        {
            return 1;
        }

        int QueryComputeLocalMemorySize()
        {
            return 0x1000;
        }

        int QueryComputeSharedMemorySize()
        {
            return 0xc000;
        }

        uint QueryConstantBufferUse()
        {
            return 0;
        }

        bool QueryIsTextureBuffer(int handle, int cbufSlot = -1)
        {
            return false;
        }

        bool QueryIsTextureRectangle(int handle, int cbufSlot = -1)
        {
            return false;
        }

        InputTopology QueryPrimitiveTopology()
        {
            return InputTopology.Points;
        }

        bool QueryProgramPointSize()
        {
            return true;
        }

        float QueryPointSize()
        {
            return 1f;
        }

        int QueryStorageBufferOffsetAlignment()
        {
            return 16;
        }

        bool QuerySupportsImageLoadFormatted()
        {
            return true;
        }

        bool QuerySupportsNonConstantTextureOffset()
        {
            return true;
        }

        bool QuerySupportsTextureShadowLod()
        {
            return true;
        }

        TextureFormat QueryTextureFormat(int handle, int cbufSlot = -1)
        {
            return TextureFormat.R8G8B8A8Unorm;
        }

        bool QueryTransformDepthMinusOneToOne()
        {
            return false;
        }

        bool QueryTransformFeedbackEnabled()
        {
            return false;
        }

        ReadOnlySpan<byte> QueryTransformFeedbackVaryingLocations(int bufferIndex)
        {
            return ReadOnlySpan<byte>.Empty;
        }

        int QueryTransformFeedbackStride(int bufferIndex)
        {
            return 0;
        }

        bool QueryEarlyZForce()
        {
            return false;
        }
    }
}
