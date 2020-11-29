using Ryujinx.Common;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu.Memory;
using Ryujinx.Graphics.Gpu.Shader.Cache.Definition;
using Ryujinx.Graphics.Shader;
using Ryujinx.Graphics.Shader.Translation;
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

        private static byte[] ComputeGuestProgramCode(ReadOnlySpan<GuestShaderCacheEntry> cachedShaderEntries, TransformFeedbackDescriptor[] tfd, bool forHashCompute = false)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);

                foreach (GuestShaderCacheEntry cachedShaderEntry in cachedShaderEntries)
                {
                    if (cachedShaderEntry != null)
                    {
                        // Code (and Code A if present)
                        stream.Write(cachedShaderEntry.Code);

                        if (forHashCompute)
                        {
                            // Guest GPU accessor header (only write this for hashes, already present in the header for dumps)
                            writer.WriteStruct(cachedShaderEntry.Header.GpuAccessorHeader);
                        }

                        // Texture descriptors
                        foreach (GuestTextureDescriptor textureDescriptor in cachedShaderEntry.TextureDescriptors.Values)
                        {
                            writer.WriteStruct(textureDescriptor);
                        }
                    }
                }

                // Transformation feedback
                if (tfd != null)
                {
                    foreach (TransformFeedbackDescriptor transform in tfd)
                    {
                        writer.WriteStruct(new GuestShaderCacheTransformFeedbackHeader(transform.BufferIndex, transform.Stride, transform.VaryingLocations.Length));
                        writer.Write(transform.VaryingLocations);
                    }
                }

                return stream.ToArray();
            }
        }

        public static Hash128 ComputeGuestHashFromCache(ReadOnlySpan<GuestShaderCacheEntry> cachedShaderEntries, TransformFeedbackDescriptor[] tfd = null)
        {
            return XXHash128.ComputeHash(ComputeGuestProgramCode(cachedShaderEntries, tfd, true));
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
        /// Create a new instance of <see cref="GuestGpuAccessorHeader"/> from an gpu accessor.
        /// </summary>
        /// <param name="gpuAccessor">The gpu accessor</param>
        /// <returns>a new instance of <see cref="GuestGpuAccessorHeader"/></returns>
        public static GuestGpuAccessorHeader CreateGuestGpuAccessorCache(IGpuAccessor gpuAccessor)
        {
            return new GuestGpuAccessorHeader
            {
                ComputeLocalSizeX = gpuAccessor.QueryComputeLocalSizeX(),
                ComputeLocalSizeY = gpuAccessor.QueryComputeLocalSizeY(),
                ComputeLocalSizeZ = gpuAccessor.QueryComputeLocalSizeZ(),
                ComputeLocalMemorySize = gpuAccessor.QueryComputeLocalMemorySize(),
                ComputeSharedMemorySize = gpuAccessor.QueryComputeSharedMemorySize(),
                PrimitiveTopology = gpuAccessor.QueryPrimitiveTopology(),
            };
        }

        public static GuestShaderCacheEntry[] CreateShaderCacheEntries(MemoryManager memoryManager, ReadOnlySpan<TranslatorContext> shaderContexts)
        {
            GuestShaderCacheEntry ComputeStage(TranslatorContext context)
            {
                if (context == null)
                {
                    return null;
                }

                int sizeA = context.AddressA == 0 ? 0 : context.SizeA;

                byte[] code = new byte[context.Size + sizeA];

                memoryManager.GetSpan(context.Address, context.Size).CopyTo(code);

                if (context.AddressA != 0)
                {
                    memoryManager.GetSpan(context.AddressA, context.SizeA).CopyTo(code.AsSpan().Slice(context.Size, context.SizeA));
                }

                GuestGpuAccessorHeader gpuAccessorHeader = CreateGuestGpuAccessorCache(context.GpuAccessor);

                if (context.GpuAccessor is GpuAccessor)
                {
                    gpuAccessorHeader.TextureDescriptorCount = context.TextureHandlesForCache.Count;
                }

                GuestShaderCacheEntryHeader header = new GuestShaderCacheEntryHeader(context.Stage, context.Size, sizeA, gpuAccessorHeader);

                GuestShaderCacheEntry entry = new GuestShaderCacheEntry(header, code);

                if (context.GpuAccessor is GpuAccessor gpuAccessor)
                {
                    foreach (int textureHandle in context.TextureHandlesForCache)
                    {
                        GuestTextureDescriptor textureDescriptor = ((Image.TextureDescriptor)gpuAccessor.GetTextureDescriptor(textureHandle)).ToCache();

                        textureDescriptor.Handle = (uint)textureHandle;

                        entry.TextureDescriptors.Add(textureHandle, textureDescriptor);
                    }
                }

                return entry;
            }

            GuestShaderCacheEntry[] entries = new GuestShaderCacheEntry[shaderContexts.Length];

            for (int i = 0; i < shaderContexts.Length; i++)
            {
                entries[i] = ComputeStage(shaderContexts[i]);
            }

            return entries;
        }

        public static byte[] CreateGuestProgramDump(GuestShaderCacheEntry[] shaderCacheEntries, TransformFeedbackDescriptor[] tfd = null)
        {
            using (MemoryStream resultStream = new MemoryStream())
            {
                BinaryWriter resultStreamWriter = new BinaryWriter(resultStream);

                byte transformFeedbackCount = 0;

                if (tfd != null)
                {
                    transformFeedbackCount = (byte)tfd.Length;
                }

                // Header
                resultStreamWriter.WriteStruct(new GuestShaderCacheHeader((byte)shaderCacheEntries.Length, transformFeedbackCount));

                // Write all entries header
                foreach (GuestShaderCacheEntry entry in shaderCacheEntries)
                {
                    if (entry == null)
                    {
                        resultStreamWriter.WriteStruct(new GuestShaderCacheEntryHeader());
                    }
                    else
                    {
                        resultStreamWriter.WriteStruct(entry.Header);
                    }
                }

                // Finally, write all program code and all transform feedback information.
                resultStreamWriter.Write(ComputeGuestProgramCode(shaderCacheEntries, tfd));

                return resultStream.ToArray();
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
