using Ryujinx.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// On-disk shader cache storage for guest code.
    /// </summary>
    class DiskCacheGuestStorage
    {
        private const uint TocMagic = (byte)'T' | ((byte)'O' << 8) | ((byte)'C' << 16) | ((byte)'G' << 24);

        private const ushort VersionMajor = 1;
        private const ushort VersionMinor = 0;
        private const uint VersionPacked = ((uint)VersionMajor << 16) | VersionMinor;

        private const string TocFileName = "guest.toc";
        private const string DataFileName = "guest.data";

        private readonly string _basePath;

        private struct TocHeader
        {
            public uint Magic;
            public uint Version;
            public uint Padding;
            public uint ModificationsCount;
            public ulong Reserved;
            public ulong Reversed2;
        }

        private struct TocEntry
        {
            public uint Offset;
            public uint CodeSize;
            public uint Cb1DataSize;
            public uint Hash;
        }

        private struct TocMemoryEntry
        {
            public readonly uint Offset;
            public readonly uint CodeSize;
            public readonly uint Cb1DataSize;
            public readonly int Index;

            public TocMemoryEntry(uint offset, uint codeSize, uint cb1DataSize, int index)
            {
                Offset = offset;
                CodeSize = codeSize;
                Cb1DataSize = cb1DataSize;
                Index = index;
            }
        }

        private Dictionary<uint, List<TocMemoryEntry>> _toc;
        private uint _tocModificationsCount;

        private (byte[], byte[])[] _cache;

        public DiskCacheGuestStorage(string basePath)
        {
            _basePath = basePath;
        }

        public bool TocFileExists()
        {
            return File.Exists(Path.Combine(_basePath, TocFileName));
        }

        public bool DataFileExists()
        {
            return File.Exists(Path.Combine(_basePath, DataFileName));
        }

        public Stream OpenTocFileStream()
        {
            return new FileStream(Path.Combine(_basePath, TocFileName), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream OpenDataFileStream()
        {
            return new FileStream(Path.Combine(_basePath, DataFileName), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public (byte[], byte[]) LoadShader(Stream tocFileStream, Stream dataFileStream, int index)
        {
            if (_cache == null || index >= _cache.Length)
            {
                _cache = new (byte[], byte[])[Math.Max(index + 1, GetShadersCountFromLength(tocFileStream.Length))];
            }

            (byte[] guestCode, byte[] cb1Data) = _cache[index];

            if (guestCode == null || cb1Data == null)
            {
                BinarySerialization tocReader = new BinarySerialization(tocFileStream);
                tocFileStream.Seek(Unsafe.SizeOf<TocHeader>() + index * Unsafe.SizeOf<TocEntry>(), SeekOrigin.Begin);

                TocEntry entry = new TocEntry();
                tocReader.TryRead(ref entry);

                guestCode = new byte[entry.CodeSize];
                cb1Data = new byte[entry.Cb1DataSize];

                dataFileStream.Seek((long)entry.Offset, SeekOrigin.Begin);
                dataFileStream.Read(cb1Data);
                BinarySerialization.ReadCompressed(dataFileStream, guestCode);

                _cache[index] = (guestCode, cb1Data);
            }

            return (guestCode, cb1Data);
        }

        public void ClearMemoryCache()
        {
            _cache = null;
        }

        private static int GetShadersCountFromLength(long length)
        {
            return (int)((length - Unsafe.SizeOf<TocHeader>()) / Unsafe.SizeOf<TocEntry>());
        }

        public int AddShader(ReadOnlySpan<byte> data, ReadOnlySpan<byte> cb1Data)
        {
            string tocFilePath = Path.Combine(_basePath, TocFileName);
            string dataFilePath = Path.Combine(_basePath, DataFileName);

            using var tocFileStream = new FileStream(tocFilePath, FileMode.OpenOrCreate);
            using var dataFileStream = new FileStream(dataFilePath, FileMode.OpenOrCreate);

            TocHeader header = new TocHeader();

            LoadOrCreateToc(tocFileStream, ref header);

            uint hash = CalcHash(data, cb1Data);

            if (_toc.TryGetValue(hash, out var list))
            {
                foreach (var entry in list)
                {
                    if (data.Length != entry.CodeSize || cb1Data.Length != entry.Cb1DataSize)
                    {
                        continue;
                    }

                    dataFileStream.Seek((long)entry.Offset, SeekOrigin.Begin);
                    byte[] cachedCode = new byte[entry.CodeSize];
                    byte[] cachedCb1Data = new byte[entry.Cb1DataSize];
                    dataFileStream.Read(cachedCb1Data);
                    BinarySerialization.ReadCompressed(dataFileStream, cachedCode);

                    if (data.SequenceEqual(cachedCode) && cb1Data.SequenceEqual(cachedCb1Data))
                    {
                        return entry.Index;
                    }
                }
            }

            return WriteNewEntry(tocFileStream, dataFileStream, ref header, data, cb1Data, hash);
        }

        private void LoadOrCreateToc(Stream tocFileStream, ref TocHeader header)
        {
            BinarySerialization reader = new BinarySerialization(tocFileStream);

            if (!reader.TryRead(ref header) || header.Magic != TocMagic || header.Version != VersionPacked)
            {
                CreateToc(tocFileStream, ref header);
            }

            if (_toc == null || header.ModificationsCount != _tocModificationsCount)
            {
                if (!LoadTocEntries(tocFileStream, ref reader))
                {
                    CreateToc(tocFileStream, ref header);
                }

                _tocModificationsCount = header.ModificationsCount;
            }
        }

        private void CreateToc(Stream tocFileStream, ref TocHeader header)
        {
            BinarySerialization writer = new BinarySerialization(tocFileStream);

            header.Magic = TocMagic;
            header.Version = VersionPacked;
            header.Padding = 0;
            header.ModificationsCount = 0;
            header.Reserved = 0;
            header.Reversed2 = 0;

            if (tocFileStream.Length > 0)
            {
                tocFileStream.Seek(0, SeekOrigin.Begin);
                tocFileStream.SetLength(0);
            }

            writer.Write(ref header);
        }

        private bool LoadTocEntries(Stream tocFileStream, ref BinarySerialization reader)
        {
            _toc = new Dictionary<uint, List<TocMemoryEntry>>();

            TocEntry entry = new TocEntry();
            int index = 0;

            while (tocFileStream.Position < tocFileStream.Length)
            {
                if (!reader.TryRead(ref entry))
                {
                    return false;
                }

                AddTocMemoryEntry(entry.Offset, entry.CodeSize, entry.Cb1DataSize, entry.Hash, index++);
            }

            return true;
        }

        private int WriteNewEntry(
            Stream tocFileStream,
            Stream dataFileStream,
            ref TocHeader header,
            ReadOnlySpan<byte> data,
            ReadOnlySpan<byte> cb1Data,
            uint hash)
        {
            BinarySerialization tocWriter = new BinarySerialization(tocFileStream);

            dataFileStream.Seek(0, SeekOrigin.End);
            uint dataOffset = checked((uint)dataFileStream.Position);
            uint codeSize = (uint)data.Length;
            uint cb1DataSize = (uint)cb1Data.Length;
            dataFileStream.Write(cb1Data);
            BinarySerialization.WriteCompressed(dataFileStream, data, DiskCacheCommon.GetCompressionAlgorithm());

            _tocModificationsCount = ++header.ModificationsCount;
            tocFileStream.Seek(0, SeekOrigin.Begin);
            tocWriter.Write(ref header);

            TocEntry entry = new TocEntry()
            {
                Offset = dataOffset,
                CodeSize = codeSize,
                Cb1DataSize = cb1DataSize,
                Hash = hash
            };

            tocFileStream.Seek(0, SeekOrigin.End);
            int index = (int)((tocFileStream.Position - Unsafe.SizeOf<TocHeader>()) / Unsafe.SizeOf<TocEntry>());

            tocWriter.Write(ref entry);

            AddTocMemoryEntry(dataOffset, codeSize, cb1DataSize, hash, index);

            return index;
        }

        private void AddTocMemoryEntry(uint dataOffset, uint codeSize, uint cb1DataSize, uint hash, int index)
        {
            if (!_toc.TryGetValue(hash, out var list))
            {
                _toc.Add(hash, list = new List<TocMemoryEntry>());
            }

            list.Add(new TocMemoryEntry(dataOffset, codeSize, cb1DataSize, index));
        }

        private static uint CalcHash(ReadOnlySpan<byte> data, ReadOnlySpan<byte> data2)
        {
            return CalcHash(data2) * 23 ^ CalcHash(data);
        }

        private static uint CalcHash(ReadOnlySpan<byte> data)
        {
            return (uint)XXHash128.ComputeHash(data).Low;
        }
    }
}