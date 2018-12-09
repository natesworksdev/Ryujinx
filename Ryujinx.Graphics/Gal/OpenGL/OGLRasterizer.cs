using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    class OGLRasterizer : IGalRasterizer
    {
        private static Dictionary<GalVertexAttribSize, int> AttribElements =
                   new Dictionary<GalVertexAttribSize, int>()
        {
            { GalVertexAttribSize._32_32_32_32, 4 },
            { GalVertexAttribSize._32_32_32,    3 },
            { GalVertexAttribSize._16_16_16_16, 4 },
            { GalVertexAttribSize._32_32,       2 },
            { GalVertexAttribSize._16_16_16,    3 },
            { GalVertexAttribSize._8_8_8_8,     4 },
            { GalVertexAttribSize._16_16,       2 },
            { GalVertexAttribSize._32,          1 },
            { GalVertexAttribSize._8_8_8,       3 },
            { GalVertexAttribSize._8_8,         2 },
            { GalVertexAttribSize._16,          1 },
            { GalVertexAttribSize._8,           1 },
            { GalVertexAttribSize._10_10_10_2,  4 },
            { GalVertexAttribSize._11_11_10,    3 }
        };

        private static Dictionary<GalVertexAttribSize, VertexAttribPointerType> FloatAttribTypes =
                   new Dictionary<GalVertexAttribSize, VertexAttribPointerType>()
        {
            { GalVertexAttribSize._32_32_32_32, VertexAttribPointerType.Float     },
            { GalVertexAttribSize._32_32_32,    VertexAttribPointerType.Float     },
            { GalVertexAttribSize._16_16_16_16, VertexAttribPointerType.HalfFloat },
            { GalVertexAttribSize._32_32,       VertexAttribPointerType.Float     },
            { GalVertexAttribSize._16_16_16,    VertexAttribPointerType.HalfFloat },
            { GalVertexAttribSize._16_16,       VertexAttribPointerType.HalfFloat },
            { GalVertexAttribSize._32,          VertexAttribPointerType.Float     },
            { GalVertexAttribSize._16,          VertexAttribPointerType.HalfFloat }
        };

        private static Dictionary<GalVertexAttribSize, VertexAttribPointerType> SignedAttribTypes =
                   new Dictionary<GalVertexAttribSize, VertexAttribPointerType>()
        {
            { GalVertexAttribSize._32_32_32_32, VertexAttribPointerType.Int           },
            { GalVertexAttribSize._32_32_32,    VertexAttribPointerType.Int           },
            { GalVertexAttribSize._16_16_16_16, VertexAttribPointerType.Short         },
            { GalVertexAttribSize._32_32,       VertexAttribPointerType.Int           },
            { GalVertexAttribSize._16_16_16,    VertexAttribPointerType.Short         },
            { GalVertexAttribSize._8_8_8_8,     VertexAttribPointerType.Byte          },
            { GalVertexAttribSize._16_16,       VertexAttribPointerType.Short         },
            { GalVertexAttribSize._32,          VertexAttribPointerType.Int           },
            { GalVertexAttribSize._8_8_8,       VertexAttribPointerType.Byte          },
            { GalVertexAttribSize._8_8,         VertexAttribPointerType.Byte          },
            { GalVertexAttribSize._16,          VertexAttribPointerType.Short         },
            { GalVertexAttribSize._8,           VertexAttribPointerType.Byte          },
            { GalVertexAttribSize._10_10_10_2,  VertexAttribPointerType.Int2101010Rev }
        };

        private static Dictionary<GalVertexAttribSize, VertexAttribPointerType> UnsignedAttribTypes =
                   new Dictionary<GalVertexAttribSize, VertexAttribPointerType>()
        {
            { GalVertexAttribSize._32_32_32_32, VertexAttribPointerType.UnsignedInt             },
            { GalVertexAttribSize._32_32_32,    VertexAttribPointerType.UnsignedInt             },
            { GalVertexAttribSize._16_16_16_16, VertexAttribPointerType.UnsignedShort           },
            { GalVertexAttribSize._32_32,       VertexAttribPointerType.UnsignedInt             },
            { GalVertexAttribSize._16_16_16,    VertexAttribPointerType.UnsignedShort           },
            { GalVertexAttribSize._8_8_8_8,     VertexAttribPointerType.UnsignedByte            },
            { GalVertexAttribSize._16_16,       VertexAttribPointerType.UnsignedShort           },
            { GalVertexAttribSize._32,          VertexAttribPointerType.UnsignedInt             },
            { GalVertexAttribSize._8_8_8,       VertexAttribPointerType.UnsignedByte            },
            { GalVertexAttribSize._8_8,         VertexAttribPointerType.UnsignedByte            },
            { GalVertexAttribSize._16,          VertexAttribPointerType.UnsignedShort           },
            { GalVertexAttribSize._8,           VertexAttribPointerType.UnsignedByte            },
            { GalVertexAttribSize._10_10_10_2,  VertexAttribPointerType.UnsignedInt2101010Rev   },
            { GalVertexAttribSize._11_11_10,    VertexAttribPointerType.UnsignedInt10F11F11FRev }
        };

        private const long MaxVertexBufferCacheSize = 128 * 1024 * 1024;
        private const long MaxIndexBufferCacheSize  = 64  * 1024 * 1024;

        private int[] VertexBuffers;

        private struct CachedVao
        {
            public int[] Attributes { get; }

            public GalVertexAttribArray[] Arrays { get; }

            public int Handle { get; }

            public CachedVao(int[] attributes, GalVertexAttribArray[] arrays)
            {
                Attributes = attributes;
                Arrays     = arrays;

                Handle = GL.GenVertexArray();
            }
        }

        private OGLResourceCache<int, CachedVao> VaoCache;

        private OGLResourceCache<long, OGLStreamBuffer> VboCache;

        private class CachedIbo
        {
            public OGLStreamBuffer Buffer { get; }

            public long VertexCount { get; set; }

            public CachedIbo(OGLStreamBuffer buffer)
            {
                Buffer = buffer;
            }
        }

        private OGLResourceCache<long, CachedIbo> IboCache;

        private struct IbInfo
        {
            public int Count;
            public int ElemSizeLog2;

            public DrawElementsType Type;
        }

        private IbInfo IndexBuffer;

        public OGLRasterizer()
        {
            VertexBuffers = new int[32];

            VaoCache = new OGLResourceCache<int, CachedVao>(DeleteVao, 64 * 1024);

            VboCache = new OGLResourceCache<long, OGLStreamBuffer>(DeleteBuffer, MaxVertexBufferCacheSize);

            IboCache = new OGLResourceCache<long, CachedIbo>(DeleteIbo, MaxIndexBufferCacheSize);

            IndexBuffer = new IbInfo();
        }

        private static void DeleteVao(CachedVao vao)
        {
            GL.DeleteVertexArray(vao.Handle);
        }

        private static void DeleteBuffer(OGLStreamBuffer buffer)
        {
            buffer.Dispose();
        }

        private static void DeleteIbo(CachedIbo ibo)
        {
            ibo.Buffer.Dispose();
        }

        public void LockCaches()
        {
            VaoCache.Lock();
            VboCache.Lock();
            IboCache.Lock();
        }

        public void UnlockCaches()
        {
            VaoCache.Unlock();
            VboCache.Unlock();
            IboCache.Unlock();
        }

        public void ClearBuffers(
            GalClearBufferFlags Flags,
            int Attachment,
            float Red,
            float Green,
            float Blue,
            float Alpha,
            float Depth,
            int Stencil)
        {
            GL.ColorMask(
                Attachment,
                Flags.HasFlag(GalClearBufferFlags.ColorRed),
                Flags.HasFlag(GalClearBufferFlags.ColorGreen),
                Flags.HasFlag(GalClearBufferFlags.ColorBlue),
                Flags.HasFlag(GalClearBufferFlags.ColorAlpha));

            GL.ClearBuffer(ClearBuffer.Color, Attachment, new float[] { Red, Green, Blue, Alpha });

            GL.ColorMask(Attachment, true, true, true, true);
            GL.DepthMask(true);

            if (Flags.HasFlag(GalClearBufferFlags.Depth))
            {
                GL.ClearBuffer(ClearBuffer.Depth, 0, ref Depth);
            }

            if (Flags.HasFlag(GalClearBufferFlags.Stencil))
            {
                GL.ClearBuffer(ClearBuffer.Stencil, 0, ref Stencil);
            }
        }

        public bool IsVboCached(long key, long size)
        {
            return VboCache.TryGetSize(key, out long vbSize) && vbSize >= size;
        }

        public bool IsIboCached(long key, long size, out long vertexCount)
        {
            if (IboCache.TryGetSizeAndValue(key, out long ibSize, out CachedIbo ibo) && ibSize >= size)
            {
                vertexCount = ibo.VertexCount;

                return true;
            }

            vertexCount = 0;

            return false;
        }

        public bool TryBindVao(ReadOnlySpan<int> rawAttributes, GalVertexAttribArray[] arrays)
        {
            long hash = CalculateHash(arrays);

            if (!VaoCache.TryGetValue(hash, out CachedVao vao))
            {
                return false;
            }

            if (rawAttributes.Length != vao.Attributes.Length)
            {
                return false;
            }

            if (arrays.Length != vao.Arrays.Length)
            {
                return false;
            }

            for (int index = 0; index < rawAttributes.Length; index++)
            {
                if (rawAttributes[index] != vao.Attributes[index])
                {
                    return false;
                }
            }

            for (int index = 0; index < arrays.Length; index++)
            {
                if (!arrays[index].Equals(vao.Arrays[index]))
                {
                    return false;
                }
            }

            GL.BindVertexArray(vao.Handle);

            return true;
        }

        public void CreateVao(
            ReadOnlySpan<int>      rawAttributes,
            GalVertexAttrib[]      attributes,
            GalVertexAttribArray[] arrays)
        {
            CachedVao vao = new CachedVao(rawAttributes.ToArray(), arrays);

            long hash = CalculateHash(arrays);

            VaoCache.AddOrUpdate(hash, 1, vao, 1);

            GL.BindVertexArray(vao.Handle);

            for (int index = 0; index < attributes.Length; index++)
            {
                GalVertexAttrib attrib = attributes[index];

                GalVertexAttribArray array = arrays[attrib.ArrayIndex];

                //Skip uninitialized attributes.
                if (attrib.Size == 0 || !array.Enabled)
                {
                    continue;
                }

                if (!VboCache.TryGetValue(array.VboKey, out OGLStreamBuffer vbo))
                {
                    continue;
                }

                VboCache.AddDependency(array.VboKey, VaoCache, hash);

                GL.BindBuffer(BufferTarget.ArrayBuffer, vbo.Handle);

                bool Unsigned =
                    attrib.Type == GalVertexAttribType.Unorm ||
                    attrib.Type == GalVertexAttribType.Uint  ||
                    attrib.Type == GalVertexAttribType.Uscaled;

                bool Normalize =
                    attrib.Type == GalVertexAttribType.Snorm ||
                    attrib.Type == GalVertexAttribType.Unorm;

                VertexAttribPointerType Type = 0;

                if (attrib.Type == GalVertexAttribType.Float)
                {
                    Type = GetType(FloatAttribTypes, attrib);
                }
                else if (Unsigned)
                {
                    Type = GetType(UnsignedAttribTypes, attrib);
                }
                else
                {
                    Type = GetType(SignedAttribTypes, attrib);
                }

                if (!AttribElements.TryGetValue(attrib.Size, out int Size))
                {
                    throw new InvalidOperationException($"Invalid attribute size \"{attrib.Size}\".");
                }

                int Offset = attrib.Offset;

                if (array.Stride != 0)
                {
                    GL.EnableVertexAttribArray(index);

                    if (attrib.Type == GalVertexAttribType.Sint ||
                        attrib.Type == GalVertexAttribType.Uint)
                    {
                        IntPtr Pointer = new IntPtr(Offset);

                        VertexAttribIntegerType IType = (VertexAttribIntegerType)Type;

                        GL.VertexAttribIPointer(index, Size, IType, array.Stride, Pointer);
                    }
                    else
                    {
                        GL.VertexAttribPointer(index, Size, Type, Normalize, array.Stride, Offset);
                    }
                }
                else
                {
                    GL.DisableVertexAttribArray(index);

                    SetConstAttrib(attrib, (uint)index);
                }

                if (array.Divisor != 0)
                {
                    GL.VertexAttribDivisor(index, 1);
                }
                else
                {
                    GL.VertexAttribDivisor(index, 0);
                }
            }
        }

        private long CalculateHash(GalVertexAttribArray[] arrays)
        {
            if (arrays.Length == 1)
            {
                return arrays[0].VboKey;
            }

            long hash = 17;

            for (int index = 0; index < arrays.Length; index++)
            {
                hash = hash * 23 + arrays[index].VboKey;
            }

            return hash;
        }

        private static VertexAttribPointerType GetType(Dictionary<GalVertexAttribSize, VertexAttribPointerType> Dict, GalVertexAttrib Attrib)
        {
            if (!Dict.TryGetValue(Attrib.Size, out VertexAttribPointerType Type))
            {
                ThrowUnsupportedAttrib(Attrib);
            }

            return Type;
        }

        private unsafe static void SetConstAttrib(GalVertexAttrib Attrib, uint Index)
        {
            if (Attrib.Size == GalVertexAttribSize._10_10_10_2 ||
                Attrib.Size == GalVertexAttribSize._11_11_10)
            {
                ThrowUnsupportedAttrib(Attrib);
            }

            fixed (byte* Ptr = Attrib.Data)
            {
                if (Attrib.Type == GalVertexAttribType.Unorm)
                {
                    switch (Attrib.Size)
                    {
                        case GalVertexAttribSize._8:
                        case GalVertexAttribSize._8_8:
                        case GalVertexAttribSize._8_8_8:
                        case GalVertexAttribSize._8_8_8_8:
                            GL.VertexAttrib4N(Index, Ptr);
                            break;

                        case GalVertexAttribSize._16:
                        case GalVertexAttribSize._16_16:
                        case GalVertexAttribSize._16_16_16:
                        case GalVertexAttribSize._16_16_16_16:
                            GL.VertexAttrib4N(Index, (ushort*)Ptr);
                            break;

                        case GalVertexAttribSize._32:
                        case GalVertexAttribSize._32_32:
                        case GalVertexAttribSize._32_32_32:
                        case GalVertexAttribSize._32_32_32_32:
                            GL.VertexAttrib4N(Index, (uint*)Ptr);
                            break;
                    }
                }
                else if (Attrib.Type == GalVertexAttribType.Snorm)
                {
                    switch (Attrib.Size)
                    {
                        case GalVertexAttribSize._8:
                        case GalVertexAttribSize._8_8:
                        case GalVertexAttribSize._8_8_8:
                        case GalVertexAttribSize._8_8_8_8:
                            GL.VertexAttrib4N(Index, (sbyte*)Ptr);
                            break;

                        case GalVertexAttribSize._16:
                        case GalVertexAttribSize._16_16:
                        case GalVertexAttribSize._16_16_16:
                        case GalVertexAttribSize._16_16_16_16:
                            GL.VertexAttrib4N(Index, (short*)Ptr);
                            break;

                        case GalVertexAttribSize._32:
                        case GalVertexAttribSize._32_32:
                        case GalVertexAttribSize._32_32_32:
                        case GalVertexAttribSize._32_32_32_32:
                            GL.VertexAttrib4N(Index, (int*)Ptr);
                            break;
                    }
                }
                else if (Attrib.Type == GalVertexAttribType.Uint)
                {
                    switch (Attrib.Size)
                    {
                        case GalVertexAttribSize._8:
                        case GalVertexAttribSize._8_8:
                        case GalVertexAttribSize._8_8_8:
                        case GalVertexAttribSize._8_8_8_8:
                            GL.VertexAttribI4(Index, Ptr);
                            break;

                        case GalVertexAttribSize._16:
                        case GalVertexAttribSize._16_16:
                        case GalVertexAttribSize._16_16_16:
                        case GalVertexAttribSize._16_16_16_16:
                            GL.VertexAttribI4(Index, (ushort*)Ptr);
                            break;

                        case GalVertexAttribSize._32:
                        case GalVertexAttribSize._32_32:
                        case GalVertexAttribSize._32_32_32:
                        case GalVertexAttribSize._32_32_32_32:
                            GL.VertexAttribI4(Index, (uint*)Ptr);
                            break;
                    }
                }
                else if (Attrib.Type == GalVertexAttribType.Sint)
                {
                    switch (Attrib.Size)
                    {
                        case GalVertexAttribSize._8:
                        case GalVertexAttribSize._8_8:
                        case GalVertexAttribSize._8_8_8:
                        case GalVertexAttribSize._8_8_8_8:
                            GL.VertexAttribI4(Index, (sbyte*)Ptr);
                            break;

                        case GalVertexAttribSize._16:
                        case GalVertexAttribSize._16_16:
                        case GalVertexAttribSize._16_16_16:
                        case GalVertexAttribSize._16_16_16_16:
                            GL.VertexAttribI4(Index, (short*)Ptr);
                            break;

                        case GalVertexAttribSize._32:
                        case GalVertexAttribSize._32_32:
                        case GalVertexAttribSize._32_32_32:
                        case GalVertexAttribSize._32_32_32_32:
                            GL.VertexAttribI4(Index, (int*)Ptr);
                            break;
                    }
                }
                else if (Attrib.Type == GalVertexAttribType.Float)
                {
                    switch (Attrib.Size)
                    {
                        case GalVertexAttribSize._32:
                        case GalVertexAttribSize._32_32:
                        case GalVertexAttribSize._32_32_32:
                        case GalVertexAttribSize._32_32_32_32:
                            GL.VertexAttrib4(Index, (float*)Ptr);
                            break;

                        default: ThrowUnsupportedAttrib(Attrib); break;
                    }
                }
            }
        }

        private static void ThrowUnsupportedAttrib(GalVertexAttrib Attrib)
        {
            throw new NotImplementedException("Unsupported size \"" + Attrib.Size + "\" on type \"" + Attrib.Type + "\"!");
        }

        public void CreateVbo(long Key, int DataSize, IntPtr HostAddress)
        {
            GetVbo(Key, DataSize).SetData(DataSize, HostAddress);
        }

        public void CreateVbo(long Key, byte[] Data)
        {
            GetVbo(Key, Data.Length).SetData(Data);
        }

        public void CreateIbo(long key, IntPtr hostAddress, int size, long vertexCount)
        {
            GetIbo(key, size, vertexCount).SetData(size, hostAddress);
        }

        public void CreateIbo(long key, byte[] buffer, long vertexCount)
        {
            GetIbo(key, buffer.Length, vertexCount).SetData(buffer);
        }

        public void SetIndexArray(int Size, GalIndexFormat Format)
        {
            IndexBuffer.Type = OGLEnumConverter.GetDrawElementsType(Format);

            IndexBuffer.Count = Size >> (int)Format;

            IndexBuffer.ElemSizeLog2 = (int)Format;
        }

        public void DrawArrays(int First, int Count, GalPrimitiveType PrimType)
        {
            if (Count == 0)
            {
                return;
            }

            if (PrimType == GalPrimitiveType.Quads)
            {
                for (int Offset = 0; Offset < Count; Offset += 4)
                {
                    GL.DrawArrays(PrimitiveType.TriangleFan, First + Offset, 4);
                }
            }
            else if (PrimType == GalPrimitiveType.QuadStrip)
            {
                GL.DrawArrays(PrimitiveType.TriangleFan, First, 4);

                for (int Offset = 2; Offset < Count; Offset += 2)
                {
                    GL.DrawArrays(PrimitiveType.TriangleFan, First + Offset, 4);
                }
            }
            else
            {
                GL.DrawArrays(OGLEnumConverter.GetPrimitiveType(PrimType), First, Count);
            }
        }

        public void DrawElements(long IboKey, int First, int VertexBase, GalPrimitiveType PrimType)
        {
            if (!IboCache.TryGetValue(IboKey, out CachedIbo Ibo))
            {
                return;
            }

            PrimitiveType Mode = OGLEnumConverter.GetPrimitiveType(PrimType);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, Ibo.Buffer.Handle);

            First <<= IndexBuffer.ElemSizeLog2;

            if (VertexBase != 0)
            {
                IntPtr Indices = new IntPtr(First);

                GL.DrawElementsBaseVertex(Mode, IndexBuffer.Count, IndexBuffer.Type, Indices, VertexBase);
            }
            else
            {
                GL.DrawElements(Mode, IndexBuffer.Count, IndexBuffer.Type, First);
            }
        }

        public bool TryGetVbo(long VboKey, out int VboHandle)
        {
            if (VboCache.TryGetValue(VboKey, out OGLStreamBuffer Vbo))
            {
                VboHandle = Vbo.Handle;

                return true;
            }

            VboHandle = 0;

            return false;
        }

        private OGLStreamBuffer GetVbo(long Key, long Size)
        {
            if (!VboCache.TryReuseValue(Key, Size, out OGLStreamBuffer Buffer))
            {
                Buffer = new OGLStreamBuffer(BufferTarget.ArrayBuffer, Size);

                VboCache.AddOrUpdate(Key, Size, Buffer, Size);
            }

            return Buffer;
        }

        private OGLStreamBuffer GetIbo(long Key, long Size, long VertexCount)
        {
            if (!IboCache.TryReuseValue(Key, Size, out CachedIbo Ibo))
            {
                OGLStreamBuffer Buffer = new OGLStreamBuffer(BufferTarget.ElementArrayBuffer, Size);

                IboCache.AddOrUpdate(Key, Size, Ibo = new CachedIbo(Buffer), Size);
            }

            Ibo.VertexCount = VertexCount;

            return Ibo.Buffer;
        }
    }
}