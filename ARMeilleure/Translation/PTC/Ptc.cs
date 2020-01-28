using ARMeilleure.CodeGen;
using ARMeilleure.CodeGen.X86;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Timers;

namespace ARMeilleure.Translation.PTC
{
    public static class Ptc
    {
        private const string HeaderMagic = "PTChd";

        private const int InternalVersion = 0; //! To be incremented manually for each change to the ARMeilleure project.

        private const string BaseDir = "Ryujinx";

        private const string TitleIdTextDefault = "0000000000000000";
        private const string DisplayVersionDefault = "0";

        private const int SaveInterval = 30; // Seconds.

        internal const int MinCodeLengthToSave = 0x180; // Bytes.

        internal const int PageTableIndex = -1; // Must be a negative value.

        private const CompressionLevel SaveCompressionLevel = CompressionLevel.Fastest;

        private static readonly MemoryStream _infosStream;
        private static readonly MemoryStream _codesStream;
        private static readonly MemoryStream _relocsStream;

        private static readonly BinaryWriter _infosWriter;

        private static readonly Timer _timer;

        private static readonly System.Threading.ManualResetEvent _waitEvent;

        private static readonly string _basePath;

        private static readonly object _locker;

        private static bool _disposed;

        public static string TitleIdText { get; private set; }
        public static string DisplayVersion { get; private set; }

        public static bool Enabled { get; private set; }
        public static bool ReadOnlyMode { get; private set; }

        public static string CachePath { get; private set; }

        static Ptc()
        {
            _infosStream = new MemoryStream();
            _codesStream = new MemoryStream();
            _relocsStream = new MemoryStream();

            _infosWriter = new BinaryWriter(_infosStream, EncodingCache.UTF8NoBOM, true);

            _timer = new Timer((double)SaveInterval * 1000d);
            _timer.Elapsed += MergeAndSave;

            _waitEvent = new System.Threading.ManualResetEvent(true);

            _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BaseDir);

            _locker = new object();

            _disposed = false;

            TitleIdText = TitleIdTextDefault;
            DisplayVersion = DisplayVersionDefault;

            Enabled = false;
            ReadOnlyMode = true;

            CachePath = string.Empty;
        }

        public static void InitAndStart(string titleIdText, string displayVersion, bool enabled, bool readOnlyMode)
        {
            _waitEvent.WaitOne();

            _infosStream.SetLength(0L);
            _codesStream.SetLength(0L);
            _relocsStream.SetLength(0L);

            if (String.IsNullOrEmpty(titleIdText) || titleIdText == TitleIdTextDefault)
            {
                TitleIdText = TitleIdTextDefault;
                DisplayVersion = DisplayVersionDefault;

                Enabled = false;
                ReadOnlyMode = true;

                CachePath = string.Empty;

                return;
            }

            TitleIdText = titleIdText;
            DisplayVersion = !String.IsNullOrEmpty(displayVersion) ? displayVersion : DisplayVersionDefault;

            Enabled = enabled;
            ReadOnlyMode = readOnlyMode;

            string workPath = Path.Combine(_basePath, "games", TitleIdText, "cache", "cpu");

            if (enabled && !Directory.Exists(workPath))
            {
                Directory.CreateDirectory(workPath);
            }

            CachePath = Path.Combine(workPath, DisplayVersion);

            if (enabled)
            {
                LoadAndSplit();

                if (!readOnlyMode)
                {
                    _timer.Enabled = true;
                }
            }
        }

        public static void Stop()
        {
            if (!_disposed)
            {
                _timer.Enabled = false;
            }
        }

        private static void LoadAndSplit()
        {
            FileInfo cacheInfo = new FileInfo(CachePath);

            if (cacheInfo.Exists && cacheInfo.Length != 0L)
            {
                using (FileStream compressedCacheStream = new FileStream(CachePath, FileMode.Open))
                using (DeflateStream deflateStream = new DeflateStream(compressedCacheStream, CompressionMode.Decompress, true))
                using (MemoryStream cacheStream = new MemoryStream())
                using (MD5 md5 = MD5.Create())
                {
                    try
                    {
                        int hashSize = md5.HashSize / 8;

                        deflateStream.CopyTo(cacheStream);

                        cacheStream.Seek(0L, SeekOrigin.Begin);

                        byte[] currentHash = new byte[hashSize];
                        cacheStream.Read(currentHash, 0, hashSize);

                        byte[] expectedHash = md5.ComputeHash(cacheStream);

                        if (!CompareHash(currentHash, expectedHash))
                        {
                            InvalidateCompressedCacheStream(compressedCacheStream);

                            return;
                        }

                        cacheStream.Seek((long)hashSize, SeekOrigin.Begin);

                        Header header = ReadHeader(cacheStream);

                        if (header.Magic != HeaderMagic)
                        {
                            InvalidateCompressedCacheStream(compressedCacheStream);

                            return;
                        }

                        if (header.CacheFileVersion != InternalVersion)
                        {
                            InvalidateCompressedCacheStream(compressedCacheStream);

                            return;
                        }

                        if (header.FeatureInfo != HardwareCapabilities.FeatureInfo)
                        {
                            InvalidateCompressedCacheStream(compressedCacheStream);

                            return;
                        }

                        if (header.InfosLen % InfoEntry.Size != 0)
                        {
                            InvalidateCompressedCacheStream(compressedCacheStream);

                            return;
                        }

                        byte[] infosBuf = new byte[header.InfosLen];
                        byte[] codesBuf = new byte[header.CodesLen];
                        byte[] relocsBuf = new byte[header.RelocsLen];

                        cacheStream.Read(infosBuf, 0, header.InfosLen);
                        cacheStream.Read(codesBuf, 0, header.CodesLen);
                        cacheStream.Read(relocsBuf, 0, header.RelocsLen);

                        if (cacheStream.Position != cacheStream.Length)
                        {
                            InvalidateCompressedCacheStream(compressedCacheStream);

                            return;
                        }

                        try
                        {
                            _infosStream.Write(infosBuf, 0, header.InfosLen);
                            _codesStream.Write(codesBuf, 0, header.CodesLen);
                            _relocsStream.Write(relocsBuf, 0, header.RelocsLen);
                        }
                        catch
                        {
                            _infosStream.SetLength(0L);
                            _codesStream.SetLength(0L);
                            _relocsStream.SetLength(0L);
                        }
                    }
                    catch
                    {
                        InvalidateCompressedCacheStream(compressedCacheStream);
                    }
                }
            }
        }

        private static bool CompareHash(ReadOnlySpan<byte> currentHash, ReadOnlySpan<byte> expectedHash)
        {
            return currentHash.SequenceEqual(expectedHash);
        }

        private static Header ReadHeader(MemoryStream cacheStream)
        {
            using (BinaryReader headerReader = new BinaryReader(cacheStream, EncodingCache.UTF8NoBOM, true))
            {
                Header header = new Header();

                header.Magic = headerReader.ReadString();

                header.CacheFileVersion = headerReader.ReadInt32();
                header.FeatureInfo = headerReader.ReadUInt64();

                header.InfosLen = headerReader.ReadInt32();
                header.CodesLen = headerReader.ReadInt32();
                header.RelocsLen = headerReader.ReadInt32();

                return header;
            }
        }

        private static void InvalidateCompressedCacheStream(FileStream compressedCacheStream)
        {
            if (!ReadOnlyMode)
            {
                compressedCacheStream.SetLength(0L);
            }
        }

        private static void MergeAndSave(Object source, ElapsedEventArgs e)
        {
            _waitEvent.Reset();

            using (MemoryStream cacheStream = new MemoryStream())
            using (MD5 md5 = MD5.Create())
            {
                int hashSize = md5.HashSize / 8;

                cacheStream.Seek((long)hashSize, SeekOrigin.Begin);

                lock (_locker) // Read.
                {
                    WriteHeader(cacheStream);

                    _infosStream.WriteTo(cacheStream);
                    _codesStream.WriteTo(cacheStream);
                    _relocsStream.WriteTo(cacheStream);
                }

                cacheStream.Seek((long)hashSize, SeekOrigin.Begin);
                byte[] hash = md5.ComputeHash(cacheStream);

                cacheStream.Seek(0L, SeekOrigin.Begin);
                cacheStream.Write(hash, 0, hashSize);

                using (FileStream compressedCacheStream = new FileStream(CachePath, FileMode.OpenOrCreate))
                using (DeflateStream deflateStream = new DeflateStream(compressedCacheStream, SaveCompressionLevel, true))
                {
                    try
                    {
                        cacheStream.WriteTo(deflateStream);
                    }
                    catch
                    {
                        compressedCacheStream.Position = 0L;
                    }

                    if (compressedCacheStream.Position < compressedCacheStream.Length)
                    {
                        compressedCacheStream.SetLength(compressedCacheStream.Position);
                    }
                }
            }

            _waitEvent.Set();
        }

        private static void WriteHeader(MemoryStream cacheStream)
        {
            using (BinaryWriter headerWriter = new BinaryWriter(cacheStream, EncodingCache.UTF8NoBOM, true))
            {
                headerWriter.Write((string)HeaderMagic); // Header.Magic

                headerWriter.Write((int)InternalVersion); // Header.CacheFileVersion
                headerWriter.Write((ulong)HardwareCapabilities.FeatureInfo); // Header.FeatureInfo

                headerWriter.Write((int)_infosStream.Length); // Header.InfosLen
                headerWriter.Write((int)_codesStream.Length); // Header.CodesLen
                headerWriter.Write((int)_relocsStream.Length); // Header.RelocsLen
            }
        }

        internal static void FullTranslate(ConcurrentDictionary<ulong, TranslatedFunction> funcsHighCq, IntPtr pageTable)
        {
            if ((int)_infosStream.Length != 0 &&
                (int)_codesStream.Length != 0 &&
                (int)_relocsStream.Length != 0)
            {
                _infosStream.Seek(0L, SeekOrigin.Begin);
                _codesStream.Seek(0L, SeekOrigin.Begin);
                _relocsStream.Seek(0L, SeekOrigin.Begin);

                using (BinaryReader infosReader = new BinaryReader(_infosStream, EncodingCache.UTF8NoBOM, true))
                using (BinaryReader codesReader = new BinaryReader(_codesStream, EncodingCache.UTF8NoBOM, true))
                using (BinaryReader relocsReader = new BinaryReader(_relocsStream, EncodingCache.UTF8NoBOM, true))
                {
                    for (int i = 0; i < (int)_infosStream.Length / InfoEntry.Size; i++) // infosEntriesCount
                    {
                        InfoEntry infoEntry = ReadInfo(infosReader);

                        byte[] code = ReadCode(codesReader, infoEntry.CodeLen);

                        if (infoEntry.RelocEntriesCount != 0)
                        {
                            PatchCode(code, GetRelocEntries(relocsReader, infoEntry.RelocEntriesCount), pageTable);
                        }

                        bool isAddressUnique = funcsHighCq.TryAdd((ulong)infoEntry.Address, FastTranslate(code));

                        Debug.Assert(isAddressUnique, $"The address 0x{(ulong)infoEntry.Address:X16} is not unique.");
                    }
                }

                if (_infosStream.Position < _infosStream.Length ||
                    _codesStream.Position < _codesStream.Length ||
                    _relocsStream.Position < _relocsStream.Length)
                {
                    throw new Exception("Could not reach the end of one or more memory streams.");
                }
            }
        }

        private static InfoEntry ReadInfo(BinaryReader infosReader)
        {
            InfoEntry infoEntry = new InfoEntry();

            infoEntry.Address = infosReader.ReadInt64();
            infoEntry.CodeLen = infosReader.ReadInt32();
            infoEntry.RelocEntriesCount = infosReader.ReadInt32();

            return infoEntry;
        }

        private static byte[] ReadCode(BinaryReader codesReader, int codeLen)
        {
            byte[] codeBuf = new byte[codeLen];

            codesReader.Read(codeBuf, 0, codeLen);

            return codeBuf;
        }

        private static RelocEntry[] GetRelocEntries(BinaryReader relocsReader, int relocEntriesCount)
        {
            RelocEntry[] relocEntries = new RelocEntry[relocEntriesCount];

            for (int i = 0; i < relocEntriesCount; i++)
            {
                int position = relocsReader.ReadInt32();
                int index = relocsReader.ReadInt32();

                relocEntries[i] = new RelocEntry(position, index);
            }

            return relocEntries;
        }

        private static void PatchCode(Span<byte> code, RelocEntry[] relocEntries, IntPtr pageTable)
        {
            foreach (RelocEntry relocEntry in relocEntries)
            {
                ulong imm;

                if (relocEntry.Index == PageTableIndex)
                {
                    imm = (ulong)pageTable.ToInt64();
                }
                else if (Delegates.TryGetDelegateFuncPtrByIndex(relocEntry.Index, out IntPtr funcPtr))
                {
                    imm = (ulong)funcPtr.ToInt64();
                }
                else
                {
                    throw new Exception($"Unexpected reloc entry {relocEntry}.");
                }

                BinaryPrimitives.WriteUInt64LittleEndian(code.Slice(relocEntry.Position, 8), imm);
            }
        }

        private static TranslatedFunction FastTranslate(byte[] code)
        {
            CompiledFunction cFunc = new CompiledFunction(code);

            IntPtr codePtr = JitCache.Map(cFunc);

            GuestFunction gFunc = Marshal.GetDelegateForFunctionPointer<GuestFunction>(codePtr);

            TranslatedFunction tFunc = new TranslatedFunction(gFunc, rejit : false);

            return tFunc;
        }

        internal static void WriteInfoCodeReloc(long address, PtcInfo ptcInfo)
        {
            lock (_locker) // Write.
            {
                // WriteInfo.
                _infosWriter.Write((long)address); // InfoEntry.Address
                _infosWriter.Write((int)ptcInfo.CodeStream.Length); // InfoEntry.CodeLen
                _infosWriter.Write((int)ptcInfo.RelocEntriesCount); // InfoEntry.RelocEntriesCount

                // WriteCode.
                ptcInfo.CodeStream.WriteTo(_codesStream);

                // WriteReloc.
                ptcInfo.RelocStream.WriteTo(_relocsStream);
            }
        }

        private struct Header
        {
            public string Magic;

            public int CacheFileVersion;
            public ulong FeatureInfo;

            public int InfosLen;
            public int CodesLen;
            public int RelocsLen;
        }

        private struct InfoEntry
        {
            public const int Size = 16; // Bytes.

            public long Address;
            public int CodeLen;
            public int RelocEntriesCount;
        }

        public static void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _timer.Elapsed -= MergeAndSave;
                _timer.Dispose();

                _waitEvent.WaitOne();
                _waitEvent.Dispose();

                _infosWriter.Dispose();

                _infosStream.Dispose();
                _codesStream.Dispose();
                _relocsStream.Dispose();
            }
        }
    }
}