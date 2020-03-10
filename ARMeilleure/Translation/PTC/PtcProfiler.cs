using ARMeilleure.Memory;
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
        private const int SaveInterval = 15; // Seconds.

        private const CompressionLevel SaveCompressionLevel = CompressionLevel.Fastest;

        private static Dictionary<ulong, ExecutionMode> _profiledFuncsHighCq;

        private static readonly BinaryFormatter _binaryFormatter;

        private static readonly System.Timers.Timer _timer;

        private static readonly ManualResetEvent _waitEvent;

        private static readonly object _locker;

        private static bool _disposed;

        public static bool Enabled { get; private set; }

        static PtcProfiler()
        {
            _profiledFuncsHighCq = new Dictionary<ulong, ExecutionMode>();

            _binaryFormatter = new BinaryFormatter();

            _timer = new System.Timers.Timer((double)SaveInterval * 1000d);
            _timer.Elapsed += Save;

            _waitEvent = new ManualResetEvent(true);

            _locker = new object();

            _disposed = false;

            Enabled = false;
        }

        internal static void AddEntry(ulong address, ExecutionMode mode)
        {
            lock (_locker)
            {
                bool isAddressUnique = _profiledFuncsHighCq.TryAdd(address, mode);

                Debug.Assert(isAddressUnique, $"The address 0x{address:X16} is not unique.");
            }
        }

        internal static void ClearEntries()
        {
            _profiledFuncsHighCq.Clear();
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
                            _profiledFuncsHighCq = (Dictionary<ulong, ExecutionMode>)_binaryFormatter.Deserialize(stream);
                        }
                        catch
                        {
                            _profiledFuncsHighCq = new Dictionary<ulong, ExecutionMode>();
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

        private static void Save(Object source, System.Timers.ElapsedEventArgs e)
        {
            _waitEvent.Reset();

            using (MemoryStream stream = new MemoryStream())
            using (MD5 md5 = MD5.Create())
            {
                int hashSize = md5.HashSize / 8;

                stream.Seek((long)hashSize, SeekOrigin.Begin);

                lock (_locker)
                {
                    _binaryFormatter.Serialize(stream, (object)_profiledFuncsHighCq);
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

        internal static void DoAndSaveTranslations(ConcurrentDictionary<ulong, TranslatedFunction> funcsHighCq, MemoryManager memory)
        {
            if (_profiledFuncsHighCq.Count != 0)
            {
                int translateCount = 0;

                void Informer()
                {
                    const int refreshRate = 1; // Seconds.

                    while (Ptc.Enabled)
                    {
                        Console.WriteLine($"Informer: {translateCount} of {_profiledFuncsHighCq.Count} functions to translate."); // TODO: .

                        Thread.Sleep(refreshRate * 1000);
                    }
                }

                Thread informer = new Thread(Informer);

                informer.IsBackground = true;
                informer.Start();

                Ptc.Start();

                foreach (var item in _profiledFuncsHighCq)
                //System.Threading.Tasks.Parallel.ForEach(_profiledFuncsHighCq, (item, state) =>
                {
                    if (!funcsHighCq.ContainsKey(item.Key))
                    {
                        TranslatedFunction func = Translator.Translate(memory, item.Key, item.Value, highCq: true);

                        bool isAddressUnique = funcsHighCq.TryAdd(item.Key, func);

                        Debug.Assert(isAddressUnique, $"The address 0x{item.Key:X16} is not unique.");

                        Interlocked.Increment(ref translateCount);
                    }

                    if (!Ptc.Enabled)
                    {
                        break;
                        //state.Stop();
                    }
                //});
                }

                Ptc.Stop();

                Ptc.Wait();

                if (translateCount != 0)
                {
                    Ptc.MergeAndSave(null, null);
                }

                Ptc.ClearMemoryStreams();
            }
        }

        internal static void Wait() // TODO: Rename ?
        {
            _waitEvent.WaitOne();
        }

        internal static void Start()
        {
            if (Ptc.Enabled)
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

        public static void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _timer.Elapsed -= Save;
                _timer.Dispose();

                _waitEvent.WaitOne(); // TODO: Wait() ?
                _waitEvent.Dispose();
            }
        }
    }
}