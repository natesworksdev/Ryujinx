using ARMeilleure.State;
using System;
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
        private const int SaveInterval = 15; // Seconds.

        private const CompressionLevel SaveCompressionLevel = CompressionLevel.Fastest;

        private static readonly BinaryFormatter _binaryFormatter;

        private static readonly System.Timers.Timer _timer;

        private static readonly ManualResetEvent _waitEvent;

        private static readonly object _locker;

        private static bool _disposed;

        internal static Dictionary<ulong, (ExecutionMode mode, bool highCq)> ProfiledFuncs { get; private set; }

        internal static bool Enabled { get; private set; }

        static PtcProfiler()
        {
            _binaryFormatter = new BinaryFormatter();

            _timer = new System.Timers.Timer((double)SaveInterval * 1000d);
            _timer.Elapsed += Save;

            _waitEvent = new ManualResetEvent(true);

            _locker = new object();

            _disposed = false;

            ProfiledFuncs = new Dictionary<ulong, (ExecutionMode, bool)>();

            Enabled = false;
        }

        internal static void AddEntry(ulong address, ExecutionMode mode, bool highCq)
        {
            lock (_locker)
            {
                Debug.Assert(!highCq && !ProfiledFuncs.ContainsKey(address));

                ProfiledFuncs.TryAdd(address, (mode, highCq));
            }
        }

        internal static void UpdateEntry(ulong address, ExecutionMode mode, bool highCq)
        {
            lock (_locker)
            {
                Debug.Assert(highCq && ProfiledFuncs.ContainsKey(address));

                ProfiledFuncs[address] = (mode, highCq);
            }
        }

        internal static void ClearEntries()
        {
            ProfiledFuncs.Clear();
        }

        internal static void Load()
        {
            FileInfo fileInfo = new FileInfo(String.Concat(Ptc.CachePath, ".info"));

            if (fileInfo.Exists && fileInfo.Length != 0L)
            {
                using (FileStream compressedStream = new FileStream(String.Concat(Ptc.CachePath, ".info"), FileMode.Open))
                using (DeflateStream deflateStream = new DeflateStream(compressedStream, CompressionMode.Decompress, true))
                using (MemoryStream stream = new MemoryStream())
                using (MD5 md5 = MD5.Create())
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

                        try
                        {
                            ProfiledFuncs = (Dictionary<ulong, (ExecutionMode, bool)>)_binaryFormatter.Deserialize(stream);
                        }
                        catch
                        {
                            ProfiledFuncs = new Dictionary<ulong, (ExecutionMode, bool)>();
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

        private static void InvalidateCompressedStream(FileStream compressedStream)
        {
            compressedStream.SetLength(0L);
        }

        private static void Save(object source, System.Timers.ElapsedEventArgs e)
        {
            _waitEvent.Reset();

            using (MemoryStream stream = new MemoryStream())
            using (MD5 md5 = MD5.Create())
            {
                int hashSize = md5.HashSize / 8;

                stream.Seek((long)hashSize, SeekOrigin.Begin);

                lock (_locker)
                {
                    _binaryFormatter.Serialize(stream, ProfiledFuncs);
                }

                stream.Seek((long)hashSize, SeekOrigin.Begin);
                byte[] hash = md5.ComputeHash(stream);

                stream.Seek(0L, SeekOrigin.Begin);
                stream.Write(hash, 0, hashSize);

                using (FileStream compressedStream = new FileStream(String.Concat(Ptc.CachePath, ".info"), FileMode.OpenOrCreate))
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

                _timer.Elapsed -= Save;
                _timer.Dispose();

                Wait();
                _waitEvent.Dispose();
            }
        }
    }
}