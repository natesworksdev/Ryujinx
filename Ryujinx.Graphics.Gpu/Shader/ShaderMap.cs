using System;
using System.Collections;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Shader
{
    class ShaderMap<T> : IEnumerable<T>
    {
        private struct ShaderEntry
        {
            public T Shader { get; }

            private readonly byte[][] _code;

            public ShaderEntry(T shader, byte[][] code)
            {
                Shader = shader;
                _code  = code;
            }

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

        public ShaderMap()
        {
            _shaders = new Dictionary<int, List<ShaderEntry>>();
        }

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

        public void Add(int hash, T shader, ShaderPack pack)
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

            list.Add(new ShaderEntry(shader, code));
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
