using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// On-disk shader cache storage for host code.
    /// </summary>
    class DiskCacheHostStorage
    {
        private const uint TocsMagic = (byte)'T' | ((byte)'O' << 8) | ((byte)'C' << 16) | ((byte)'S' << 24);
        private const uint TochMagic = (byte)'T' | ((byte)'O' << 8) | ((byte)'C' << 16) | ((byte)'H' << 24);
        private const uint ShdiMagic = (byte)'S' | ((byte)'H' << 8) | ((byte)'D' << 16) | ((byte)'I' << 24);
        private const uint BufdMagic = (byte)'B' | ((byte)'U' << 8) | ((byte)'F' << 16) | ((byte)'D' << 24);
        private const uint TexdMagic = (byte)'T' | ((byte)'E' << 8) | ((byte)'X' << 16) | ((byte)'D' << 24);

        private const ushort FileFormatVersionMajor = 1;
        private const ushort FileFormatVersionMinor = 0;
        private const uint FileFormatVersionPacked = ((uint)FileFormatVersionMajor << 16) | FileFormatVersionMinor;
        private const uint CodeGenVersion = 0;

        private const string SharedTocFileName = "shared.toc";
        private const string SharedDataFileName = "shared.data";

        private readonly string _basePath;

        public bool CacheEnabled => !string.IsNullOrEmpty(_basePath);

        private struct TocHeader
        {
            public uint Magic;
            public uint FormatVersion;
            public uint CodeGenVersion;
            public uint Padding;
            public ulong Reserved;
            public ulong Reversed2;
        }

        private struct OffsetAndSize
        {
            public ulong Offset;
            public uint Size;
        }

        private struct DataEntryPerStage
        {
            public int GuestCodeIndex;
        }

        private struct DataEntry
        {
            public uint StagesBitMask;
        }

        private struct DataShaderInfo
        {
            public ushort CBuffersCount;
            public ushort SBuffersCount;
            public ushort TexturesCount;
            public ushort ImagesCount;
            public ShaderStage Stage;
            public bool UsesInstanceId;
            public bool UsesRtLayer;
            public byte ClipDistancesWritten;
            public int FragmentOutputMap;
        }

        private readonly DiskCacheGuestStorage _guestStorage;

        public DiskCacheHostStorage(string basePath)
        {
            _basePath = basePath;
            _guestStorage = new DiskCacheGuestStorage(basePath);
        }

        public int GetProgramCount()
        {
            string tocFilePath = Path.Combine(_basePath, SharedTocFileName);

            if (!File.Exists(tocFilePath))
            {
                return 0;
            }

            return (int)((new FileInfo(tocFilePath).Length - Unsafe.SizeOf<TocHeader>()) / sizeof(ulong));
        }

        private static string GetHostFileName(GpuContext context)
        {
            string apiName = context.Capabilities.Api.ToString().ToLowerInvariant();
            string vendorName = RemoveInvalidCharacters(context.Capabilities.VendorName.ToLowerInvariant());
            return $"{apiName}_{vendorName}";
        }

        private static string RemoveInvalidCharacters(string fileName)
        {
            int indexOfSpace = fileName.IndexOf(' ');
            if (indexOfSpace >= 0)
            {
                fileName = fileName.Substring(0, indexOfSpace);
            }

            return string.Concat(fileName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        }

        private static string GetHostTocFileName(GpuContext context)
        {
            return GetHostFileName(context) + ".toc";
        }

        private static string GetHostDataFileName(GpuContext context)
        {
            return GetHostFileName(context) + ".data";
        }

        public void LoadShaders(
            GpuContext context,
            ShaderCacheHashTable graphicsCache,
            ComputeShaderCacheHashTable computeCache,
            ParallelDiskCacheLoader loader)
        {
            string tocFilePath = Path.Combine(_basePath, SharedTocFileName);
            string dataFilePath = Path.Combine(_basePath, SharedDataFileName);

            if (!File.Exists(tocFilePath) || !File.Exists(dataFilePath) || !_guestStorage.TocFileExists() || !_guestStorage.DataFileExists())
            {
                return;
            }

            Stream hostTocFileStream = null;
            Stream hostDataFileStream = null;

            using var tocFileStream = DiskCacheCommon.OpenFile(_basePath, SharedTocFileName, writable: false);
            using var dataFileStream = DiskCacheCommon.OpenFile(_basePath, SharedDataFileName, writable: false);

            using var guestTocFileStream = _guestStorage.OpenTocFileStream();
            using var guestDataFileStream = _guestStorage.OpenDataFileStream();

            BinarySerialization tocReader = new BinarySerialization(tocFileStream);
            BinarySerialization dataReader = new BinarySerialization(dataFileStream);

            TocHeader header = new TocHeader();

            if (!tocReader.TryRead(ref header) || header.Magic != TocsMagic || header.FormatVersion != FileFormatVersionPacked)
            {
                return;
            }

            bool loadHostCache = header.CodeGenVersion == CodeGenVersion;

            int programIndex = 0;

            DataEntry entry = new DataEntry();

            while (tocFileStream.Position < tocFileStream.Length && loader.Active)
            {
                ulong dataOffset = 0;
                tocReader.TryRead(ref dataOffset);

                dataFileStream.Seek((long)dataOffset, SeekOrigin.Begin);

                dataReader.BeginCompression();
                dataReader.TryRead(ref entry);
                uint stagesBitMask = entry.StagesBitMask;

                bool isCompute = stagesBitMask == 0;
                if (isCompute)
                {
                    stagesBitMask = 1;
                }

                CachedShaderStage[] shaders = new CachedShaderStage[isCompute ? 1 : Constants.ShaderStages + 1];

                DataEntryPerStage stageEntry = new DataEntryPerStage();

                while (stagesBitMask != 0)
                {
                    int stageIndex = BitOperations.TrailingZeroCount(stagesBitMask);

                    dataReader.TryRead(ref stageEntry);

                    ShaderProgramInfo info = stageIndex != 0 || isCompute ? ReadShaderProgramInfo(ref dataReader) : null;

                    (byte[] guestCode, byte[] cb1Data) = _guestStorage.LoadShader(
                        guestTocFileStream,
                        guestDataFileStream,
                        stageEntry.GuestCodeIndex);

                    shaders[stageIndex] = new CachedShaderStage(info, guestCode, cb1Data);

                    stagesBitMask &= ~(1u << stageIndex);
                }

                ShaderSpecializationState specState = ShaderSpecializationState.Read(ref dataReader);
                dataReader.EndCompression();

                if (loadHostCache)
                {
                    byte[] hostCode = ReadHostCode(context, ref hostTocFileStream, ref hostDataFileStream, programIndex);

                    if (hostCode != null)
                    {
                        bool hasFragmentShader = shaders.Length > 5 && shaders[5] != null;
                        int fragmentOutputMap = hasFragmentShader ? shaders[5].Info.FragmentOutputMap : -1;
                        IProgram hostProgram = context.Renderer.LoadProgramBinary(hostCode, hasFragmentShader, new ShaderInfo(fragmentOutputMap));

                        CachedShaderProgram program = new CachedShaderProgram(hostProgram, specState, shaders);

                        loader.QueueHostProgram(program, hostProgram, programIndex, isCompute);
                    }
                    else
                    {
                        loadHostCache = false;
                    }
                }

                if (!loadHostCache)
                {
                    loader.QueueGuestProgram(shaders, specState, programIndex, isCompute);
                }

                loader.CheckCompilation();
                programIndex++;
            }

            _guestStorage.ClearMemoryCache();

            hostTocFileStream?.Dispose();
            hostDataFileStream?.Dispose();
        }

        private byte[] ReadHostCode(GpuContext context, ref Stream tocFileStream, ref Stream dataFileStream, int programIndex)
        {
            if (tocFileStream == null && dataFileStream == null)
            {
                string tocFilePath = Path.Combine(_basePath, GetHostTocFileName(context));
                string dataFilePath = Path.Combine(_basePath, GetHostDataFileName(context));

                if (!File.Exists(tocFilePath) || !File.Exists(dataFilePath))
                {
                    return null;
                }

                tocFileStream = DiskCacheCommon.OpenFile(_basePath, GetHostTocFileName(context), writable: false);
                dataFileStream = DiskCacheCommon.OpenFile(_basePath, GetHostDataFileName(context), writable: false);
            }

            int offset = Unsafe.SizeOf<TocHeader>() + programIndex * Unsafe.SizeOf<OffsetAndSize>();
            if (offset + Unsafe.SizeOf<OffsetAndSize>() > tocFileStream.Length)
            {
                return null;
            }

            tocFileStream.Seek(offset, SeekOrigin.Begin);

            BinarySerialization tocReader = new BinarySerialization(tocFileStream);

            OffsetAndSize offsetAndSize = new OffsetAndSize();
            tocReader.TryRead(ref offsetAndSize);

            dataFileStream.Seek((long)offsetAndSize.Offset, SeekOrigin.Begin);

            byte[] hostCode = new byte[offsetAndSize.Size];

            BinarySerialization.ReadCompressed(dataFileStream, hostCode);

            return hostCode;
        }

        public void AddShader(GpuContext context, CachedShaderProgram program, ReadOnlySpan<byte> hostCode)
        {
            uint stagesBitMask = 0;

            for (int index = 0; index < program.Shaders.Length; index++)
            {
                var shader = program.Shaders[index];
                if (shader == null || (shader.Info != null && shader.Info.Stage == ShaderStage.Compute))
                {
                    continue;
                }

                stagesBitMask |= 1u << index;
            }

            using var tocFileStream = DiskCacheCommon.OpenFile(_basePath, SharedTocFileName, writable: true);
            using var dataFileStream = DiskCacheCommon.OpenFile(_basePath, SharedDataFileName, writable: true);

            if (tocFileStream.Length == 0)
            {
                TocHeader header = new TocHeader();
                CreateToc(tocFileStream, ref header, TocsMagic, CodeGenVersion);
            }

            tocFileStream.Seek(0, SeekOrigin.End);
            dataFileStream.Seek(0, SeekOrigin.End);

            BinarySerialization tocWriter = new BinarySerialization(tocFileStream);
            BinarySerialization dataWriter = new BinarySerialization(dataFileStream);

            ulong dataOffset = (ulong)dataFileStream.Position;
            tocWriter.Write(ref dataOffset);

            DataEntry entry = new DataEntry();

            entry.StagesBitMask = stagesBitMask;

            dataWriter.BeginCompression(DiskCacheCommon.GetCompressionAlgorithm());
            dataWriter.Write(ref entry);

            DataEntryPerStage stageEntry = new DataEntryPerStage();

            for (int index = 0; index < program.Shaders.Length; index++)
            {
                var shader = program.Shaders[index];
                if (shader == null)
                {
                    continue;
                }

                stageEntry.GuestCodeIndex = _guestStorage.AddShader(shader.Code, shader.Cb1Data);

                dataWriter.Write(ref stageEntry);

                WriteShaderProgramInfo(ref dataWriter, shader.Info);
            }

            program.SpecializationState.Write(ref dataWriter);
            dataWriter.EndCompression();

            if (hostCode.IsEmpty)
            {
                return;
            }

            WriteHostCode(context, hostCode);
        }

        public void ClearHostCache(GpuContext context)
        {
            using var tocFileStream = DiskCacheCommon.OpenFile(_basePath, GetHostTocFileName(context), writable: true);
            using var dataFileStream = DiskCacheCommon.OpenFile(_basePath, GetHostDataFileName(context), writable: true);

            tocFileStream.SetLength(0);
            dataFileStream.SetLength(0);
        }

        public void AddHostShader(GpuContext context, ReadOnlySpan<byte> hostCode, int programIndex)
        {
            WriteHostCode(context, hostCode, programIndex);
        }

        private void WriteHostCode(GpuContext context, ReadOnlySpan<byte> hostCode, int programIndex = -1)
        {
            using var tocFileStream = DiskCacheCommon.OpenFile(_basePath, GetHostTocFileName(context), writable: true);
            using var dataFileStream = DiskCacheCommon.OpenFile(_basePath, GetHostDataFileName(context), writable: true);

            if (tocFileStream.Length == 0)
            {
                TocHeader header = new TocHeader();
                CreateToc(tocFileStream, ref header, TochMagic, 0);
            }

            if (programIndex == -1)
            {
                tocFileStream.Seek(0, SeekOrigin.End);
            }
            else
            {
                tocFileStream.Seek(Unsafe.SizeOf<TocHeader>() + (programIndex * Unsafe.SizeOf<OffsetAndSize>()), SeekOrigin.Begin);
            }

            dataFileStream.Seek(0, SeekOrigin.End);

            BinarySerialization tocWriter = new BinarySerialization(tocFileStream);

            OffsetAndSize offsetAndSize = new OffsetAndSize();
            offsetAndSize.Offset = (ulong)dataFileStream.Position;
            offsetAndSize.Size = (uint)hostCode.Length;
            tocWriter.Write(ref offsetAndSize);

            BinarySerialization.WriteCompressed(dataFileStream, hostCode, DiskCacheCommon.GetCompressionAlgorithm());
        }

        private void CreateToc(Stream tocFileStream, ref TocHeader header, uint magic, uint codegenVersion)
        {
            BinarySerialization writer = new BinarySerialization(tocFileStream);

            header.Magic = magic;
            header.FormatVersion = FileFormatVersionPacked;
            header.CodeGenVersion = codegenVersion;
            header.Padding = 0;
            header.Reserved = 0;
            header.Reversed2 = 0;

            if (tocFileStream.Length > 0)
            {
                tocFileStream.Seek(0, SeekOrigin.Begin);
                tocFileStream.SetLength(0);
            }

            writer.Write(ref header);
        }

        private static ShaderProgramInfo ReadShaderProgramInfo(ref BinarySerialization dataReader)
        {
            DataShaderInfo dataInfo = new DataShaderInfo();

            dataReader.ReadWithMagicAndSize(ref dataInfo, ShdiMagic);

            BufferDescriptor[] cBuffers = new BufferDescriptor[dataInfo.CBuffersCount];
            BufferDescriptor[] sBuffers = new BufferDescriptor[dataInfo.SBuffersCount];
            TextureDescriptor[] textures = new TextureDescriptor[dataInfo.TexturesCount];
            TextureDescriptor[] images = new TextureDescriptor[dataInfo.ImagesCount];

            for (int index = 0; index < dataInfo.CBuffersCount; index++)
            {
                dataReader.ReadWithMagicAndSize(ref cBuffers[index], BufdMagic);
            }

            for (int index = 0; index < dataInfo.SBuffersCount; index++)
            {
                dataReader.ReadWithMagicAndSize(ref sBuffers[index], BufdMagic);
            }

            for (int index = 0; index < dataInfo.TexturesCount; index++)
            {
                dataReader.ReadWithMagicAndSize(ref textures[index], TexdMagic);
            }

            for (int index = 0; index < dataInfo.ImagesCount; index++)
            {
                dataReader.ReadWithMagicAndSize(ref images[index], TexdMagic);
            }

            return new ShaderProgramInfo(
                cBuffers,
                sBuffers,
                textures,
                images,
                dataInfo.Stage,
                dataInfo.UsesInstanceId,
                dataInfo.UsesRtLayer,
                dataInfo.ClipDistancesWritten,
                dataInfo.FragmentOutputMap);
        }

        private static void WriteShaderProgramInfo(ref BinarySerialization dataWriter, ShaderProgramInfo info)
        {
            if (info == null)
            {
                return;
            }

            DataShaderInfo dataInfo = new DataShaderInfo();

            dataInfo.CBuffersCount = (ushort)info.CBuffers.Count;
            dataInfo.SBuffersCount = (ushort)info.SBuffers.Count;
            dataInfo.TexturesCount = (ushort)info.Textures.Count;
            dataInfo.ImagesCount = (ushort)info.Images.Count;
            dataInfo.Stage = info.Stage;
            dataInfo.UsesInstanceId = info.UsesInstanceId;
            dataInfo.UsesRtLayer = info.UsesRtLayer;
            dataInfo.ClipDistancesWritten = info.ClipDistancesWritten;
            dataInfo.FragmentOutputMap = info.FragmentOutputMap;

            dataWriter.WriteWithMagicAndSize(ref dataInfo, ShdiMagic);

            for (int index = 0; index < info.CBuffers.Count; index++)
            {
                var entry = info.CBuffers[index];
                dataWriter.WriteWithMagicAndSize(ref entry, BufdMagic);
            }

            for (int index = 0; index < info.SBuffers.Count; index++)
            {
                var entry = info.SBuffers[index];
                dataWriter.WriteWithMagicAndSize(ref entry, BufdMagic);
            }

            for (int index = 0; index < info.Textures.Count; index++)
            {
                var entry = info.Textures[index];
                dataWriter.WriteWithMagicAndSize(ref entry, TexdMagic);
            }

            for (int index = 0; index < info.Images.Count; index++)
            {
                var entry = info.Images[index];
                dataWriter.WriteWithMagicAndSize(ref entry, TexdMagic);
            }
        }
    }
}