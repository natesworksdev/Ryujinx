using ARMeilleure.CodeGen;
using ARMeilleure.Memory;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Timers;

namespace ARMeilleure.Translation.AOT
{
    public static class Aot
    {
        private const string WorkDir = "RyuAot";

        private const int SaveInterval = 30; // Seconds.

        internal const int MinCodeLengthToSave = 0x180; // Bytes.

        private const CompressionLevel SaveCompressionLevel = CompressionLevel.Fastest;

        private static readonly MemoryStream _infosStream;
        private static readonly MemoryStream _codesStream;
        private static readonly MemoryStream _relocsStream;

        private static readonly BinaryWriter _infosWriter;

        private static readonly Timer _timer;

        private static readonly object _locker;

        private static bool _disposed;

        public static string WorkPath { get; }
        public static string TitleId  { get; private set; }

        public static bool Enabled      { get; private set; }
        public static bool ReadOnlyMode { get; private set; }

        static Aot()
        {
            string basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            Debug.Assert(basePath != String.Empty);

            WorkPath = Path.Combine(basePath, WorkDir);

            if (!Directory.Exists(WorkPath))
            {
                Directory.CreateDirectory(WorkPath);
            }

            TitleId = String.Empty;

            Enabled      = false;
            ReadOnlyMode = true;

            _infosStream  = new MemoryStream();
            _codesStream  = new MemoryStream();
            _relocsStream = new MemoryStream();

            _infosWriter = new BinaryWriter(_infosStream, EncodingCache.UTF8NoBOM, true);

            _timer = new Timer((double)SaveInterval * 1000d);
            _timer.Elapsed += MergeAndSave;

            _locker = new object();

            _disposed = false;
        }

        public static void Init(string titleId, bool enabled, bool readOnlyMode = false)
        {
            if (!String.IsNullOrEmpty(titleId))
            {
                TitleId = titleId.ToUpper();
            }

            Enabled      = enabled;
            ReadOnlyMode = readOnlyMode;

            if (enabled)
            {
                LoadAndSplit();

                if (!readOnlyMode)
                {
                    _timer.Enabled = true;
                }
            }
        }

        private static void LoadAndSplit()
        {
            string cachePath = Path.Combine(WorkPath, TitleId);

            FileInfo cacheInfo = new FileInfo(cachePath);

            if (cacheInfo.Exists && cacheInfo.Length != 0L)
            {
                using (FileStream compressedCacheStream = new FileStream(cachePath, FileMode.Open))
                {
                    DeflateStream deflateStream = new DeflateStream(compressedCacheStream, CompressionMode.Decompress, true);

                    MemoryStream cacheStream = new MemoryStream();

                    MD5 md5 = MD5.Create();

                    int hashSize = md5.HashSize / 8;

                    try
                    {
                        using (deflateStream)
                        {
                            deflateStream.CopyTo(cacheStream);
                        }

                        cacheStream.Seek(0L, SeekOrigin.Begin);

                        byte[] currentHash = new byte[hashSize];
                        cacheStream.Read(currentHash, 0, hashSize);

                        byte[] expectedHash = md5.ComputeHash(cacheStream);

                        if (CompareHash(currentHash, expectedHash))
                        {
                            cacheStream.Seek((long)hashSize, SeekOrigin.Begin);

                            ReadHeader(cacheStream, out int infosLen, out int codesLen, out int relocsLen);

                            if (infosLen % InfoEntry.Size == 0)
                            {
                                byte[] infosBuf  = new byte[infosLen];
                                byte[] codesBuf  = new byte[codesLen];
                                byte[] relocsBuf = new byte[relocsLen];

                                cacheStream.Read(infosBuf,  0, infosLen);
                                cacheStream.Read(codesBuf,  0, codesLen);
                                cacheStream.Read(relocsBuf, 0, relocsLen);

                                if (cacheStream.Position == cacheStream.Length)
                                {
                                    try
                                    {
                                        _infosStream. Write(infosBuf,  0, infosLen);
                                        _codesStream. Write(codesBuf,  0, codesLen);
                                        _relocsStream.Write(relocsBuf, 0, relocsLen);
                                    }
                                    catch
                                    {
                                        _infosStream. SetLength(0L);
                                        _codesStream. SetLength(0L);
                                        _relocsStream.SetLength(0L);
                                    }
                                }
                                else
                                {
                                    InvalidateCompressedCacheStream(compressedCacheStream);
                                }
                            }
                            else
                            {
                                InvalidateCompressedCacheStream(compressedCacheStream);
                            }
                        }
                        else
                        {
                            InvalidateCompressedCacheStream(compressedCacheStream);
                        }
                    }
                    catch
                    {
                        InvalidateCompressedCacheStream(compressedCacheStream);
                    }

                    md5.Dispose();

                    cacheStream.Dispose();
                }
            }
        }

        private static bool CompareHash(byte[] currentHash, byte[] expectedHash)
        {
            if (currentHash.Length != expectedHash.Length)
            {
                return false;
            }

            for (int i = 0; i < currentHash.Length; i++)
            {
                if (currentHash[i] != expectedHash[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void ReadHeader(
            MemoryStream cacheStream,
            out int      infosLen,
            out int      codesLen,
            out int      relocsLen)
        {
            BinaryReader headerReader = new BinaryReader(cacheStream, EncodingCache.UTF8NoBOM, true);

            infosLen  = headerReader.ReadInt32();
            codesLen  = headerReader.ReadInt32();
            relocsLen = headerReader.ReadInt32();

            headerReader.Dispose();
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
            string cachePath = Path.Combine(WorkPath, TitleId);

            using (FileStream compressedCacheStream = new FileStream(cachePath, FileMode.OpenOrCreate))
            {
                MemoryStream cacheStream = new MemoryStream();

                MD5 md5 = MD5.Create();

                int hashSize = md5.HashSize / 8;

                cacheStream.Seek((long)hashSize, SeekOrigin.Begin);

                bool disposed = true;

                lock (_locker) // Read.
                {
                    if (!_disposed)
                    {
                        WriteHeader(cacheStream);

                        _infosStream. WriteTo(cacheStream);
                        _codesStream. WriteTo(cacheStream);
                        _relocsStream.WriteTo(cacheStream);

                        disposed = false;
                    }
                }

                if (!disposed)
                {
                    cacheStream.Seek((long)hashSize, SeekOrigin.Begin);
                    byte[] hash = md5.ComputeHash(cacheStream);

                    cacheStream.Seek(0L, SeekOrigin.Begin);
                    cacheStream.Write(hash, 0, hashSize);

                    using (DeflateStream deflateStream = new DeflateStream(compressedCacheStream, SaveCompressionLevel, true))
                    {
                        cacheStream.WriteTo(deflateStream);
                    }

                    if (compressedCacheStream.Length > compressedCacheStream.Position)
                    {
                        compressedCacheStream.SetLength(compressedCacheStream.Position);
                    }
                }

                md5.Dispose();

                cacheStream.Dispose();
            }
        }

        private static void WriteHeader(MemoryStream cacheStream)
        {
            BinaryWriter headerWriter = new BinaryWriter(cacheStream, EncodingCache.UTF8NoBOM, true);

            headerWriter.Write((int)_infosStream. Length); // infosLen
            headerWriter.Write((int)_codesStream. Length); // codesLen
            headerWriter.Write((int)_relocsStream.Length); // relocsLen

            headerWriter.Dispose();
        }

        internal static void FullTranslate(ConcurrentDictionary<ulong, TranslatedFunction> funcsHighCq, IntPtr pageTable)
        {
            if ((int)_infosStream. Length != 0 &&
                (int)_codesStream. Length != 0 &&
                (int)_relocsStream.Length != 0)
            {
                _infosStream. Seek(0L, SeekOrigin.Begin);
                _codesStream. Seek(0L, SeekOrigin.Begin);
                _relocsStream.Seek(0L, SeekOrigin.Begin);

                BinaryReader infoReader  = new BinaryReader(_infosStream,  EncodingCache.UTF8NoBOM, true);
                BinaryReader relocReader = new BinaryReader(_relocsStream, EncodingCache.UTF8NoBOM, true);

                for (int i = 0; i < (int)_infosStream.Length / InfoEntry.Size; i++) // infosEntriesCount
                {
                    InfoEntry infoEntry = ReadInfo(infoReader);

                    byte[] code = ReadCode(infoEntry.CodeLen);

                    if (infoEntry.RelocEntriesCount != 0)
                    {
                        PatchCode(code, GetRelocEntries(relocReader, infoEntry.RelocEntriesCount), pageTable);
                    }

                    bool isAddressUnique = funcsHighCq.TryAdd((ulong)infoEntry.Address, FastTranslate(code));

                    Debug.Assert(isAddressUnique, $"The address 0x{(ulong)infoEntry.Address:X16} is not unique.");
                }

                infoReader. Dispose();
                relocReader.Dispose();

                if (_infosStream. Position != _infosStream. Length ||
                    _codesStream. Position != _codesStream. Length ||
                    _relocsStream.Position != _relocsStream.Length)
                {
                    throw new Exception("Unexpected unbalance of memory streams.");
                }
            }
        }

        private static InfoEntry ReadInfo(BinaryReader infoReader)
        {
            long address           = infoReader.ReadInt64();
            int  codeLen           = infoReader.ReadInt32();
            int  relocEntriesCount = infoReader.ReadInt32();

            return new InfoEntry(address, codeLen, relocEntriesCount);
        }

        private static byte[] ReadCode(int codeLen)
        {
            byte[] codeBuf = new byte[codeLen];

            _codesStream.Read(codeBuf, 0, codeLen);

            return codeBuf;
        }

        private static RelocEntry[] GetRelocEntries(BinaryReader relocReader, int relocEntriesCount)
        {
            RelocEntry[] relocEntries = new RelocEntry[relocEntriesCount];

            for (int i = 0; i < relocEntriesCount; i++)
            {
                int    position = relocReader.ReadInt32();
                string name     = relocReader.ReadString();

                relocEntries[i] = new RelocEntry(position, name);
            }

            return relocEntries;
        }

        private static void PatchCode(byte[] code, RelocEntry[] relocEntries, IntPtr pageTable)
        {
            foreach (RelocEntry relocEntry in relocEntries)
            {
                byte[] immBytes = new byte[8];

                if (relocEntry.Name == nameof(MemoryManager.PageTable))
                {
                    immBytes = BitConverter.GetBytes((ulong)pageTable.ToInt64());
                }
                else if (Delegates.TryGetDelegateFuncPtr(relocEntry.Name, out IntPtr funcPtr))
                {
                    immBytes = BitConverter.GetBytes((ulong)funcPtr.ToInt64());
                }
                else
                {
                    throw new Exception($"Unexpected reloc entry {relocEntry}.");
                }

                Buffer.BlockCopy(immBytes, 0, code, relocEntry.Position, 8);
            }
        }

        private static TranslatedFunction FastTranslate(byte[] code)
        {
            CompiledFunction cFunc = new CompiledFunction(code);

            IntPtr codePtr = JitCache.Map(cFunc);

            GuestFunction gFunc = Marshal.GetDelegateForFunctionPointer<GuestFunction>(codePtr);

            TranslatedFunction tFunc = new TranslatedFunction(gFunc, rejit: false);

            return tFunc;
        }

        internal static void WriteInfoCodeReloc(long address, AotInfo aotInfo)
        {
            lock (_locker) // Write.
            {
                if (!_disposed)
                {
                    WriteInfo (new InfoEntry(address, aotInfo));
                    WriteCode (aotInfo);
                    WriteReloc(aotInfo);
                }
            }
        }

        private static void WriteInfo(InfoEntry infoEntry)
        {
            _infosWriter.Write(infoEntry.Address);
            _infosWriter.Write(infoEntry.CodeLen);
            _infosWriter.Write(infoEntry.RelocEntriesCount);
        }

        private static void WriteCode(AotInfo aotInfo)
        {
            aotInfo.CodeStream.WriteTo(_codesStream);
        }

        private static void WriteReloc(AotInfo aotInfo)
        {
            aotInfo.RelocStream.WriteTo(_relocsStream);
        }

        private struct InfoEntry
        {
            public const int Size = 16; // Bytes.

            public long Address;
            public int  CodeLen;
            public int  RelocEntriesCount;

            public InfoEntry(long address, int codeLen, int relocEntriesCount)
            {
                Address           = address;
                CodeLen           = codeLen;
                RelocEntriesCount = relocEntriesCount;
            }

            public InfoEntry(long address, AotInfo aotInfo)
            {
                Address           = address;
                CodeLen           = (int)aotInfo.CodeStream.Length;
                RelocEntriesCount = aotInfo.RelocEntriesCount;
            }
        }

        public static void Dispose()
        {
            if (!_disposed)
            {
                _timer.Elapsed -= MergeAndSave;
                _timer.Dispose();

                lock (_locker) // Dispose.
                {
                    _infosWriter.Dispose();

                    _infosStream. Dispose();
                    _codesStream. Dispose();
                    _relocsStream.Dispose();

                    _disposed = true;
                }
            }
        }
    }
}
