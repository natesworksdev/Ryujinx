using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.OpenGL.Image
{
    /// <summary>
    /// Host bindless texture handle manager.
    /// </summary>
    class BindlessHandleManager
    {
        // This uses two tables to store the handles.
        // The first level has a fixed size, and stores indices pointing into the second level.
        // The second level is dynamically allocated, and stores the host handles themselves (among other things).
        //
        // The first level is indexed using the low bits of the (guest) texture ID and sampler ID.
        // The second level can be thought as a 2D array, where the first dimension is indexed using the index from
        // the first level, and the second dimension is indexed using the high texture ID and sampler ID bits.

        private const int BlockSize = 0x10000;

        private readonly BitMap _freeList;

        /// <summary>
        /// Second level block state.
        /// </summary>
        private struct Block
        {
            public int Index;
            public int ReferenceCount;
        }

        private readonly Block[] _blocks;

        private readonly Dictionary<int, List<int>> _texturesOnBlocks;

        /// <summary>
        /// Handle entry accessed by the shader.
        /// </summary>
        private struct HandleEntry
        {
            public long Handle;
            public float Scale;
            public uint Padding;

            public HandleEntry(long handle, float scale)
            {
                Handle = handle;
                Scale = scale;
                Padding = 0;
            }
        }

        private readonly TypedBuffer<int> _textureList; // First level.
        private readonly TypedBuffer<HandleEntry> _handleList; // Second level.

        private readonly ITexture _bufferTextureForTextureList;
        private readonly ITexture _bufferTextureForHandleList;

        /// <summary>
        /// Creates a new instance of the host bindless texture handle manager.
        /// </summary>
        /// <param name="renderer">Renderer</param>
        public BindlessHandleManager(OpenGLRenderer renderer)
        {
            _freeList = new BitMap();
            _blocks = new Block[0x100000];
            _texturesOnBlocks = new Dictionary<int, List<int>>();

            _textureList = new TypedBuffer<int>(renderer, 0x100000);
            _handleList = new TypedBuffer<HandleEntry>(renderer, BlockSize);

            _bufferTextureForTextureList = CreateBufferTexture(renderer, _textureList);
            _bufferTextureForHandleList = CreateBufferTexture(renderer, _handleList);
        }

        /// <summary>
        /// Creates a buffer texture with the provided buffer.
        /// </summary>
        /// <typeparam name="T">Type of the data on the buffer</typeparam>
        /// <param name="renderer">Renderer</param>
        /// <param name="buffer">Buffer</param>
        /// <returns>Buffer texture</returns>
        private static ITexture CreateBufferTexture<T>(OpenGLRenderer renderer, TypedBuffer<T> buffer) where T : unmanaged
        {
            int bytesPerPixel = Unsafe.SizeOf<T>();

            Format format = bytesPerPixel switch
            {
                1 => Format.R8Uint,
                2 => Format.R16Uint,
                4 => Format.R32Uint,
                8 => Format.R32G32Uint,
                16 => Format.R32G32B32A32Uint,
                _ => throw new ArgumentException("Invalid type specified.")
            };

            ITexture texture = renderer.CreateTexture(new TextureCreateInfo(
                buffer.Size / bytesPerPixel,
                1,
                1,
                1,
                1,
                1,
                1,
                bytesPerPixel,
                format,
                DepthStencilMode.Depth,
                Target.TextureBuffer,
                SwizzleComponent.Red,
                SwizzleComponent.Green,
                SwizzleComponent.Blue,
                SwizzleComponent.Alpha));

            texture.SetStorage(buffer.GetBufferRange());

            return texture;
        }

        /// <summary>
        /// Binds the multi-level handle table buffer textures on the host.
        /// </summary>
        /// <param name="renderer">Renderer</param>
        public void Bind(OpenGLRenderer renderer)
        {
            // TODO: Proper shader stage (doesn't really matter as the OpenGL backend doesn't use this at all).
            renderer.Pipeline.SetTextureAndSampler(ShaderStage.Vertex, 0, _bufferTextureForTextureList, null);
            renderer.Pipeline.SetTextureAndSampler(ShaderStage.Vertex, 1, _bufferTextureForHandleList, null);
        }

        /// <summary>
        /// Adds a new host handle to the table.
        /// </summary>
        /// <param name="textureId">Guest ID of the texture the handle belongs to</param>
        /// <param name="samplerId">Guest ID of the sampler the handle belongs to</param>
        /// <param name="handle">Host handle</param>
        /// <param name="scale">Texture scale factor</param>
        public void AddBindlessHandle(int textureId, int samplerId, long handle, float scale)
        {
            int tableIndex = GetTableIndex(textureId, samplerId);
            int blockIndex = GetBlockIndex(tableIndex);
            int subIndex = GetSubIndex(textureId, samplerId);

            _blocks[tableIndex].ReferenceCount++;

            if (!_texturesOnBlocks.TryGetValue(subIndex, out List<int> list))
            {
                _texturesOnBlocks.Add(subIndex, list = new List<int>());
            }

            list.Add(tableIndex);

            if (_handleList.EnsureCapacity((blockIndex + 1) * BlockSize))
            {
                _bufferTextureForHandleList.SetStorage(_handleList.GetBufferRange());
            }

            _handleList.Write(blockIndex * BlockSize + subIndex, new HandleEntry(handle, scale));
        }

        /// <summary>
        /// Removes a handle from the table.
        /// </summary>
        /// <param name="textureId">Guest ID of the texture the handle belongs to</param>
        /// <param name="samplerId">Guest ID of the sampler the handle belongs to</param>
        public void RemoveBindlessHandle(int textureId, int samplerId)
        {
            int tableIndex = GetTableIndex(textureId, samplerId);
            int blockIndex = _blocks[tableIndex].Index - 1;
            int subIndex = GetSubIndex(textureId, samplerId);

            Debug.Assert(blockIndex >= 0);

            _handleList.Write(blockIndex * BlockSize + subIndex, new HandleEntry(0L, 0f));

            if (_texturesOnBlocks.TryGetValue(subIndex, out List<int> list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    PutBlockIndex(list[i]);
                }

                _texturesOnBlocks.Remove(subIndex);
            }
        }

        /// <summary>
        /// Gets a index, pointing inside the second level table, from the first level table.
        /// This will dynamically allocate a new block on the second level if needed, and write
        /// its index into the first level.
        /// </summary>
        /// <param name="tableIndex">Index pointing inside the first level table, where the other index is located</param>
        /// <returns>The index of a block on the second level table</returns>
        private int GetBlockIndex(int tableIndex)
        {
            if (_blocks[tableIndex].Index != 0)
            {
                return _blocks[tableIndex].Index - 1;
            }

            int blockIndex = _freeList.FindFirstUnset();

            _freeList.Set(blockIndex);

            _blocks[tableIndex].Index = blockIndex + 1;

            _textureList.Write(tableIndex, blockIndex);

            return blockIndex;
        }

        /// <summary>
        /// Indicates that a given block was dereferenced, eventually freeing it if no longer in use.
        /// </summary>
        /// <param name="tableIndex">Index of the block index on the first level table</param>
        private void PutBlockIndex(int tableIndex)
        {
            if (--_blocks[tableIndex].ReferenceCount == 0)
            {
                _freeList.Clear(_blocks[tableIndex].Index - 1);

                _blocks[tableIndex].Index = 0;
            }
        }

        /// <summary>
        /// Assembles a index from the low bits of the texture and sampler ID, used for the first level indexing.
        /// </summary>
        /// <param name="textureId">Texture ID</param>
        /// <param name="samplerId">Sampler ID</param>
        /// <returns>The first level index</returns>
        private static int GetTableIndex(int textureId, int samplerId) => (textureId >> 8) | ((samplerId >> 8) << 12);

        /// <summary>
        /// Assembles a index from the low bits of the texture and sampler ID, used for the second level indexing.
        /// </summary>
        /// <param name="textureId">Texture ID</param>
        /// <param name="samplerId">Sampler ID</param>
        /// <returns>The second level index</returns>
        private static int GetSubIndex(int textureId, int samplerId) => (textureId & 0xff) | ((samplerId & 0xff) << 8);
    }
}
