using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Texture.FileFormats
{
    public static class PngFileFormat
    {
        private const int ChunkOverheadSize = 12;
        private const int MaxIdatChunkSize = 0x2000;

        private static readonly uint[] _crcTable;

        static PngFileFormat()
        {
            _crcTable = new uint[256];

            uint c;

            for (int n = 0; n < _crcTable.Length; n++)
            {
                c = (uint)n;

                for (int k = 0; k < 8; k++)
                {
                    if ((c & 1) != 0)
                    {
                        c = 0xedb88320 ^ (c >> 1);
                    }
                    else
                    {
                        c >>= 1;
                    }
                }

                _crcTable[n] = c;
            }
        }

        private ref struct PngChunk
        {
            public uint ChunkType;
            public ReadOnlySpan<byte> Data;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PngHeader
        {
            public int Width;
            public int Height;
            public byte BitDepth;
            public byte ColorType;
            public byte CompressionMethod;
            public byte FilterMethod;
            public byte InterlaceMethod;
        }

        private enum FilterType : byte
        {
            None = 0,
            Sub = 1,
            Up = 2,
            Average = 3,
            Paeth = 4,
        }

        private const uint IhdrMagic = ((byte)'I' << 24) | ((byte)'H' << 16) | ((byte)'D' << 8) | (byte)'R';
        private const uint PlteMagic = ((byte)'P' << 24) | ((byte)'L' << 16) | ((byte)'T' << 8) | (byte)'E';
        private const uint IdatMagic = ((byte)'I' << 24) | ((byte)'D' << 16) | ((byte)'A' << 8) | (byte)'T';
        private const uint IendMagic = ((byte)'I' << 24) | ((byte)'E' << 16) | ((byte)'N' << 8) | (byte)'D';

        private static readonly byte[] _pngSignature = new byte[]
        {
            0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a,
        };

        public static ImageLoadResult TryLoadHeader(ReadOnlySpan<byte> pngData, out ImageParameters parameters)
        {
            parameters = default;

            if (pngData.Length < 8)
            {
                return ImageLoadResult.DataTooShort;
            }

            if (!pngData[..8].SequenceEqual(_pngSignature))
            {
                return ImageLoadResult.CorruptedHeader;
            }

            pngData = pngData[8..];

            ImageLoadResult result = TryParseChunk(pngData, out PngChunk ihdrChunk);

            if (result != ImageLoadResult.Success)
            {
                return result;
            }

            if (ihdrChunk.ChunkType != IhdrMagic)
            {
                return ImageLoadResult.CorruptedHeader;
            }

            if (ihdrChunk.Data.Length < Unsafe.SizeOf<PngHeader>())
            {
                return ImageLoadResult.DataTooShort;
            }

            PngHeader header = MemoryMarshal.Cast<byte, PngHeader>(ihdrChunk.Data)[0];

            if (!ValidateHeader(header))
            {
                return ImageLoadResult.CorruptedHeader;
            }

            parameters = new(
                ReverseEndianness(header.Width),
                ReverseEndianness(header.Height),
                1,
                1,
                ImageFormat.R8G8B8A8Unorm,
                ImageDimensions.Dim2D);

            return ImageLoadResult.Success;
        }

        public static ImageLoadResult TryLoadData(ReadOnlySpan<byte> pngData, Span<byte> output)
        {
            if (pngData.Length < 8)
            {
                return ImageLoadResult.DataTooShort;
            }

            if (!pngData[..8].SequenceEqual(_pngSignature))
            {
                return ImageLoadResult.CorruptedHeader;
            }

            pngData = pngData[8..];

            ImageLoadResult result = TryParseChunk(pngData, out PngChunk ihdrChunk);

            if (result != ImageLoadResult.Success)
            {
                return result;
            }

            if (ihdrChunk.ChunkType != IhdrMagic)
            {
                return ImageLoadResult.CorruptedHeader;
            }

            if (ihdrChunk.Data.Length < Unsafe.SizeOf<PngHeader>())
            {
                return ImageLoadResult.DataTooShort;
            }

            PngHeader header = MemoryMarshal.Cast<byte, PngHeader>(ihdrChunk.Data)[0];

            if (!ValidateHeader(header))
            {
                return ImageLoadResult.CorruptedHeader;
            }

            // We currently don't support Adam7 interlaced images.
            if (header.InterlaceMethod != 0)
            {
                return ImageLoadResult.UnsupportedFormat;
            }

            // Make sure the output can fit the data.
            if (output.Length < ReverseEndianness(header.Width) * ReverseEndianness(header.Height) * 4)
            {
                return ImageLoadResult.OutputTooShort;
            }

            pngData = pngData[(ihdrChunk.Data.Length + ChunkOverheadSize)..];

            int outputOffset = 0;
            int bpp = header.ColorType switch
            {
                0 => (header.BitDepth + 7) / 8,
                2 => ((header.BitDepth + 7) / 8) * 3,
                3 => 1,
                4 => ((header.BitDepth + 7) / 8) * 2,
                6 => ((header.BitDepth + 7) / 8) * 4,
                _ => 0,
            };

            ReadOnlySpan<uint> palette = ReadOnlySpan<uint>.Empty;

            using MemoryStream compressedStream = new();
            using ZLibStream zLibStream = new(compressedStream, CompressionMode.Decompress);

            int stride = ReverseEndianness(header.Width) * bpp;
            Span<byte> tempOutput = header.ColorType == 6 && header.BitDepth <= 8 ? output : new byte[stride * ReverseEndianness(header.Height)];
            byte[] scanline = new byte[stride];
            int scanlineOffset = 0;
            int filterType = -1;

            while (pngData.Length > 0)
            {
                result = TryParseChunk(pngData, out PngChunk chunk);

                if (result != ImageLoadResult.Success)
                {
                    return result;
                }

                switch (chunk.ChunkType)
                {
                    case IhdrMagic:
                        break;
                    case PlteMagic:
                        palette = DecodePalette(chunk.Data);
                        break;
                    case IdatMagic:
                        long position = compressedStream.Position;
                        compressedStream.Seek(0, SeekOrigin.End);
                        compressedStream.Write(chunk.Data);
                        compressedStream.Seek(position, SeekOrigin.Begin);
                        try
                        {
                            DecodeImageData(
                                zLibStream,
                                tempOutput,
                                ref outputOffset,
                                scanline,
                                ref scanlineOffset,
                                ref filterType,
                                ReverseEndianness(header.Width),
                                bpp);
                        }
                        catch (InvalidDataException)
                        {
                            return ImageLoadResult.CorruptedData;
                        }
                        break;
                    case IendMagic:
                        pngData = ReadOnlySpan<byte>.Empty;
                        break;
                    default:
                        bool isAncillary = char.IsAsciiLetterLower((char)(chunk.ChunkType >> 24));
                        if (!isAncillary)
                        {
                            return ImageLoadResult.CorruptedHeader;
                        }
                        break;
                }

                if (pngData.IsEmpty)
                {
                    break;
                }

                pngData = pngData[(chunk.Data.Length + ChunkOverheadSize)..];
            }

            if (header.BitDepth == 16)
            {
                Convert16BitTo8Bit(tempOutput[..(tempOutput.Length / 2)], tempOutput);
                tempOutput = tempOutput[..(tempOutput.Length / 2)];
            }

            switch (header.ColorType)
            {
                case 0:
                    CopyLToRgba(output, tempOutput);
                    break;
                case 2:
                    CopyRgbToRgba(output, tempOutput);
                    break;
                case 3:
                    CopyIndexedToRgba(output, tempOutput, palette);
                    break;
                case 4:
                    CopyLaToRgba(output, tempOutput);
                    break;
                case 6:
                    if (header.BitDepth == 16)
                    {
                        tempOutput.CopyTo(output);
                    }
                    break;
            }

            return ImageLoadResult.Success;
        }

        private static bool ValidateHeader(in PngHeader header)
        {
            // Width and height must be a non-zero positive value.
            if (ReverseEndianness(header.Width) <= 0 || ReverseEndianness(header.Height) <= 0)
            {
                return false;
            }

            // Only compression and filter methods 0 were ever defined as part of the spec, everything else is invalid.
            if ((header.CompressionMethod | header.FilterMethod) != 0)
            {
                return false;
            }

            // Only interlace methods 0 (None) and 1 (Adam7) are valid.
            if ((header.InterlaceMethod | 1) != 1)
            {
                return false;
            }

            return header.ColorType switch
            {
                0 => header.BitDepth == 1 ||
                     header.BitDepth == 2 ||
                     header.BitDepth == 4 ||
                     header.BitDepth == 8 ||
                     header.BitDepth == 16,
                2 or 4 or 6 => header.BitDepth == 8 || header.BitDepth == 16,
                3 => header.BitDepth == 1 ||
                     header.BitDepth == 2 ||
                     header.BitDepth == 4 ||
                     header.BitDepth == 8,
                _ => false,
            };
        }

        private static ImageLoadResult TryParseChunk(ReadOnlySpan<byte> pngData, out PngChunk chunk)
        {
            if (pngData.Length < 8)
            {
                chunk = default;
                return ImageLoadResult.DataTooShort;
            }

            uint length = BinaryPrimitives.ReadUInt32BigEndian(pngData);
            uint chunkType = BinaryPrimitives.ReadUInt32BigEndian(pngData[4..]);

            if (length + ChunkOverheadSize > pngData.Length)
            {
                chunk = default;
                return ImageLoadResult.DataTooShort;
            }

            uint crc = BinaryPrimitives.ReadUInt32BigEndian(pngData[(8 + (int)length)..]);

            ReadOnlySpan<byte> data = pngData.Slice(8, (int)length);

            if (crc != ComputeCrc(chunkType, data))
            {
                chunk = default;
                return ImageLoadResult.CorruptedData;
            }

            chunk = new()
            {
                ChunkType = chunkType,
                Data = data,
            };

            return ImageLoadResult.Success;
        }

        private static uint[] DecodePalette(ReadOnlySpan<byte> input)
        {
            uint[] palette = new uint[input.Length / 3];

            for (int i = 0; i < palette.Length; i++)
            {
                byte r = input[i * 3];
                byte g = input[i * 3 + 1];
                byte b = input[i * 3 + 2];

                palette[i] = 0xff000000 | ((uint)b << 16) | ((uint)g << 8) | r;

                if (!BitConverter.IsLittleEndian)
                {
                    palette[i] = BinaryPrimitives.ReverseEndianness(palette[i]);
                }
            }

            return palette;
        }

        private static void DecodeImageData(
            Stream zLibStream,
            Span<byte> output,
            ref int outputOffset,
            byte[] scanline,
            ref int scanlineOffset,
            ref int filterType,
            int width,
            int bpp)
        {
            int stride = width * bpp;

            while (true)
            {
                if (filterType == -1)
                {
                    filterType = zLibStream.ReadByte();

                    if (filterType == -1)
                    {
                        break;
                    }
                }

                while (scanlineOffset < scanline.Length)
                {
                    int bytesRead = zLibStream.Read(scanline.AsSpan()[scanlineOffset..]);

                    if (bytesRead == 0)
                    {
                        return;
                    }

                    scanlineOffset += bytesRead;

                    if (scanlineOffset >= scanline.Length)
                    {
                        scanlineOffset = 0;
                        break;
                    }
                }

                if (scanlineOffset == 0)
                {
                    switch ((FilterType)filterType)
                    {
                        case FilterType.None:
                            scanline.AsSpan().CopyTo(output[outputOffset..]);
                            break;
                        case FilterType.Sub:
                            for (int x = 0; x < scanline.Length; x++)
                            {
                                byte left = x < bpp ? (byte)0 : output[outputOffset + x - bpp];
                                output[outputOffset + x] = (byte)(left + scanline[x]);
                            }
                            break;
                        case FilterType.Up:
                            for (int x = 0; x < scanline.Length; x++)
                            {
                                byte above = outputOffset < stride ? (byte)0 : output[outputOffset + x - stride];
                                output[outputOffset + x] = (byte)(above + scanline[x]);
                            }
                            break;
                        case FilterType.Average:
                            for (int x = 0; x < scanline.Length; x++)
                            {
                                byte left = x < bpp ? (byte)0 : output[outputOffset + x - bpp];
                                byte above = outputOffset < stride ? (byte)0 : output[outputOffset + x - stride];
                                output[outputOffset + x] = (byte)(((left + above) >> 1) + scanline[x]);
                            }
                            break;
                        case FilterType.Paeth:
                            for (int x = 0; x < scanline.Length; x++)
                            {
                                byte left = x < bpp ? (byte)0 : output[outputOffset + x - bpp];
                                byte above = outputOffset < stride ? (byte)0 : output[outputOffset + x - stride];
                                byte leftAbove = outputOffset < stride || x < bpp ? (byte)0 : output[outputOffset + x - bpp - stride];
                                output[outputOffset + x] = (byte)(PaethPredict(left, above, leftAbove) + scanline[x]);
                            }
                            break;
                    }

                    outputOffset += scanline.Length;
                    filterType = -1;
                }
            }
        }

        public static void Save(Stream output, ImageParameters parameters, ReadOnlySpan<byte> data, bool fastMode = false)
        {
            output.Write(_pngSignature);

            WriteChunk(output, IhdrMagic, new PngHeader()
            {
                Width = ReverseEndianness(parameters.Width),
                Height = ReverseEndianness(parameters.Height),
                BitDepth = 8,
                ColorType = 6,
            });

            byte[] encoded = EncodeImageData(data, parameters.Width, parameters.Height, fastMode);

            for (int encodedOffset = 0; encodedOffset < encoded.Length; encodedOffset += MaxIdatChunkSize)
            {
                int length = Math.Min(MaxIdatChunkSize, encoded.Length - encodedOffset);

                WriteChunk(output, IdatMagic, encoded.AsSpan().Slice(encodedOffset, length));
            }

            WriteChunk(output, IendMagic, ReadOnlySpan<byte>.Empty);
        }

        private static byte[] EncodeImageData(ReadOnlySpan<byte> input, int width, int height, bool fastMode)
        {
            int bpp = 4;
            int stride = width * bpp;
            byte[] tempLine = new byte[stride];

            using MemoryStream ms = new();

            using (ZLibStream zLibStream = new(ms, fastMode ? CompressionLevel.Fastest : CompressionLevel.SmallestSize))
            {
                for (int y = 0; y < height; y++)
                {
                    ReadOnlySpan<byte> scanline = input.Slice(y * stride, stride);
                    FilterType filterType = fastMode ? FilterType.None : SelectFilterType(input, scanline, y, width, bpp);

                    zLibStream.WriteByte((byte)filterType);

                    switch (filterType)
                    {
                        case FilterType.None:
                            zLibStream.Write(scanline);
                            break;
                        case FilterType.Sub:
                            for (int x = 0; x < scanline.Length; x++)
                            {
                                byte left = x < bpp ? (byte)0 : scanline[x - bpp];
                                tempLine[x] = (byte)(scanline[x] - left);
                            }
                            zLibStream.Write(tempLine);
                            break;
                        case FilterType.Up:
                            for (int x = 0; x < scanline.Length; x++)
                            {
                                byte above = y == 0 ? (byte)0 : input[y * stride + x - stride];
                                tempLine[x] = (byte)(scanline[x] - above);
                            }
                            zLibStream.Write(tempLine);
                            break;
                        case FilterType.Average:
                            for (int x = 0; x < scanline.Length; x++)
                            {
                                byte left = x < bpp ? (byte)0 : scanline[x - bpp];
                                byte above = y == 0 ? (byte)0 : input[y * stride + x - stride];
                                tempLine[x] = (byte)(scanline[x] - ((left + above) >> 1));
                            }
                            zLibStream.Write(tempLine);
                            break;
                        case FilterType.Paeth:
                            for (int x = 0; x < scanline.Length; x++)
                            {
                                byte left = x < bpp ? (byte)0 : scanline[x - bpp];
                                byte above = y == 0 ? (byte)0 : input[y * stride + x - stride];
                                byte leftAbove = y == 0 || x < bpp ? (byte)0 : input[y * stride + x - bpp - stride];
                                tempLine[x] = (byte)(scanline[x] - PaethPredict(left, above, leftAbove));
                            }
                            zLibStream.Write(tempLine);
                            break;
                    }

                }
            }

            return ms.ToArray();
        }

        private static FilterType SelectFilterType(ReadOnlySpan<byte> input, ReadOnlySpan<byte> scanline, int y, int width, int bpp)
        {
            int stride = width * bpp;

            Span<int> deltas = stackalloc int[4];

            for (int x = 0; x < scanline.Length; x++)
            {
                byte left = x < bpp ? (byte)0 : scanline[x - bpp];
                byte above = y == 0 ? (byte)0 : input[y * stride + x - stride];
                byte leftAbove = y == 0 || x < bpp ? (byte)0 : input[y * stride + x - bpp - stride];

                int value = scanline[x];
                int valueSub = value - left;
                int valueUp = value - above;
                int valueAverage = value - ((left + above) >> 1);
                int valuePaeth = value - PaethPredict(left, above, leftAbove);

                deltas[0] += Math.Abs(valueSub);
                deltas[1] += Math.Abs(valueUp);
                deltas[2] += Math.Abs(valueAverage);
                deltas[3] += Math.Abs(valuePaeth);
            }

            int lowestDelta = int.MaxValue;
            FilterType bestCandidate = FilterType.None;

            for (int i = 0; i < deltas.Length; i++)
            {
                if (deltas[i] < lowestDelta)
                {
                    lowestDelta = deltas[i];
                    bestCandidate = (FilterType)(i + 1);
                }
            }

            return bestCandidate;
        }

        private static void WriteChunk<T>(Stream output, uint chunkType, T data) where T : unmanaged
        {
            WriteChunk(output, chunkType, MemoryMarshal.Cast<T, byte>(MemoryMarshal.CreateSpan(ref data, 1)));
        }

        private static void WriteChunk(Stream output, uint chunkType, ReadOnlySpan<byte> data)
        {
            WriteUInt32BE(output, (uint)data.Length);
            WriteUInt32BE(output, chunkType);
            output.Write(data);
            WriteUInt32BE(output, ComputeCrc(chunkType, data));
        }

        private static void WriteUInt32BE(Stream output, uint value)
        {
            output.WriteByte((byte)(value >> 24));
            output.WriteByte((byte)(value >> 16));
            output.WriteByte((byte)(value >> 8));
            output.WriteByte((byte)value);
        }

        private static int PaethPredict(int a, int b, int c)
        {
            int p = a + b - c;
            int pa = Math.Abs(p - a);
            int pb = Math.Abs(p - b);
            int pc = Math.Abs(p - c);

            if (pa <= pb && pa <= pc)
            {
                return a;
            }
            else if (pb <= pc)
            {
                return b;
            }
            else
            {
                return c;
            }
        }

        private static void Convert16BitTo8Bit(Span<byte> output, ReadOnlySpan<byte> input)
        {
            for (int i = 0; i < input.Length; i += 2)
            {
                output[i / 2] = input[i];
            }
        }

        private static void CopyLToRgba(Span<byte> output, ReadOnlySpan<byte> input)
        {
            int width = input.Length;

            for (int pixel = 0; pixel < width; pixel++)
            {
                byte luminance = input[pixel];
                int dstX = pixel * 4;

                output[dstX] = luminance;
                output[dstX + 1] = luminance;
                output[dstX + 2] = luminance;
                output[dstX + 3] = 0xff;
            }
        }

        private static void CopyRgbToRgba(Span<byte> output, ReadOnlySpan<byte> input)
        {
            int width = input.Length / 3;

            for (int pixel = 0; pixel < width; pixel++)
            {
                int srcX = pixel * 3;
                int dstX = pixel * 4;

                output[dstX] = input[srcX];
                output[dstX + 1] = input[srcX + 1];
                output[dstX + 2] = input[srcX + 2];
                output[dstX + 3] = 0xff;
            }
        }

        private static void CopyIndexedToRgba(Span<byte> output, ReadOnlySpan<byte> input, ReadOnlySpan<uint> palette)
        {
            Span<uint> outputAsUint = MemoryMarshal.Cast<byte, uint>(output);

            for (int pixel = 0; pixel < outputAsUint.Length; pixel++)
            {
                byte index = input[pixel];

                if (index < palette.Length)
                {
                    outputAsUint[pixel] = palette[index];
                }
            }
        }

        private static void CopyLaToRgba(Span<byte> output, ReadOnlySpan<byte> input)
        {
            int width = input.Length / 2;

            for (int pixel = 0; pixel < width; pixel++)
            {
                int srcX = pixel * 2;
                int dstX = pixel * 4;

                byte luminance = input[srcX];
                byte alpha = input[srcX + 1];

                output[dstX] = luminance;
                output[dstX + 1] = luminance;
                output[dstX + 2] = luminance;
                output[dstX + 3] = alpha;
            }
        }

        private static uint ComputeCrc(uint chunkType, ReadOnlySpan<byte> input)
        {
            uint crc = UpdateCrc(uint.MaxValue, (byte)(chunkType >> 24));
            crc = UpdateCrc(crc, (byte)(chunkType >> 16));
            crc = UpdateCrc(crc, (byte)(chunkType >> 8));
            crc = UpdateCrc(crc, (byte)chunkType);
            crc = UpdateCrc(crc, input);

            return ~crc;
        }

        private static uint UpdateCrc(uint crc, byte input)
        {
            return _crcTable[(byte)(crc ^ input)] ^ (crc >> 8);
        }

        private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> input)
        {
            uint c = crc;

            for (int n = 0; n < input.Length; n++)
            {
                c = _crcTable[(byte)(c ^ input[n])] ^ (c >> 8);
            }

            return c;
        }

        private static int ReverseEndianness(int value)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BinaryPrimitives.ReverseEndianness(value);
            }

            return value;
        }
    }
}
