using ARMeilleure.State;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Threading;

namespace ARMeilleure.Translation.PTC
{
    public static class PtcProfiler
    {
        private const string HeaderMagic = "Phd";

        private const uint InternalVersion = 1712; //! Not to be incremented manually for each change to the ARMeilleure project.

        private const int SaveInterval = 30; // Seconds.

        private const CompressionLevel SaveCompressionLevel = CompressionLevel.Fastest;

        private static readonly BinaryFormatter _binaryFormatter;

        private static readonly System.Timers.Timer _timer;

        private static readonly ManualResetEvent _waitEvent;

        private static readonly ConcurrentQueue<(ulong address, ulong size, ExecutionMode mode)> _backgroundQueue;
        private static readonly AutoResetEvent _backgroundEvent;

        private static readonly object _lock;

        private static bool _disposed;

        internal static Dictionary<ulong, (ExecutionMode mode, bool highCq, bool overlapped)> ProfiledFuncs { get; private set; }

        internal static bool Enabled { get; private set; }

        public static ulong StaticCodeStart { internal get; set; }
        public static ulong StaticCodeSize  { internal get; set; }

        static PtcProfiler()
        {
            _binaryFormatter = new BinaryFormatter();

            _timer = new System.Timers.Timer((double)SaveInterval * 1000d);
            _timer.Elapsed += PreSave;

            _waitEvent = new ManualResetEvent(true);

            _lock = new object();

            _disposed = false;

            ProfiledFuncs = new Dictionary<ulong, (ExecutionMode, bool, bool)>();

            Enabled = false;

            _backgroundQueue = new ConcurrentQueue<(ulong, ulong, ExecutionMode)>();
            _backgroundEvent = new AutoResetEvent(false);

            ThreadPool.QueueUserWorkItem(BackgroundMethod);
        }

        internal static void AddEntry(ulong address, ExecutionMode mode, bool highCq)
        {
            if (IsAddressInStaticCodeRange(address))
            {
                Debug.Assert(!highCq);

                lock (_lock)
                {
                    Debug.Assert(!ProfiledFuncs.ContainsKey(address));

                    ProfiledFuncs.TryAdd(address, (mode, highCq: false, overlapped: false));
                }
            }
        }

        internal static void UpdateEntries(ulong address, ulong size, ExecutionMode mode, bool highCq)
        {
            if (IsAddressInStaticCodeRange(address))
            {
                Debug.Assert(highCq);

                _backgroundQueue.Enqueue((address, size, mode));
                _backgroundEvent.Set();
            }
        }

        private static void BackgroundMethod(object state)
        {
            while (!_disposed)
            {
                if (Enabled && _backgroundQueue.TryDequeue(out var item))
                {
                    lock (_lock)
                    {
                        if (Enabled)
                        {
                            Debug.Assert(ProfiledFuncs.ContainsKey(item.address));

                            ProfiledFuncs[item.address] = (item.mode, highCq: true, overlapped: false);

                            foreach (ulong address in new List<ulong>(ProfiledFuncs.Keys))
                            {
                                var value = ProfiledFuncs[address];

                                if (!value.highCq && address >= item.address && address < item.address + item.size)
                                {
                                    if (!Enabled)
                                    {
                                        break;
                                    }

                                    ProfiledFuncs[address] = (value.mode, highCq: false, overlapped: true);
                                }
                            }
                        }
                    }

                    if (Enabled)
                    {
                        Thread.Sleep(1);
                    }
                }
                else
                {
                    if (!_disposed)
                    {
                        _backgroundEvent.WaitOne();
                    }
                }
            }
        }

        internal static bool IsAddressInStaticCodeRange(ulong address)
        {
            return address >= StaticCodeStart && address < StaticCodeStart + StaticCodeSize;
        }

        internal static void ClearEntries()
        {
            lock (_lock)
            {
                ProfiledFuncs.Clear();
            }

            _backgroundQueue.Clear();
        }

        internal static void PreLoad()
        {
            string fileNameActual = String.Concat(Ptc.CachePathActual, ".info");
            string fileNameBackup = String.Concat(Ptc.CachePathBackup, ".info");

            FileInfo fileInfoActual = new FileInfo(fileNameActual);
            FileInfo fileInfoBackup = new FileInfo(fileNameBackup);

            if (fileInfoActual.Exists && fileInfoActual.Length != 0L)
            {
                if (!Load(fileNameActual))
                {
                    if (fileInfoBackup.Exists && fileInfoBackup.Length != 0L)
                    {
                        Load(fileNameBackup);
                    }
                }
            }
            else if (fileInfoBackup.Exists && fileInfoBackup.Length != 0L)
            {
                Load(fileNameBackup);
            }
        }

        private static bool Load(string fileName)
        {
            using (FileStream compressedStream = new FileStream(fileName, FileMode.Open))
            using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, true))
            using (MemoryStream stream = new MemoryStream())
            using (MD5 md5 = MD5.Create())
            {
                int hashSize = md5.HashSize / 8;

                try
                {
                    deflateStream.CopyTo(stream);
                }
                catch
                {
                    InvalidateCompressedStream(compressedStream);

                    return false;
                }

                stream.Seek(0L, SeekOrigin.Begin);

                byte[] currentHash = new byte[hashSize];
                stream.Read(currentHash, 0, hashSize);

                byte[] expectedHash = md5.ComputeHash(stream);

                if (!CompareHash(currentHash, expectedHash))
                {
                    InvalidateCompressedStream(compressedStream);

                    return false;
                }

                stream.Seek((long)hashSize, SeekOrigin.Begin);

                Header header = ReadHeader(stream);

                if (header.Magic != HeaderMagic)
                {
                    InvalidateCompressedStream(compressedStream);

                    return false;
                }

                if (header.InfoFileVersion != InternalVersion)
                {
                    InvalidateCompressedStream(compressedStream);

                    return false;
                }

                try
                {
                    ProfiledFuncs = Deserialize(stream);
                }
                catch
                {
                    ProfiledFuncs = new Dictionary<ulong, (ExecutionMode, bool, bool)>();

                    InvalidateCompressedStream(compressedStream);

                    return false;
                }

                return true;
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

                header.InfoFileVersion = headerReader.ReadUInt32();

                return header;
            }
        }

        private static Dictionary<ulong, (ExecutionMode, bool, bool)> Deserialize(MemoryStream stream)
        {
            using (BinaryReader reader = new BinaryReader(stream, EncodingCache.UTF8NoBOM, true))
            {
                var profiledFuncs = new Dictionary<ulong, (ExecutionMode, bool, bool)>();

                int profiledFuncsCount = reader.ReadInt32();

                for (int i = 0; i < profiledFuncsCount; i++)
                {
                    ulong address = reader.ReadUInt64();

                    ExecutionMode mode = (ExecutionMode)reader.ReadInt32();
                    bool highCq = reader.ReadBoolean();
                    bool overlapped = reader.ReadBoolean();

                    profiledFuncs.Add(address, (mode, highCq, overlapped));
                }

                return profiledFuncs;
            }
        }

        private static void InvalidateCompressedStream(FileStream compressedStream)
        {
            compressedStream.SetLength(0L);
        }

        internal static void PreSave(object source, System.Timers.ElapsedEventArgs e)
        {
            _waitEvent.Reset();

            string fileNameActual = String.Concat(Ptc.CachePathActual, ".info");
            string fileNameBackup = String.Concat(Ptc.CachePathBackup, ".info");

            FileInfo fileInfoActual = new FileInfo(fileNameActual);

            if (fileInfoActual.Exists && fileInfoActual.Length != 0L)
            {
                File.Copy(fileNameActual, fileNameBackup, true);
            }

            Save(fileNameActual);

            _waitEvent.Set();
        }

        private static void Save(string fileName)
        {
            using (MemoryStream stream = new MemoryStream())
            using (MD5 md5 = MD5.Create())
            {
                int hashSize = md5.HashSize / 8;

                stream.Seek((long)hashSize, SeekOrigin.Begin);

                WriteHeader(stream);

                lock (_lock)
                {
                    Serialize(stream, ProfiledFuncs);
                }

                stream.Seek((long)hashSize, SeekOrigin.Begin);
                byte[] hash = md5.ComputeHash(stream);

                stream.Seek(0L, SeekOrigin.Begin);
                stream.Write(hash, 0, hashSize);

                using (FileStream compressedStream = new FileStream(fileName, FileMode.OpenOrCreate))
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
        }

        private static void WriteHeader(MemoryStream stream)
        {
            using (BinaryWriter headerWriter = new BinaryWriter(stream, EncodingCache.UTF8NoBOM, true))
            {
                headerWriter.Write((string)HeaderMagic); // Header.Magic

                headerWriter.Write((uint)InternalVersion); // Header.InfoFileVersion
            }
        }

        private static void Serialize(MemoryStream stream, Dictionary<ulong, (ExecutionMode, bool, bool)> profiledFuncs)
        {
            using (BinaryWriter writer = new BinaryWriter(stream, EncodingCache.UTF8NoBOM, true))
            {
                writer.Write((int)profiledFuncs.Count);

                foreach (var kv in profiledFuncs)
                {
                    writer.Write((ulong)kv.Key); // address

                    writer.Write((int)kv.Value.Item1); // mode
                    writer.Write((bool)kv.Value.Item2); // highCq
                    writer.Write((bool)kv.Value.Item3); // overlapped
                }
            }
        }

        private struct Header
        {
            public string Magic;

            public uint InfoFileVersion;
        }

        internal static void Start()
        {
            if (Ptc.State == PtcState.Enabled ||
                Ptc.State == PtcState.Continuing)
            {
                Enabled = true;

                _timer.Enabled = true;
            }
        }

        public static void Stop()
        {
            Enabled = false;

            if (!_disposed)
            {
                _timer.Enabled = false;
            }
        }

        internal static void Wait()
        {
            _waitEvent.WaitOne();
        }

        public static void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _timer.Elapsed -= PreSave;
                _timer.Dispose();

                Wait();
                _waitEvent.Dispose();

                _backgroundEvent.Set();
                _backgroundEvent.Dispose();
            }
        }
    }
}