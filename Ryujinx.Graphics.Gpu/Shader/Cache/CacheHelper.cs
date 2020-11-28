using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Shader.Cache.Definition;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Shader.Cache
{
    /// <summary>
    /// Helper to manipulate the disk shader cache.
    /// </summary>
    static class CacheHelper
    {
        public static bool TryReadManifestHeader(string manifestPath, out CacheManifestHeader header)
        {
            header = default;

            if (File.Exists(manifestPath))
            {
                Memory<byte> rawManifest = File.ReadAllBytes(manifestPath);

                if (MemoryMarshal.TryRead(rawManifest.Span, out header))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool TryReadManifestFile(string manifestPath, CacheGraphicsApi graphicsApi, CacheHashType hashType, out CacheManifestHeader header, out HashSet<Hash128> entries)
        {
            header = default;
            entries = new HashSet<Hash128>();

            if (File.Exists(manifestPath))
            {
                Memory<byte> rawManifest = File.ReadAllBytes(manifestPath);

                if (MemoryMarshal.TryRead(rawManifest.Span, out header))
                {
                    Memory<byte> hashTableRaw = rawManifest.Slice(Unsafe.SizeOf<CacheManifestHeader>());

                    bool isValid = header.IsValid(graphicsApi, hashType, hashTableRaw.Span);

                    if (isValid)
                    {
                        ReadOnlySpan<Hash128> hashTable = MemoryMarshal.Cast<byte, Hash128>(hashTableRaw.Span);

                        foreach (Hash128 hash in hashTable)
                        {
                            entries.Add(hash);
                        }
                    }

                    return isValid;
                }
            }

            return false;
        }

        public static byte[] ComputeManifest(ulong version, CacheGraphicsApi graphicsApi, CacheHashType hashType, HashSet<Hash128> entries)
        {
            CacheManifestHeader manifestHeader = new CacheManifestHeader(version, graphicsApi, hashType);

            byte[] data = new byte[Unsafe.SizeOf<CacheManifestHeader>() + entries.Count * Unsafe.SizeOf<Hash128>()];

            // CacheManifestHeader has the same size as a Hash128.
            Span<Hash128> dataSpan = MemoryMarshal.Cast<byte, Hash128>(data.AsSpan()).Slice(1);

            int i = 0;

            foreach (Hash128 hash in entries)
            {
                dataSpan[i++] = hash;
            }

            manifestHeader.UpdateChecksum(data.AsSpan().Slice(Unsafe.SizeOf<CacheManifestHeader>()));

            MemoryMarshal.Write(data, ref manifestHeader);

            return data;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetBaseCacheDirectory(string titleId) => Path.Combine(AppDataManager.GamesDirPath, titleId, "cache", "shader");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetCacheTempDataPath(string cacheDirectory) => Path.Combine(cacheDirectory, "temp");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetArchivePath(string cacheDirectory) => Path.Combine(cacheDirectory, "cache.zip");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetManifestPath(string cacheDirectory) => Path.Combine(cacheDirectory, "cache.info");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GenCacheTempFilePath(string cacheDirectory, Hash128 key) => Path.Combine(GetCacheTempDataPath(cacheDirectory), key.ToString());

        /// <summary>
        /// Generate the path to the cache directory.
        /// </summary>
        /// <param name="baseCacheDirectory">The base of the cache directory</param>
        /// <param name="graphicsApi">The graphics api in use</param>
        /// <param name="shaderProvider">The name of the shader provider in use</param>
        /// <param name="cacheName">The name of the cache</param>
        /// <returns>The path to the cache directory</returns>
        public static string GenerateCachePath(string baseCacheDirectory, CacheGraphicsApi graphicsApi, string shaderProvider, string cacheName)
        {
            string graphicsApiName = graphicsApi switch
            {
                CacheGraphicsApi.OpenGL => "opengl",
                CacheGraphicsApi.OpenGLES => "opengles",
                CacheGraphicsApi.Vulkan => "vulkan",
                CacheGraphicsApi.DirectX => "directx",
                CacheGraphicsApi.Metal => "metal",
                CacheGraphicsApi.Guest => "guest",
                _ => throw new NotImplementedException(graphicsApi.ToString()),
            };

            return Path.Combine(baseCacheDirectory, graphicsApiName, shaderProvider, cacheName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadFromArchive(ZipArchive archive, Hash128 entry)
        {
            if (archive != null)
            {
                ZipArchiveEntry archiveEntry = archive.GetEntry($"{entry}");

                if (archiveEntry != null)
                {
                    try
                    {
                        byte[] result = new byte[archiveEntry.Length];

                        using (Stream archiveStream = archiveEntry.Open())
                        {
                            archiveStream.Read(result);

                            return result;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error?.Print(LogClass.Gpu, $"Cannot load cache file {entry} from archive");
                        Logger.Error?.Print(LogClass.Gpu, e.ToString());
                    }
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadFromFile(string tempPath, Hash128 entry)
        {
            string cacheTempFilePath = GenCacheTempFilePath(tempPath, entry);

            try
            {
                return File.ReadAllBytes(cacheTempFilePath);
            }
            catch (Exception e)
            {
                Logger.Error?.Print(LogClass.Gpu, $"Cannot load cache file at {cacheTempFilePath}");
                Logger.Error?.Print(LogClass.Gpu, e.ToString());
            }

            return null;
        }

        public static Hash128 ComputeGuestHashFromCache(ReadOnlySpan<GuestShaderCacheEntry> cachedShaderEntries, TransformFeedbackDescriptor[] tfd)
        {
            byte[] data;

            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);
                foreach (GuestShaderCacheEntry cachedShaderEntry in cachedShaderEntries)
                {
                    if (cachedShaderEntry != null)
                    {
                        // Code (and Code A if present)
                        stream.Write(cachedShaderEntry.Code);

                        // Guest GPU accessor header
                        writer.WriteStruct(cachedShaderEntry.Header.GpuAccessorHeader);

                        // Texture descriptors
                        foreach (GuestTextureDescriptor textureDescriptor in cachedShaderEntry.TextureDescriptors.Values)
                        {
                            writer.WriteStruct(textureDescriptor);
                        }
                    }
                }

                // Transformation feedback
                WriteTransformationFeedbackInformation(stream, tfd);

                data = stream.ToArray();
            }

            return XXHash128.ComputeHash(data);
        }

        /// <summary>
        /// Read transform feedback descriptors from guest.
        /// </summary>
        /// <param name="data">The raw guest transform feedback descriptors</param>
        /// <param name="header">The guest shader program header</param>
        /// <returns>The transform feedback descriptors read from guest</returns>
        public static TransformFeedbackDescriptor[] ReadTransformationFeedbackInformations(ref ReadOnlySpan<byte> data, GuestShaderCacheHeader header)
        {
            if (header.TransformFeedbackCount != 0)
            {
                TransformFeedbackDescriptor[] result = new TransformFeedbackDescriptor[header.TransformFeedbackCount];

                for (int i = 0; i < result.Length; i++)
                {
                    GuestShaderCacheTransformFeedbackHeader feedbackHeader = MemoryMarshal.Read<GuestShaderCacheTransformFeedbackHeader>(data);

                    result[i] = new TransformFeedbackDescriptor(feedbackHeader.BufferIndex, feedbackHeader.Stride, data.Slice(Unsafe.SizeOf<GuestShaderCacheTransformFeedbackHeader>(), feedbackHeader.VaryingLocationsLength).ToArray());

                    data = data.Slice(Unsafe.SizeOf<GuestShaderCacheTransformFeedbackHeader>() + feedbackHeader.VaryingLocationsLength);
                }

                return result;
            }

            return null;
        }

        /// <summary>
        /// Write transform feedback guest information to the given stream.
        /// </summary>
        /// <param name="stream">The stream to write data to</param>
        /// <param name="tfd">The current transform feedback descriptors used</param>
        public static void WriteTransformationFeedbackInformation(Stream stream, TransformFeedbackDescriptor[] tfd)
        {
            if (tfd != null)
            {
                BinaryWriter writer = new BinaryWriter(stream);

                foreach (TransformFeedbackDescriptor transform in tfd)
                {
                    writer.WriteStruct(new GuestShaderCacheTransformFeedbackHeader(transform.BufferIndex, transform.Stride, transform.VaryingLocations.Length));
                    writer.Write(transform.VaryingLocations);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureArchiveUpToDate(string baseCacheDirectory, ZipArchive archive, HashSet<Hash128> entries)
        {
            foreach (Hash128 hash in entries)
            {
                string cacheTempFilePath = GenCacheTempFilePath(baseCacheDirectory, hash);

                if (File.Exists(cacheTempFilePath))
                {
                    string cacheHash = $"{hash}";

                    ZipArchiveEntry entry = archive.GetEntry(cacheHash);

                    entry?.Delete();

                    archive.CreateEntryFromFile(cacheTempFilePath, cacheHash);

                    File.Delete(cacheTempFilePath);
                }
            }
        }
    }
}
