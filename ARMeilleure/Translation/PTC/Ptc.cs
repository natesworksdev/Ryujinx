using ARMeilleure.CodeGen;
using ARMeilleure.Memory;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace ARMeilleure.Translation.PTC
{
    public static class Ptc
    {
        private const string HeaderMagic = "PTChd";

        private const int InternalVersion = 0; //! To be incremented manually for each change to the ARMeilleure project.

        private const string BaseDir = "Ryujinx";

        private const string TitleIdTextDefault = "0000000000000000";
        private const string DisplayVersionDefault = "0";

        internal const int MinCodeLengthToSave = 0; // Bytes.

        internal const int PageTableIndex = -1; // Must be a negative value.
        internal const int JumpPointerIndex = -2; // Must be a negative value.
        internal const int DynamicPointerIndex = -3; // Must be a negative value.

        private const CompressionLevel SaveCompressionLevel = CompressionLevel.Fastest;

        private static readonly MemoryStream _infosStream;
        private static readonly MemoryStream _codesStream;
        private static readonly MemoryStream _relocsStream;

        private static readonly BinaryWriter _infosWriter;

        private static readonly BinaryFormatter _binaryFormatter;

        private static readonly ManualResetEvent _waitEvent;

        private static readonly string _basePath;

        private static readonly object _locker;

        private static bool _disposed;

        internal static PtcJumpTable PtcJumpTable { get; private set; }

        internal static string TitleIdText { get; private set; }
        internal static string DisplayVersion { get; private set; }

        internal static string CachePath { get; private set; }

        internal static PtcState State { get; private set; }

        static Ptc()
        {
            _infosStream = new MemoryStream();
            _codesStream = new MemoryStream();
            _relocsStream = new MemoryStream();

            _infosWriter = new BinaryWriter(_infosStream, EncodingCache.UTF8NoBOM, true);

            _binaryFormatter = new BinaryFormatter();

            _waitEvent = new ManualResetEvent(true);

            _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), BaseDir);

            _locker = new object();

            _disposed = false;

            PtcJumpTable = new PtcJumpTable();

            TitleIdText = TitleIdTextDefault;
            DisplayVersion = DisplayVersionDefault;

            CachePath = string.Empty;

            Disable();
        }

        public static void Initialize(string titleIdText, string displayVersion, bool enabled)
        {
            Wait();
            ClearMemoryStreams();
            PtcJumpTable.Clear();

            PtcProfiler.Stop();
            PtcProfiler.Wait();
            PtcProfiler.ClearEntries();

            if (String.IsNullOrEmpty(titleIdText) || titleIdText == TitleIdTextDefault)
            {
                TitleIdText = TitleIdTextDefault;
                DisplayVersion = DisplayVersionDefault;

                CachePath = string.Empty;

                Disable();

                return;
            }

            TitleIdText = titleIdText;
            DisplayVersion = !String.IsNullOrEmpty(displayVersion) ? displayVersion : DisplayVersionDefault;

            string workPath = Path.Combine(_basePath, "games", TitleIdText, "cache", "cpu");

            if (enabled && !Directory.Exists(workPath)) Directory.CreateDirectory(workPath);

            CachePath = Path.Combine(workPath, DisplayVersion);

            if (enabled) Enable(); else Disable();

            if (enabled)
            {
                Load();

                PtcProfiler.Load();
            }
        }

        internal static void ClearMemoryStreams()
        {
            _infosStream.SetLength(0L);
            _codesStream.SetLength(0L);
            _relocsStream.SetLength(0L);
        }

        private static void Load()
        {
            FileInfo fileInfo = new FileInfo(String.Concat(CachePath, ".cache"));

            if (fileInfo.Exists && fileInfo.Length != 0L)
            {
                using (FileStream compressedStream = new FileStream(String.Concat(CachePath, ".cache"), FileMode.Open))
                using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, true))
                using (MemoryStream stream = new MemoryStream())
                using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
                {
                    try
                    {
                        int hashSize = md5.HashSize / 8;

                        deflateStream.CopyTo(stream);

                        stream.Seek(0L, SeekOrigin.Begin);

                        byte[] currentHash = new byte[hashSize];
                        stream.Read(currentHash, 0, hashSize);

                        byte[] expectedHash = md5.ComputeHash(stream);

                        if (!CompareHash(currentHash, expectedHash))
                        {
                            InvalidateCompressedStream(compressedStream);

                            return;
                        }

                        stream.Seek((long)hashSize, SeekOrigin.Begin);

                        Header header = ReadHeader(stream);

                        if (header.Magic != HeaderMagic)
                        {
                            InvalidateCompressedStream(compressedStream);

                            return;
                        }

                        if (header.CacheFileVersion != InternalVersion)
                        {
                            InvalidateCompressedStream(compressedStream);

                            return;
                        }

                        if (header.FeatureInfo != GetFeatureInfo())
                        {
                            InvalidateCompressedStream(compressedStream);

                            return;
                        }

                        if (header.InfosLen % InfoEntry.Size != 0)
                        {
                            InvalidateCompressedStream(compressedStream);

                            return;
                        }

                        byte[] infosBuf = new byte[header.InfosLen];
                        byte[] codesBuf = new byte[header.CodesLen];
                        byte[] relocsBuf = new byte[header.RelocsLen];

                        stream.Read(infosBuf, 0, header.InfosLen);
                        stream.Read(codesBuf, 0, header.CodesLen);
                        stream.Read(relocsBuf, 0, header.RelocsLen);

                        try
                        {
                            PtcJumpTable = (PtcJumpTable)_binaryFormatter.Deserialize(stream);
                        }
                        catch
                        {
                            PtcJumpTable = new PtcJumpTable();

                            InvalidateCompressedStream(compressedStream);

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
                            ClearMemoryStreams();
                            PtcJumpTable.Clear();
                        }
                    }
                    catch
                    {
                        InvalidateCompressedStream(compressedStream);
                    }
                }
            }
        }

        private static bool CompareHash(ReadOnlySpan<byte> currentHash, ReadOnlySpan<byte> expectedHash)
        {
            return currentHash.SequenceEqual(expectedHash);
        }

        private static Header ReadHeader(MemoryStream stream)
        {
            using (BinaryReader headerReader = new BinaryReader(stream, EncodingCache.UTF8NoBOM, true))
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

        private static void InvalidateCompressedStream(FileStream compressedStream)
        {
            compressedStream.SetLength(0L);
        }

        private static void Save(object state)
        {
            _waitEvent.Reset();

            using (MemoryStream stream = new MemoryStream())
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                int hashSize = md5.HashSize / 8;

                stream.Seek((long)hashSize, SeekOrigin.Begin);

                WriteHeader(stream);

                _infosStream.WriteTo(stream);
                _codesStream.WriteTo(stream);
                _relocsStream.WriteTo(stream);

                _binaryFormatter.Serialize(stream, (object)PtcJumpTable);

                stream.Seek((long)hashSize, SeekOrigin.Begin);
                byte[] hash = md5.ComputeHash(stream);

                stream.Seek(0L, SeekOrigin.Begin);
                stream.Write(hash, 0, hashSize);

                using (FileStream compressedStream = new FileStream(String.Concat(CachePath, ".cache"), FileMode.OpenOrCreate))
                using (DeflateStream deflateStream = new DeflateStream(compressedStream, SaveCompressionLevel, true))
                {
                    try
                    {
                        stream.WriteTo(deflateStream);
                    }
                    catch
                    {
                        compressedStream.Position = 0L;
                    }

                    if (compressedStream.Position < compressedStream.Length)
                    {
                        compressedStream.SetLength(compressedStream.Position);
                    }
                }
            }

            _waitEvent.Set();
        }

        private static void WriteHeader(MemoryStream stream)
        {
            using (BinaryWriter headerWriter = new BinaryWriter(stream, EncodingCache.UTF8NoBOM, true))
            {
                headerWriter.Write((string)HeaderMagic); // Header.Magic

                headerWriter.Write((int)InternalVersion); // Header.CacheFileVersion
                headerWriter.Write((ulong)GetFeatureInfo()); // Header.FeatureInfo

                headerWriter.Write((int)_infosStream.Length); // Header.InfosLen
                headerWriter.Write((int)_codesStream.Length); // Header.CodesLen
                headerWriter.Write((int)_relocsStream.Length); // Header.RelocsLen
            }
        }

        internal static void LoadTranslations(ConcurrentDictionary<ulong, TranslatedFunction> funcsHighCq, IntPtr pageTable, JumpTable jumpTable)
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
                            PatchCode(code, GetRelocEntries(relocsReader, infoEntry.RelocEntriesCount), pageTable, jumpTable);
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

                jumpTable.Initialize(PtcJumpTable, funcsHighCq);

                PtcJumpTable.WriteJumpTable(jumpTable, funcsHighCq);
                PtcJumpTable.WriteDynamicTable(jumpTable);
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

        private static void PatchCode(Span<byte> code, RelocEntry[] relocEntries, IntPtr pageTable, JumpTable jumpTable)
        {
            foreach (RelocEntry relocEntry in relocEntries)
            {
                ulong imm;

                if (relocEntry.Index == PageTableIndex)
                {
                    imm = (ulong)pageTable.ToInt64();
                }
                else if (relocEntry.Index == JumpPointerIndex)
                {
                    imm = (ulong)jumpTable.JumpPointer.ToInt64();
                }
                else if (relocEntry.Index == DynamicPointerIndex)
                {
                    imm = (ulong)jumpTable.DynamicPointer.ToInt64();
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

            TranslatedFunction tFunc = new TranslatedFunction(gFunc, highCq: true);

            return tFunc;
        }

        internal static void MakeAndSaveTranslations(ConcurrentDictionary<ulong, TranslatedFunction> funcsHighCq, MemoryManager memory, JumpTable jumpTable)
        {
            if (PtcProfiler.ProfiledFuncsHighCq.Count != 0)
            {
                int translateCount = 0;

                void PtcLogger(object state) // TODO: .
                {
                    const int refreshRate = 1; // Seconds.

                    int funcsHighCqCount         = funcsHighCq.Count;
                    int ProfiledFuncsHighCqCount = PtcProfiler.ProfiledFuncsHighCq.Count;

                    void WriteLog() => Console.WriteLine($"{nameof(PtcLogger)}: {funcsHighCqCount + translateCount} of {ProfiledFuncsHighCqCount} functions to translate.");

                    while (funcsHighCqCount + translateCount < ProfiledFuncsHighCqCount)
                    {
                        if (State != PtcState.Enabled)
                        {
                            break;
                        }

                        WriteLog();

                        Thread.Sleep(refreshRate * 1000);
                    }

                    WriteLog();
                }

                ThreadPool.QueueUserWorkItem(PtcLogger);

                ParallelOptions parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 };

                Parallel.ForEach(PtcProfiler.ProfiledFuncsHighCq, parallelOptions, (item, state) =>
                {
                    if (!funcsHighCq.ContainsKey(item.Key))
                    {
                        TranslatedFunction func = Translator.Translate(memory, jumpTable, item.Key, item.Value, highCq: true);

                        bool isAddressUnique = funcsHighCq.TryAdd(item.Key, func);

                        Debug.Assert(isAddressUnique, $"The address 0x{item.Key:X16} is not unique.");

                        jumpTable.RegisterFunction(item.Key, func);

                        Interlocked.Increment(ref translateCount);
                    }

                    if (State != PtcState.Enabled)
                    {
                        state.Stop();
                    }
                });

                if (translateCount != 0)
                {
                    PtcJumpTable.Initialize(jumpTable);

                    PtcJumpTable.ReadJumpTable(jumpTable);
                    PtcJumpTable.ReadDynamicTable(jumpTable);

                    ThreadPool.QueueUserWorkItem(Save);
                }
            }
        }

        internal static void WriteInfoCodeReloc(long address, PtcInfo ptcInfo)
        {
            lock (_locker)
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

        private static ulong GetFeatureInfo()
        {
            ulong featureInfo = 0ul;

            featureInfo |= (Sse3.IsSupported      ? 1ul : 0ul) << 0;
            featureInfo |= (Pclmulqdq.IsSupported ? 1ul : 0ul) << 1;
            featureInfo |= (Ssse3.IsSupported     ? 1ul : 0ul) << 9;
            featureInfo |= (Fma.IsSupported       ? 1ul : 0ul) << 12;
            featureInfo |= (Sse41.IsSupported     ? 1ul : 0ul) << 19;
            featureInfo |= (Sse42.IsSupported     ? 1ul : 0ul) << 20;
            featureInfo |= (Popcnt.IsSupported    ? 1ul : 0ul) << 23;
            featureInfo |= (Aes.IsSupported       ? 1ul : 0ul) << 25;
            featureInfo |= (Avx.IsSupported       ? 1ul : 0ul) << 28;
            featureInfo |= (Sse.IsSupported       ? 1ul : 0ul) << 57;
            featureInfo |= (Sse2.IsSupported      ? 1ul : 0ul) << 58;

            return featureInfo;
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

        private static void Enable()
        {
            State = PtcState.Enabled;
        }

        public static void Close()
        {
            if (State == PtcState.Enabled)
            {
                State = PtcState.Closing;
            }
        }

        internal static void Disable()
        {
            State = PtcState.Disabled;
        }

        private static void Wait()
        {
            _waitEvent.WaitOne();
        }

        public static void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                Wait();
                _waitEvent.Dispose();

                _infosWriter.Dispose();

                _infosStream.Dispose();
                _codesStream.Dispose();
                _relocsStream.Dispose();
            }
        }
    }
}