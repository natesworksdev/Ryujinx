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
        private const int SaveInterval = 30; // Seconds.

        private const CompressionLevel SaveCompressionLevel = CompressionLevel.Fastest;

        private static readonly BinaryFormatter _binaryFormatter;

        private static readonly System.Timers.Timer _timer;

        private static readonly ManualResetEvent _waitEvent;

        private static readonly ConcurrentQueue<(ulong address, ulong size, ExecutionMode mode)> _backgroundQueue;
        private static readonly AutoResetEvent _backgroundEvent;

        private static readonly object _lock;

        private static bool _disposed;

        internal static Dictionary<ulong, (ExecutionMode mode, bool highCq, bool overlapped)> ProfiledFuncs { get; private set; } //! Not to be modified.

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
                        Debug.Assert(ProfiledFuncs.ContainsKey(item.address));

                        if (Enabled)
                        {
                            ProfiledFuncs[item.address] = (item.mode, highCq: true, overlapped: false);
                        }

                        foreach (ulong key in new List<ulong>(ProfiledFuncs.Keys))
                        {
                            var value = ProfiledFuncs[key];

                            if (!value.highCq && key >= item.address && key < item.address + item.size)
                            {
                                if (Enabled)
                                {
                                    ProfiledFuncs[key] = (value.mode, highCq: false, overlapped: true);
                                }
                            }
                        }
                    }

                    Thread.Sleep(1);
                }
                else
                {
                    _backgroundEvent.WaitOne();
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

                try
                {
                    ProfiledFuncs = (Dictionary<ulong, (ExecutionMode, bool, bool)>)_binaryFormatter.Deserialize(stream);
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

                lock (_lock)
                {
                    _binaryFormatter.Serialize(stream, ProfiledFuncs);
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