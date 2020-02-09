using System;
using System.Collections;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Map of compiled host shader code, keyed by hash and guest code.
    /// </summary>
    /// <typeparam name="T">Type of the host shader code</typeparam>
    class ShaderMap<T> : IEnumerable<T>
    {
        private struct ShaderEntry
        {
            /// <summary>
            /// Host shader.
            /// </summary>
            public T Shader { get; }

            private readonly byte[][] _code;

            /// <summary>
            /// Creates a new shader entry on the shader map.
            /// </summary>
            /// <param name="shader">Host shader</param>
            /// <param name="code">Guest shader code for all active shaders</param>
            public ShaderEntry(T shader, byte[][] code)
            {
                Shader = shader;
                _code  = code;
            }

            /// <summary>
            /// Checks if the shader code on this entry is equal to the guest shader code in memory.
            /// </summary>
            /// <param name="pack">Pack of guest shader code</param>
            /// <returns>True if the code is equal, false otherwise</returns>
            public bool CodeEquals(ShaderPack pack)
            {
                if (pack.Count != _code.Length)
                {
                    return false;
                }

                for (int index = 0; index < pack.Count; index++)
                {
                    if (!pack[index].SequenceEqual(_code[index]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private readonly Dictionary<int, List<ShaderEntry>> _shaders;

        /// <summary>
        /// Creates a new instance of the shader map.
        /// </summary>
        public ShaderMap()
        {
            _shaders = new Dictionary<int, List<ShaderEntry>>();
        }

        /// <summary>
        /// Gets the host shader for the guest shader code,
        /// or the default value for the type if not found.
        /// </summary>
        /// <param name="pack">Pack with spans of guest shader code</param>
        /// <param name="hash">Calculated hash of all the shader code on the pack</param>
        /// <returns>Host shader, or the default value if not found</returns>
        public T Get(ShaderPack pack, out int hash)
        {
            Fnv1a hasher = new Fnv1a();

            hasher.Initialize();

            for (int index = 0; index < pack.Count; index++)
            {
                hasher.Add(pack[index]);
            }

            hash = hasher.Hash;

            if (_shaders.TryGetValue(hash, out List<ShaderEntry> list))
            {
                for (int index = 0; index < list.Count; index++)
                {
                    ShaderEntry entry = list[index];

                    if (entry.CodeEquals(pack))
                    {
                        return entry.Shader;
                    }
                }
            }

            return default;
        }

        /// <summary>
        /// Adds a new host shader to the shader map.
        /// </summary>
        /// <param name="hash">Hash of the shader code, returned from the <see cref="Get"/> method</param>
        /// <param name="shader">Host shader</param>
        /// <param name="pack">Pack with spans of guest shader code</param>
        /// <returns>Index to disambiguate the shader in case of hash collisions</returns>
        public int Add(int hash, T shader, ShaderPack pack)
        {
            if (!_shaders.TryGetValue(hash, out List<ShaderEntry> list))
            {
                list = new List<ShaderEntry>();

                _shaders.Add(hash, list);
            }

            byte[][] code = new byte[pack.Count][];

            for (int index = 0; index < pack.Count; index++)
            {
                code[index] = pack[index].ToArray();
            }

            int insertIndex = list.Count;

            list.Add(new ShaderEntry(shader, code));

            return insertIndex;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var list in _shaders.Values)
            {
                foreach (ShaderEntry entry in list)
                {
                    yield return entry.Shader;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
