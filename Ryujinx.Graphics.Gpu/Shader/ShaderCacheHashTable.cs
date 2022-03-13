using Ryujinx.Common.Cache;
using Ryujinx.Graphics.Gpu.Memory;
using Ryujinx.Graphics.Shader;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Shader
{
    struct CachedGraphicsGuestCode
    {
        public byte[] VertexACode;
        public byte[] VertexBCode;
        public byte[] TessControlCode;
        public byte[] TessEvaluationCode;
        public byte[] GeometryCode;
        public byte[] FragmentCode;

        public byte[] GetByIndex(int stageIndex)
        {
            return stageIndex switch
            {
                1 => TessControlCode,
                2 => TessEvaluationCode,
                3 => GeometryCode,
                4 => FragmentCode,
                _ => VertexBCode
            };
        }
    }

    class ShaderCacheHashTable
    {
        private struct IdCache
        {
            private PartitionedHashTable<int> _cache;
            private int _id;

            public void Initialize()
            {
                _cache = new PartitionedHashTable<int>();
                _id = 0;
            }

            public int Add(byte[] code)
            {
                int id = ++_id;
                int cachedId = _cache.GetOrAdd(code, id);
                if (cachedId != id)
                {
                    --_id;
                }

                return cachedId;
            }

            public bool TryFind(IDataAccessor dataAccessor, out int id, out byte[] data)
            {
                return _cache.TryFindItem(dataAccessor, out id, out data);
            }
        }

        private struct IdTable : IEquatable<IdTable>
        {
            public int VertexAId;
            public int VertexBId;
            public int TessControlId;
            public int TessEvaluationId;
            public int GeometryId;
            public int FragmentId;

            public override bool Equals(object obj)
            {
                return obj is IdTable other && Equals(other);
            }

            public bool Equals(IdTable other)
            {
                return other.VertexAId == VertexAId &&
                       other.VertexBId == VertexBId &&
                       other.TessControlId == TessControlId &&
                       other.TessEvaluationId == TessEvaluationId &&
                       other.GeometryId == GeometryId &&
                       other.FragmentId == FragmentId;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(VertexAId, VertexBId, TessControlId, TessEvaluationId, GeometryId, FragmentId);
            }
        }

        private IdCache _vertexACache;
        private IdCache _vertexBCache;
        private IdCache _tessControlCache;
        private IdCache _tessEvaluationCache;
        private IdCache _geometryCache;
        private IdCache _fragmentCache;

        private readonly Dictionary<IdTable, ShaderSpecializationList> _shaderPrograms;

        public ShaderCacheHashTable()
        {
            _vertexACache.Initialize();
            _vertexBCache.Initialize();
            _tessControlCache.Initialize();
            _tessEvaluationCache.Initialize();
            _geometryCache.Initialize();
            _fragmentCache.Initialize();

            _shaderPrograms = new Dictionary<IdTable, ShaderSpecializationList>();
        }

        public void Add(CachedShaderProgram program)
        {
            IdTable idTable = new IdTable();

            foreach (var shader in program.Shaders)
            {
                if (shader == null)
                {
                    continue;
                }

                if (shader.Info != null)
                {
                    switch (shader.Info.Stage)
                    {
                        case ShaderStage.Vertex:
                            idTable.VertexBId = _vertexBCache.Add(shader.Code);
                            break;
                        case ShaderStage.TessellationControl:
                            idTable.TessControlId = _tessControlCache.Add(shader.Code);
                            break;
                        case ShaderStage.TessellationEvaluation:
                            idTable.TessEvaluationId = _tessEvaluationCache.Add(shader.Code);
                            break;
                        case ShaderStage.Geometry:
                            idTable.GeometryId = _geometryCache.Add(shader.Code);
                            break;
                        case ShaderStage.Fragment:
                            idTable.FragmentId = _fragmentCache.Add(shader.Code);
                            break;
                    }
                }
                else
                {
                    idTable.VertexAId = _vertexACache.Add(shader.Code);
                }
            }

            // System.Console.WriteLine($"ids {idTable.VertexBId} {idTable.GeometryId} {idTable.FragmentId} total {_shaderPrograms.Count}");

            if (!_shaderPrograms.TryGetValue(idTable, out ShaderSpecializationList specList))
            {
                specList = new ShaderSpecializationList();
                _shaderPrograms.Add(idTable, specList);
            }

            specList.Add(program);
        }

        public bool TryFind(
            GpuChannel channel,
            GpuChannelPoolState poolState,
            ShaderAddresses addresses,
            out CachedShaderProgram program,
            out CachedGraphicsGuestCode guestCode)
        {
            var memoryManager = channel.MemoryManager;
            IdTable idTable = new IdTable();
            guestCode = new CachedGraphicsGuestCode();

            program = null;

            bool found = TryGetId(_vertexACache, memoryManager, addresses.VertexA, out idTable.VertexAId, out guestCode.VertexACode);
            found &= TryGetId(_vertexBCache, memoryManager, addresses.VertexB, out idTable.VertexBId, out guestCode.VertexBCode);
            found &= TryGetId(_tessControlCache, memoryManager, addresses.TessControl, out idTable.TessControlId, out guestCode.TessControlCode);
            found &= TryGetId(_tessEvaluationCache, memoryManager, addresses.TessEvaluation, out idTable.TessEvaluationId, out guestCode.TessEvaluationCode);
            found &= TryGetId(_geometryCache, memoryManager, addresses.Geometry, out idTable.GeometryId, out guestCode.GeometryCode);
            found &= TryGetId(_fragmentCache, memoryManager, addresses.Fragment, out idTable.FragmentId, out guestCode.FragmentCode);

            if (found && _shaderPrograms.TryGetValue(idTable, out ShaderSpecializationList specList))
            {
                return specList.TryFindForGraphics(channel, poolState, out program);
            }

            return false;
        }

        private static bool TryGetId(IdCache idCache, MemoryManager memoryManager, ulong baseAddress, out int id, out byte[] data)
        {
            if (baseAddress == 0)
            {
                id = 0;
                data = null;
                return true;
            }

            ShaderCodeAccessor codeAccessor = new ShaderCodeAccessor(memoryManager, baseAddress);
            return idCache.TryFind(codeAccessor, out id, out data);
        }
    }
}