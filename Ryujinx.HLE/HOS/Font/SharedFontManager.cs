using Ryujinx.HLE.Memory;
using Ryujinx.HLE.Resource;
using Ryujinx.HLE.Utilities;
using System.Collections.Generic;
using System.IO;

namespace Ryujinx.HLE.HOS.Font
{
    internal class SharedFontManager
    {
        private DeviceMemory _memory;

        private long _physicalAddress;

        private string _fontsPath;

        private struct FontInfo
        {
            public int Offset;
            public int Size;

            public FontInfo(int offset, int size)
            {
                Offset = offset;
                Size   = size;
            }
        }

        private Dictionary<SharedFontType, FontInfo> _fontData;

        public SharedFontManager(Switch device, long physicalAddress)
        {
            _physicalAddress = physicalAddress;

            _memory = device.Memory;

            _fontsPath = Path.Combine(device.FileSystem.GetSystemPath(), "fonts");
        }

        public void EnsureInitialized()
        {
            if (_fontData == null)
            {
                _memory.FillWithZeros(_physicalAddress, Horizon.FontSize);

                uint fontOffset = 0;

                FontInfo CreateFont(string name)
                {
                    string fontFilePath = Path.Combine(_fontsPath, name + ".ttf");

                    if (File.Exists(fontFilePath))
                    {
                        byte[] data = File.ReadAllBytes(fontFilePath);

                        FontInfo info = new FontInfo((int)fontOffset, data.Length);

                        WriteMagicAndSize(_physicalAddress + fontOffset, data.Length);

                        fontOffset += 8;

                        uint start = fontOffset;

                        for (; fontOffset - start < data.Length; fontOffset++)
                        {
                            _memory.WriteByte(_physicalAddress + fontOffset, data[fontOffset - start]);
                        }

                        return info;
                    }
                    else
                    {
                        throw new InvalidSystemResourceException($"Font \"{name}.ttf\" not found. Please provide it in \"{_fontsPath}\".");
                    }
                }

                _fontData = new Dictionary<SharedFontType, FontInfo>()
                {
                    { SharedFontType.JapanUsEurope,       CreateFont("FontStandard")                  },
                    { SharedFontType.SimplifiedChinese,   CreateFont("FontChineseSimplified")         },
                    { SharedFontType.SimplifiedChineseEx, CreateFont("FontExtendedChineseSimplified") },
                    { SharedFontType.TraditionalChinese,  CreateFont("FontChineseTraditional")        },
                    { SharedFontType.Korean,              CreateFont("FontKorean")                    },
                    { SharedFontType.NintendoEx,          CreateFont("FontNintendoExtended")          }
                };

                if (fontOffset > Horizon.FontSize)
                {
                    throw new InvalidSystemResourceException(
                        $"The sum of all fonts size exceed the shared memory size. " +
                        $"Please make sure that the fonts don't exceed {Horizon.FontSize} bytes in total. " +
                        $"(actual size: {fontOffset} bytes).");
                }
            }
        }

        private void WriteMagicAndSize(long position, int size)
        {
            const int decMagic = 0x18029a7f;
            const int key      = 0x49621806;

            int encryptedSize = EndianSwap.Swap32(size ^ key);

            _memory.WriteInt32(position + 0, decMagic);
            _memory.WriteInt32(position + 4, encryptedSize);
        }

        public int GetFontSize(SharedFontType fontType)
        {
            EnsureInitialized();

            return _fontData[fontType].Size;
        }

        public int GetSharedMemoryAddressOffset(SharedFontType fontType)
        {
            EnsureInitialized();

            return _fontData[fontType].Offset + 8;
        }
    }
}
