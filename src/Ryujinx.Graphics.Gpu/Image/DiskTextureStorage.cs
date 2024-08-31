using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Memory;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Texture;
using Ryujinx.Graphics.Texture.Astc;
using Ryujinx.Graphics.Texture.FileFormats;
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Ryujinx.Graphics.Gpu.Image
{
    public class DiskTextureStorage
    {
        private const long MinTimeDeltaForRealTimeLoad = 5; // Seconds.

        private const int StrideAlignment = 4;

        private enum FileFormat
        {
            Dds,
            Png
        }

        private struct ScopedLoadLock : IDisposable
        {
            private readonly DiskTextureStorage _storage;
            private readonly string _outputFileName;
            private bool _isNewDump;

            public ScopedLoadLock(DiskTextureStorage storage, string outputFileName)
            {
                _storage = storage;
                _outputFileName = outputFileName;
                _isNewDump = storage._newDumpFiles.TryAdd(outputFileName, long.MaxValue);
            }

            public void Cancel()
            {
                _isNewDump = false;
                _storage._newDumpFiles.TryRemove(_outputFileName, out _);
            }

            public readonly void Dispose()
            {
                if (_isNewDump && TryGetFileTimestamp(_outputFileName, out long timestamp))
                {
                    _storage._newDumpFiles[_outputFileName] = timestamp;
                }
            }
        }

        private readonly struct TextureRequest
        {
            public readonly int Width;
            public readonly int Height;
            public readonly int Depth;
            public readonly int Layers;
            public readonly int Levels;
            public readonly Format Format;
            public readonly Target Target;
            public readonly byte[] Data;

            public TextureRequest(int width, int height, int depth, int layers, int levels, Format format, Target target, byte[] data)
            {
                Width = width;
                Height = height;
                Depth = depth;
                Layers = layers;
                Levels = levels;
                Format = format;
                Target = target;
                Data = data;
            }
        }

        private AsyncWorkQueue<(Texture, TextureRequest)> _exportQueue;
        private readonly List<string> _importList;

        private readonly ConcurrentDictionary<string, long> _newDumpFiles;
        private readonly ConcurrentDictionary<string, Texture> _fileToTextureMap;
        private FileSystemWatcher _fileSystemWatcher;

        private string _outputDirectoryPath;
        private FileFormat _outputFormat;
        private bool _enableTextureDump;
        private bool _enableRealTimeTextureEdit;

        internal bool IsActive => !string.IsNullOrEmpty(_outputDirectoryPath) || _importList.Count != 0;

        internal DiskTextureStorage()
        {
            _importList = new List<string>();

            _newDumpFiles = new ConcurrentDictionary<string, long>();
            _fileToTextureMap = new ConcurrentDictionary<string, Texture>();
        }

        internal void Initialize()
        {
            _enableTextureDump = GraphicsConfig.EnableTextureDump;
            _enableRealTimeTextureEdit = GraphicsConfig.EnableTextureRealTimeEdit;

            if (_enableRealTimeTextureEdit)
            {
                _fileSystemWatcher = new FileSystemWatcher();
                _fileSystemWatcher.Changed += OnChanged;
            }

            string textureDumpPath = GraphicsConfig.TextureDumpPath;

            if (!string.IsNullOrEmpty(textureDumpPath))
            {
                textureDumpPath = Path.Combine(textureDumpPath, GraphicsConfig.TitleId);
            }

            SetOutputDirectory(textureDumpPath);
            _outputFormat = GraphicsConfig.TextureDumpFormatPng ? FileFormat.Png : FileFormat.Dds;

            if (_enableTextureDump)
            {
                _exportQueue = new AsyncWorkQueue<(Texture, TextureRequest)>(ExportTexture, "GPU.TextureExportQueue");
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }

            // If this a new file that we just created, ignore it.
            if (_newDumpFiles.TryGetValue(e.FullPath, out long savedTimestamp) &&
                TryGetFileTimestamp(e.FullPath, out long currentTimestamp) &&
                savedTimestamp > currentTimestamp - MinTimeDeltaForRealTimeLoad)
            {
                return;
            }

            for (int attempt = 0; attempt < 100; attempt++)
            {
                try
                {
                    File.ReadAllBytes(e.FullPath);
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(10);
                }
            }

            if (_fileToTextureMap.TryGetValue(e.Name, out Texture texture))
            {
                texture.ForceReimport();
            }
        }

        public void AddInputDirectory(string directoryPath)
        {
            if (Directory.Exists(directoryPath) && !_importList.Contains(directoryPath))
            {
                _importList.Add(directoryPath);
            }
        }

        public void SetOutputDirectory(string directoryPath)
        {
            string previousOutputDirectoryPath = _outputDirectoryPath;

            if (!string.IsNullOrEmpty(previousOutputDirectoryPath))
            {
                _importList.Remove(previousOutputDirectoryPath);
            }

            bool hasOutputDir = !string.IsNullOrEmpty(directoryPath);

            if (hasOutputDir)
            {
                try
                {
                    Directory.CreateDirectory(directoryPath);
                }
                catch (Exception ex)
                {
                    LogDirCreationException(ex, directoryPath);
                    hasOutputDir = false;
                }
            }

            if (hasOutputDir)
            {
                _outputDirectoryPath = directoryPath;
                AddInputDirectory(directoryPath);

                if (_enableRealTimeTextureEdit)
                {
                    _fileSystemWatcher.Path = directoryPath;
                    _fileSystemWatcher.EnableRaisingEvents = true;
                }
            }
            else
            {
                _outputDirectoryPath = null;
            }
        }

        internal TextureInfoOverride? ImportTexture(out MemoryOwner<byte> cachedData, Texture texture, byte[] data)
        {
            cachedData = default;

            if (!IsSupportedFormat(texture.Format))
            {
                return null;
            }

            TextureInfoOverride? infoOverride = ImportDdsTexture(out cachedData, texture, data);

            if (!infoOverride.HasValue)
            {
                infoOverride = ImportPngTexture(out cachedData, texture, data);
            }

            return infoOverride;
        }

        private TextureInfoOverride? ImportDdsTexture(out MemoryOwner<byte> cachedData, Texture texture, byte[] data)
        {
            cachedData = default;

            if (!IsSupportedFormat(texture.Format))
            {
                return null;
            }

            TextureRequest request = new(
                texture.Width,
                texture.Height,
                texture.Depth,
                texture.Layers,
                texture.Info.Levels,
                texture.Format,
                texture.Target,
                data);

            ImageParameters parameters = default;
            MemoryOwner<byte> buffer = null;

            bool imported = false;
            string fileName = BuildFileName(request, "dds");

            foreach (string inputDirectoryPath in _importList)
            {
                string inputFileName = Path.Combine(inputDirectoryPath, fileName);

                if (File.Exists(inputFileName))
                {
                    _fileToTextureMap.AddOrUpdate(fileName, texture, (key, old) => texture);

                    byte[] imageFile = null;

                    try
                    {
                        imageFile = File.ReadAllBytes(inputFileName);
                    }
                    catch (IOException ex)
                    {
                        LogReadException(ex, inputFileName);
                        break;
                    }

                    ImageLoadResult loadResult = DdsFileFormat.TryLoadHeader(imageFile, out parameters);

                    if (loadResult != ImageLoadResult.Success)
                    {
                        LogFailureResult(loadResult, inputFileName);
                        break;
                    }

                    buffer = MemoryOwner<byte>.Rent(DdsFileFormat.CalculateSize(parameters));

                    loadResult = DdsFileFormat.TryLoadData(imageFile, buffer.Span);

                    if (loadResult != ImageLoadResult.Success)
                    {
                        LogFailureResult(loadResult, inputFileName);
                        break;
                    }

                    imported = true;
                    break;
                }
            }

            if (!imported)
            {
                return null;
            }

            if (parameters.Format == ImageFormat.B8G8R8A8Srgb || parameters.Format == ImageFormat.B8G8R8A8Unorm)
            {
                ConvertBgraToRgbaInPlace(buffer.Span);
            }

            cachedData = buffer;

            return new TextureInfoOverride(
                parameters.Width,
                parameters.Height,
                parameters.DepthOrLayers,
                parameters.Levels,
                ConvertToFormat(parameters.Format));
        }

        private TextureInfoOverride? ImportPngTexture(out MemoryOwner<byte> cachedData, Texture texture, byte[] data)
        {
            cachedData = default;

            if (!IsSupportedFormat(texture.Format))
            {
                return null;
            }

            TextureRequest request = new(
                texture.Width,
                texture.Height,
                texture.Depth,
                texture.Layers,
                texture.Info.Levels,
                texture.Format,
                texture.Target,
                data);

            MemoryOwner<byte> buffer = null;

            int importedFirstLevel = 0;
            int importedWidth = 0;
            int importedHeight = 0;
            int levels = 0;
            int slices = 0;
            int writtenSize = 0;
            int offset = 0;

            DoForEachSlice(request, (level, slice, _, _) =>
            {
                int sliceSize = (importedWidth | importedHeight) != 0 ? Math.Max(1, importedWidth >> level) * Math.Max(1, importedHeight >> level) * 4 : 0;

                bool imported = false;
                string fileName = BuildFileName(request, level, slice, "png");

                foreach (string inputDirectoryPath in _importList)
                {
                    string inputFileName = Path.Combine(inputDirectoryPath, fileName);

                    if (File.Exists(inputFileName))
                    {
                        _fileToTextureMap.AddOrUpdate(fileName, texture, (key, old) => texture);

                        byte[] imageFile = null;

                        try
                        {
                            imageFile = File.ReadAllBytes(inputFileName);
                        }
                        catch (IOException ex)
                        {
                            LogReadException(ex, inputFileName);
                            break;
                        }

                        ImageLoadResult loadResult = PngFileFormat.TryLoadHeader(imageFile, out ImageParameters parameters);

                        if (loadResult != ImageLoadResult.Success)
                        {
                            LogFailureResult(loadResult, inputFileName);
                            break;
                        }

                        int importedSizeWL = Math.Max(1, importedWidth >> level);
                        int importedSizeHL = Math.Max(1, importedHeight >> level);

                        if (writtenSize == 0 || (importedSizeWL == parameters.Width && importedSizeHL == parameters.Height))
                        {
                            if (writtenSize == 0)
                            {
                                importedFirstLevel = level;
                                importedWidth = parameters.Width << level;
                                importedHeight = parameters.Height << level;
                                sliceSize = Math.Max(1, importedWidth >> level) * Math.Max(1, importedHeight >> level) * 4;
                                buffer = MemoryOwner<byte>.Rent(CalculateSize(importedWidth, importedHeight, request.Depth, request.Layers, request.Levels));
                            }

                            loadResult = PngFileFormat.TryLoadData(imageFile, buffer.Span.Slice(offset, sliceSize));

                            if (loadResult != ImageLoadResult.Success)
                            {
                                LogFailureResult(loadResult, inputFileName);
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }

                        imported = true;
                        break;
                    }
                }

                if (imported)
                {
                    levels = level + 1;

                    if (level == importedFirstLevel)
                    {
                        slices = slice + 1;
                    }

                    writtenSize = offset + sliceSize;
                }

                offset += sliceSize;

                return imported;
            });

            if (writtenSize == 0)
            {
                return null;
            }

            if (writtenSize == buffer.Length)
            {
                cachedData = buffer;
            }
            else
            {
                using (buffer)
                {
                    cachedData = MemoryOwner<byte>.RentCopy(buffer.Span[..writtenSize]);
                }
            }

            Format format;

            if (IsSupportedSnormFormat(request.Format))
            {
                format = Format.R8G8B8A8Snorm;
            }
            else if (IsSupportedSrgbFormat(request.Format))
            {
                format = Format.R8G8B8A8Srgb;
            }
            else
            {
                format = Format.R8G8B8A8Unorm;
            }

            return new TextureInfoOverride(
                importedWidth,
                importedHeight,
                slices,
                levels,
                new FormatInfo(format, 1, 1, 4, 4));
        }

        private static int CalculateSize(int width, int height, int depth, int layers, int levels)
        {
            int size = 0;

            for (int level = 0; level < levels; level++)
            {
                int w = Math.Max(1, width >> level);
                int h = Math.Max(1, height >> level);
                int d = Math.Max(1, depth >> level);
                int sliceSize = w * h * 4;

                size += sliceSize * layers * d;
            }

            return size;
        }

        internal void EnqueueTextureDataForExport(Texture texture, byte[] data)
        {
            if (_enableTextureDump && !string.IsNullOrEmpty(_outputDirectoryPath))
            {
                _exportQueue.Add((texture, new(
                    texture.Width,
                    texture.Height,
                    texture.Depth,
                    texture.Layers,
                    texture.Info.Levels,
                    texture.Format,
                    texture.Target,
                    data)));
            }
        }

        private void ExportTexture((Texture, TextureRequest) tuple)
        {
            if (_outputFormat == FileFormat.Png)
            {
                ExportPngTexturePerSlice(tuple.Item1, tuple.Item2);
            }
            else
            {
                ExportDdsTexture(tuple.Item1, tuple.Item2);
            }
        }

        private void ExportDdsTexture(Texture texture, TextureRequest request)
        {
            if (!TryGetDimensions(request.Target, out ImageDimensions imageDimensions))
            {
                return;
            }

            MemoryOwner<byte> dataOwner = null;
            ReadOnlySpan<byte> data = request.Data;

            ImageFormat imageFormat = GetFormat(request.Format);

            if (imageFormat == ImageFormat.Unknown)
            {
                dataOwner = ConvertFormatToRgba8(request);
                if (dataOwner == null)
                {
                    return;
                }

                data = dataOwner.Span;
                imageFormat = IsSupportedSrgbFormat(request.Format) ? ImageFormat.R8G8B8A8Srgb : ImageFormat.R8G8B8A8Unorm;
            }

            string fileName = BuildFileName(request, "dds");
            string outputFileName = Path.Combine(_outputDirectoryPath, fileName);

            using ScopedLoadLock loadLock = new(this, outputFileName);

            _fileToTextureMap.TryAdd(fileName, texture);

            ImageParameters parameters = new(
                request.Width,
                request.Height,
                request.Depth * request.Layers,
                request.Levels,
                imageFormat,
                imageDimensions);

            try
            {
                using FileStream fs = new(outputFileName, FileMode.Create);
                DdsFileFormat.Save(fs, parameters, data);
            }
            catch (IOException ex)
            {
                LogWriteException(ex, outputFileName);
                loadLock.Cancel();
            }
            finally
            {
                dataOwner?.Dispose();
            }
        }

        private void ExportPngTexturePerSlice(Texture texture, TextureRequest request)
        {
            using MemoryOwner<byte> data = ConvertFormatToRgba8(request);
            if (data == null)
            {
                return;
            }

            DoForEachSlice(request, (level, slice, offset, sliceSize) =>
            {
                ReadOnlySpan<byte> buffer = data.Span;

                int w = Math.Max(1, request.Width >> level);
                int h = Math.Max(1, request.Height >> level);

                string fileName = BuildFileName(request, level, slice, "png");
                string outputFileName = Path.Combine(_outputDirectoryPath, fileName);

                using ScopedLoadLock loadLock = new(this, outputFileName);

                _fileToTextureMap.TryAdd(fileName, texture);

                ImageParameters parameters = new(w, h, 1, 1, ImageFormat.R8G8B8A8Unorm, ImageDimensions.Dim2D);

                try
                {
                    using FileStream fs = new(outputFileName, FileMode.Create);
                    PngFileFormat.Save(fs, parameters, buffer.Slice(offset, sliceSize), fastMode: true);
                }
                catch (IOException ex)
                {
                    LogWriteException(ex, outputFileName);
                    loadLock.Cancel();
                    return false;
                }

                return true;
            });
        }

        private static bool TryGetFileTimestamp(string fileName, out long timestamp)
        {
            try
            {
                DateTime time = File.GetLastWriteTimeUtc(fileName);
                timestamp = ((DateTimeOffset)time).ToUnixTimeSeconds();
                return true;
            }
            catch (Exception)
            {
                timestamp = 0;
                return false;
            }
        }

        private static void DoForEachSlice(in TextureRequest request, Func<int, int, int, int, bool> callback)
        {
            bool is3D = request.Depth > 1;
            int offset = 0;

            for (int level = 0; level < request.Levels; level++)
            {
                int w = Math.Max(1, request.Width >> level);
                int h = Math.Max(1, request.Height >> level);
                int d = is3D ? Math.Max(1, request.Depth >> level) : request.Layers;
                int sliceSize = w * h * 4;

                for (int slice = 0; slice < d; slice++)
                {
                    if (!callback(level, slice, offset, sliceSize))
                    {
                        break;
                    }

                    offset += sliceSize;
                }
            }
        }

        private static string BuildFileName(TextureRequest request, string extension)
        {
            int w = request.Width;
            int h = request.Height;
            int d = request.Depth * request.Layers;
            string hash = ComputeHash(request.Data);
            return $"{GetNamePrefix(request.Target)}_{hash}_{w}x{h}x{d}.{extension}";
        }

        private static string BuildFileName(TextureRequest request, int level, int slice, string extension)
        {
            int w = request.Width;
            int h = request.Height;
            int d = request.Depth * request.Layers;
            string hash = ComputeHash(request.Data);
            return $"{GetNamePrefix(request.Target)}_{hash}_{w}x{h}x{d}_{level}x{slice}.{extension}";
        }

        private static string GetNamePrefix(Target target)
        {
            return target switch
            {
                Target.Texture2D => "tex2d",
                Target.Texture2DArray => "texa2d",
                Target.Texture3D => "tex3d",
                Target.Cubemap => "texcube",
                Target.CubemapArray => "texacube",
                _ => "tex",
            };
        }

        private static string ComputeHash(byte[] data)
        {
            Hash128 hash = XXHash128.ComputeHash(data);
            return $"{hash.High:x16}{hash.Low:x16}";
        }

        private static ImageFormat GetFormat(Format format)
        {
            return format switch
            {
                Format.Bc1RgbaSrgb => ImageFormat.Bc1RgbaSrgb,
                Format.Bc1RgbaUnorm => ImageFormat.Bc1RgbaUnorm,
                Format.Bc2Srgb => ImageFormat.Bc2Srgb,
                Format.Bc2Unorm => ImageFormat.Bc2Unorm,
                Format.Bc3Srgb => ImageFormat.Bc3Srgb,
                Format.Bc3Unorm => ImageFormat.Bc3Unorm,
                Format.Bc4Snorm => ImageFormat.Bc4Snorm,
                Format.Bc4Unorm => ImageFormat.Bc4Unorm,
                Format.Bc5Snorm => ImageFormat.Bc5Snorm,
                Format.Bc5Unorm => ImageFormat.Bc5Unorm,
                Format.Bc7Srgb => ImageFormat.Bc7Srgb,
                Format.Bc7Unorm => ImageFormat.Bc7Unorm,
                Format.R8Unorm => ImageFormat.R8Unorm,
                Format.R8G8Unorm => ImageFormat.R8G8Unorm,
                Format.R8G8B8A8Srgb => ImageFormat.R8G8B8A8Srgb,
                Format.R8G8B8A8Unorm => ImageFormat.R8G8B8A8Unorm,
                Format.R5G6B5Unorm => ImageFormat.R5G6B5Unorm,
                Format.R5G5B5A1Unorm => ImageFormat.R5G5B5A1Unorm,
                Format.R4G4B4A4Unorm => ImageFormat.R4G4B4A4Unorm,
                _ => ImageFormat.Unknown,
            };
        }

        private static bool TryGetDimensions(Target target, out ImageDimensions imageDimensions)
        {
            switch (target)
            {
                case Target.Texture2D:
                    imageDimensions = ImageDimensions.Dim2D;
                    return true;
                case Target.Texture2DArray:
                    imageDimensions = ImageDimensions.Dim2DArray;
                    return true;
                case Target.Texture3D:
                    imageDimensions = ImageDimensions.Dim3D;
                    return true;
                case Target.Cubemap:
                    imageDimensions = ImageDimensions.DimCube;
                    return true;
                case Target.CubemapArray:
                    imageDimensions = ImageDimensions.DimCubeArray;
                    return true;
            }

            imageDimensions = default;
            return false;
        }

        private static MemoryOwner<byte> ConvertFormatToRgba8(in TextureRequest request)
        {
            byte[] data = request.Data;
            int width = request.Width;
            int height = request.Height;
            int depth = request.Depth;
            int layers = request.Layers;
            int levels = request.Levels;

            switch (request.Format)
            {
                case Format.Astc4x4Srgb:
                case Format.Astc4x4Unorm:
                    return DecodeAstc(data, 4, 4, width, height, depth, levels, layers);
                case Format.Astc5x4Srgb:
                case Format.Astc5x4Unorm:
                    return DecodeAstc(data, 5, 4, width, height, depth, levels, layers);
                case Format.Astc5x5Srgb:
                case Format.Astc5x5Unorm:
                    return DecodeAstc(data, 5, 5, width, height, depth, levels, layers);
                case Format.Astc6x5Srgb:
                case Format.Astc6x5Unorm:
                    return DecodeAstc(data, 6, 5, width, height, depth, levels, layers);
                case Format.Astc6x6Srgb:
                case Format.Astc6x6Unorm:
                    return DecodeAstc(data, 6, 6, width, height, depth, levels, layers);
                case Format.Astc8x5Srgb:
                case Format.Astc8x5Unorm:
                    return DecodeAstc(data, 8, 5, width, height, depth, levels, layers);
                case Format.Astc8x6Srgb:
                case Format.Astc8x6Unorm:
                    return DecodeAstc(data, 8, 6, width, height, depth, levels, layers);
                case Format.Astc8x8Srgb:
                case Format.Astc8x8Unorm:
                    return DecodeAstc(data, 8, 8, width, height, depth, levels, layers);
                case Format.Astc10x5Srgb:
                case Format.Astc10x5Unorm:
                    return DecodeAstc(data, 10, 5, width, height, depth, levels, layers);
                case Format.Astc10x6Srgb:
                case Format.Astc10x6Unorm:
                    return DecodeAstc(data, 10, 6, width, height, depth, levels, layers);
                case Format.Astc10x8Srgb:
                case Format.Astc10x8Unorm:
                    return DecodeAstc(data, 10, 8, width, height, depth, levels, layers);
                case Format.Astc10x10Srgb:
                case Format.Astc10x10Unorm:
                    return DecodeAstc(data, 10, 10, width, height, depth, levels, layers);
                case Format.Astc12x10Srgb:
                case Format.Astc12x10Unorm:
                    return DecodeAstc(data, 12, 10, width, height, depth, levels, layers);
                case Format.Astc12x12Srgb:
                case Format.Astc12x12Unorm:
                    return DecodeAstc(data, 12, 12, width, height, depth, levels, layers);
                case Format.Bc1RgbaSrgb:
                case Format.Bc1RgbaUnorm:
                    return BCnDecoder.DecodeBC1(data, width, height, depth, levels, layers);
                case Format.Bc2Srgb:
                case Format.Bc2Unorm:
                    return BCnDecoder.DecodeBC2(data, width, height, depth, levels, layers);
                case Format.Bc3Srgb:
                case Format.Bc3Unorm:
                    return BCnDecoder.DecodeBC3(data, width, height, depth, levels, layers);
                case Format.Bc4Snorm:
                case Format.Bc4Unorm:
                    using (MemoryOwner<byte> decoded = BCnDecoder.DecodeBC4(data, width, height, depth, levels, layers, request.Format == Format.Bc4Snorm))
                    {
                        return ConvertRToRgba(decoded.Span, request);
                    }
                case Format.Bc5Snorm:
                case Format.Bc5Unorm:
                    using (MemoryOwner<byte> decoded = BCnDecoder.DecodeBC5(data, width, height, depth, levels, layers, request.Format == Format.Bc5Snorm))
                    {
                        return ConvertRgToRgba(decoded.Span, request);
                    }
                case Format.Bc7Srgb:
                case Format.Bc7Unorm:
                    return BCnDecoder.DecodeBC7(data, width, height, depth, levels, layers);
                case Format.Etc2RgbaSrgb:
                case Format.Etc2RgbaUnorm:
                    return ETC2Decoder.DecodeRgba(data, width, height, depth, levels, layers);
                case Format.Etc2RgbSrgb:
                case Format.Etc2RgbUnorm:
                    return ETC2Decoder.DecodeRgb(data, width, height, depth, levels, layers);
                case Format.Etc2RgbPtaSrgb:
                case Format.Etc2RgbPtaUnorm:
                    return ETC2Decoder.DecodePta(data, width, height, depth, levels, layers);
                case Format.R8Unorm:
                    return ConvertRToRgba(request.Data, request);
                case Format.R8G8Unorm:
                    return ConvertRgToRgba(request.Data, request);
                case Format.R8G8B8A8Unorm:
                case Format.R8G8B8A8Srgb:
                    return MemoryOwner<byte>.RentCopy(request.Data);
                case Format.B5G6R5Unorm:
                case Format.R5G6B5Unorm:
                    return PixelConverter.ConvertR5G6B5ToR8G8B8A8(data, width);
                case Format.B5G5R5A1Unorm:
                case Format.R5G5B5X1Unorm:
                case Format.R5G5B5A1Unorm:
                    return PixelConverter.ConvertR5G5B5ToR8G8B8A8(data, width, request.Format == Format.R5G5B5X1Unorm);
                case Format.A1B5G5R5Unorm:
                    return PixelConverter.ConvertA1B5G5R5ToR8G8B8A8(data, width);
                case Format.R4G4B4A4Unorm:
                    return PixelConverter.ConvertR4G4B4A4ToR8G8B8A8(data, width);
            }

            return null;
        }

        private static MemoryOwner<byte> DecodeAstc(byte[] data, int blockWidth, int blockHeight, int width, int height, int depth, int levels, int layers)
        {
            AstcDecoder.TryDecodeToRgba8P(
                data,
                blockWidth,
                blockHeight,
                width,
                height,
                depth,
                levels,
                layers,
                out MemoryOwner<byte> decoded);

            return decoded;
        }

        private static void ConvertBgraToRgbaInPlace(Span<byte> buffer)
        {
            for (int i = 0; i < buffer.Length; i += 4)
            {
                (buffer[i + 2], buffer[i]) = (buffer[i], buffer[i + 2]);
            }
        }

        private static MemoryOwner<byte> ConvertRToRgba(ReadOnlySpan<byte> input, in TextureRequest request)
        {
            MemoryOwner<byte> output = MemoryOwner<byte>.Rent(CalculateSize(request, 4));

            int srcBaseOffset = 0;
            int dstBaseOffset = 0;

            for (int l = 0; l < request.Levels; l++)
            {
                int w = Math.Max(1, request.Width >> l);
                int h = Math.Max(1, request.Height >> l);
                int d = Math.Max(1, request.Depth >> l) * request.Layers;

                int stride = w;
                int strideAligned = (stride + StrideAlignment - 1) & -StrideAlignment;
                int rows = h * d;

                for (int i = 0; i < rows; i++)
                {
                    int dstOffset = dstBaseOffset + i * stride * 4;
                    int srcOffset = srcBaseOffset + i * strideAligned;

                    for (int j = 0; j < stride; j++)
                    {
                        output.Span[dstOffset + j * 4] = input[srcOffset + j];
                        output.Span[dstOffset + j * 4 + 3] = 0xff;
                    }
                }

                dstBaseOffset += rows * stride * 4;
                srcBaseOffset += rows * strideAligned;
            }

            return output;
        }

        private static MemoryOwner<byte> ConvertRgToRgba(ReadOnlySpan<byte> input, in TextureRequest request)
        {
            MemoryOwner<byte> output = MemoryOwner<byte>.Rent(CalculateSize(request, 4));

            int srcBaseOffset = 0;
            int dstBaseOffset = 0;

            for (int l = 0; l < request.Levels; l++)
            {
                int w = Math.Max(1, request.Width >> l);
                int h = Math.Max(1, request.Height >> l);
                int d = Math.Max(1, request.Depth >> l) * request.Layers;

                int stride = w * 2;
                int strideAligned = (stride + StrideAlignment - 1) & -StrideAlignment;
                int rows = h * d;

                for (int i = 0; i < rows; i++)
                {
                    int dstOffset = dstBaseOffset + i * stride * 2;
                    int srcOffset = srcBaseOffset + i * strideAligned;

                    for (int j = 0; j < stride; j += 2)
                    {
                        output.Span[dstOffset + j * 2] = input[srcOffset + j];
                        output.Span[dstOffset + j * 2 + 1] = input[srcOffset + j + 1];
                        output.Span[dstOffset + j * 2 + 3] = 0xff;
                    }
                }

                dstBaseOffset += rows * stride * 2;
                srcBaseOffset += rows * strideAligned;
            }

            return output;
        }

        private static int CalculateSize(in TextureRequest request, int bpp)
        {
            int size = 0;

            for (int l = 0; l < request.Levels; l++)
            {
                int w = Math.Max(1, request.Width >> l);
                int h = Math.Max(1, request.Height >> l);
                int d = Math.Max(1, request.Depth >> l) * request.Layers;

                size += w * h * d;
            }

            return size * bpp;
        }

        private static bool IsSupportedFormat(Format format)
        {
            switch (format)
            {
                case Format.Astc4x4Srgb:
                case Format.Astc4x4Unorm:
                case Format.Astc5x4Srgb:
                case Format.Astc5x4Unorm:
                case Format.Astc5x5Srgb:
                case Format.Astc5x5Unorm:
                case Format.Astc6x5Srgb:
                case Format.Astc6x5Unorm:
                case Format.Astc6x6Srgb:
                case Format.Astc6x6Unorm:
                case Format.Astc8x5Srgb:
                case Format.Astc8x5Unorm:
                case Format.Astc8x6Srgb:
                case Format.Astc8x6Unorm:
                case Format.Astc8x8Srgb:
                case Format.Astc8x8Unorm:
                case Format.Astc10x5Srgb:
                case Format.Astc10x5Unorm:
                case Format.Astc10x6Srgb:
                case Format.Astc10x6Unorm:
                case Format.Astc10x8Srgb:
                case Format.Astc10x8Unorm:
                case Format.Astc10x10Srgb:
                case Format.Astc10x10Unorm:
                case Format.Astc12x10Srgb:
                case Format.Astc12x10Unorm:
                case Format.Astc12x12Srgb:
                case Format.Astc12x12Unorm:
                case Format.Bc1RgbaSrgb:
                case Format.Bc1RgbaUnorm:
                case Format.Bc2Srgb:
                case Format.Bc2Unorm:
                case Format.Bc3Srgb:
                case Format.Bc3Unorm:
                case Format.Bc4Unorm:
                case Format.Bc5Snorm:
                case Format.Bc5Unorm:
                case Format.Bc7Srgb:
                case Format.Bc7Unorm:
                case Format.Etc2RgbaSrgb:
                case Format.Etc2RgbaUnorm:
                case Format.Etc2RgbSrgb:
                case Format.Etc2RgbUnorm:
                case Format.Etc2RgbPtaSrgb:
                case Format.Etc2RgbPtaUnorm:
                case Format.R8Unorm:
                case Format.R8G8Unorm:
                case Format.R8G8B8A8Unorm:
                case Format.R8G8B8A8Srgb:
                case Format.B5G6R5Unorm:
                case Format.R5G6B5Unorm:
                case Format.B5G5R5A1Unorm:
                case Format.R5G5B5X1Unorm:
                case Format.R5G5B5A1Unorm:
                case Format.A1B5G5R5Unorm:
                case Format.R4G4B4A4Unorm:
                    return true;
            }

            return false;
        }

        private static bool IsSupportedSrgbFormat(Format format)
        {
            switch (format)
            {
                case Format.Astc4x4Srgb:
                case Format.Astc5x4Srgb:
                case Format.Astc5x5Srgb:
                case Format.Astc6x5Srgb:
                case Format.Astc6x6Srgb:
                case Format.Astc8x5Srgb:
                case Format.Astc8x6Srgb:
                case Format.Astc8x8Srgb:
                case Format.Astc10x5Srgb:
                case Format.Astc10x6Srgb:
                case Format.Astc10x8Srgb:
                case Format.Astc10x10Srgb:
                case Format.Astc12x10Srgb:
                case Format.Astc12x12Srgb:
                case Format.Bc1RgbaSrgb:
                case Format.Bc2Srgb:
                case Format.Bc3Srgb:
                case Format.Bc7Srgb:
                case Format.Etc2RgbaSrgb:
                case Format.Etc2RgbSrgb:
                case Format.Etc2RgbPtaSrgb:
                case Format.R8G8B8A8Srgb:
                    return true;
            }

            return false;
        }

        private static bool IsSupportedSnormFormat(Format format)
        {
            return format == Format.Bc5Snorm;
        }

        private static FormatInfo ConvertToFormat(ImageFormat format)
        {
            return format switch
            {
                ImageFormat.Bc1RgbaSrgb => new FormatInfo(Format.Bc1RgbaSrgb, 4, 4, 8, 4),
                ImageFormat.Bc1RgbaUnorm => new FormatInfo(Format.Bc1RgbaUnorm, 4, 4, 8, 4),
                ImageFormat.Bc2Srgb => new FormatInfo(Format.Bc2Srgb, 4, 4, 16, 4),
                ImageFormat.Bc2Unorm => new FormatInfo(Format.Bc2Unorm, 4, 4, 16, 4),
                ImageFormat.Bc3Srgb => new FormatInfo(Format.Bc3Srgb, 4, 4, 16, 4),
                ImageFormat.Bc3Unorm => new FormatInfo(Format.Bc3Unorm, 4, 4, 16, 4),
                ImageFormat.Bc4Snorm => new FormatInfo(Format.Bc4Snorm, 4, 4, 8, 1),
                ImageFormat.Bc4Unorm => new FormatInfo(Format.Bc4Unorm, 4, 4, 8, 1),
                ImageFormat.Bc5Snorm => new FormatInfo(Format.Bc5Snorm, 4, 4, 16, 2),
                ImageFormat.Bc5Unorm => new FormatInfo(Format.Bc5Unorm, 4, 4, 16, 2),
                ImageFormat.Bc7Srgb => new FormatInfo(Format.Bc7Srgb, 4, 4, 16, 4),
                ImageFormat.Bc7Unorm => new FormatInfo(Format.Bc7Unorm, 4, 4, 16, 4),
                ImageFormat.R8Unorm => new FormatInfo(Format.R8Unorm, 1, 1, 1, 1),
                ImageFormat.R8G8Unorm => new FormatInfo(Format.R8G8Unorm, 1, 1, 2, 2),
                ImageFormat.R8G8B8A8Srgb or ImageFormat.B8G8R8A8Srgb => new FormatInfo(Format.R8G8B8A8Srgb, 1, 1, 4, 4),
                ImageFormat.R8G8B8A8Unorm or ImageFormat.B8G8R8A8Unorm => new FormatInfo(Format.R8G8B8A8Unorm, 1, 1, 4, 4),
                ImageFormat.R5G6B5Unorm => new FormatInfo(Format.R5G6B5Unorm, 1, 1, 2, 3),
                ImageFormat.R5G5B5A1Unorm => new FormatInfo(Format.R5G5B5A1Unorm, 1, 1, 2, 4),
                ImageFormat.R4G4B4A4Unorm => new FormatInfo(Format.R4G4B4A4Unorm, 1, 1, 2, 4),
                _ => throw new ArgumentException($"Invalid format {format}."),
            };
        }

        private static void LogFailureResult(ImageLoadResult result, string fullPath)
        {
            string fileName = Path.GetFileName(fullPath);

            switch (result)
            {
                case ImageLoadResult.CorruptedHeader:
                    Logger.Error?.Print(LogClass.Gpu, $"Failed to load \"{fileName}\" because the file header is corrupted.");
                    break;
                case ImageLoadResult.CorruptedData:
                    Logger.Error?.Print(LogClass.Gpu, $"Failed to load \"{fileName}\" because the file data is corrupted.");
                    break;
                case ImageLoadResult.DataTooShort:
                    Logger.Error?.Print(LogClass.Gpu, $"Failed to load \"{fileName}\" because some data is missing from the file.");
                    break;
                case ImageLoadResult.OutputTooShort:
                    Logger.Error?.Print(LogClass.Gpu, $"Failed to load \"{fileName}\" because the output buffer was not large enough.");
                    break;
                case ImageLoadResult.UnsupportedFormat:
                    Logger.Error?.Print(LogClass.Gpu, $"Failed to load \"{fileName}\" because the image format is not currently supported.");
                    break;
            }
        }

        private static void LogReadException(IOException exception, string fullPath)
        {
            Logger.Error?.Print(LogClass.Gpu, exception.ToString());

            string fileName = Path.GetFileName(fullPath);

            Logger.Error?.Print(LogClass.Gpu, $"Failed to load \"{fileName}\", see logged exception for details.");
        }

        private static void LogWriteException(IOException exception, string fullPath)
        {
            Logger.Error?.Print(LogClass.Gpu, exception.ToString());

            string fileName = Path.GetFileName(fullPath);

            Logger.Error?.Print(LogClass.Gpu, $"Failed to save \"{fileName}\", see logged exception for details.");
        }

        private static void LogDirCreationException(Exception exception, string fullPath)
        {
            Logger.Error?.Print(LogClass.Gpu, exception.ToString());
            Logger.Error?.Print(LogClass.Gpu, $"Failed to create a directory on path \"{fullPath}\", see logged exception for details.");
        }
    }
}
