using LibHac.Common;
using LibHac.Fs;
using LibHac.FsSystem;
using LibHac.FsSystem.NcaUtils;
using Ryujinx.HLE.Exceptions;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.FileSystem.Content;
using Ryujinx.Horizon.Kernel;
using Ryujinx.Horizon.Kernel.Memory;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

using static Ryujinx.HLE.Utilities.FontUtils;

namespace Ryujinx.HLE.HOS.Services.Sdb.Pl
{
    class SharedFontManager : ServerBase
    {
        private readonly Switch _device;

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

        private int _sharedMemoryHandle;
        private ulong _sharedMemoryBaseAddress;
        public const int SharedMemorySize = 0x1100000;

        public SharedFontManager(Switch device) : base(device, "SdbServer")
        {
            _device = device;
        }

        private void EnsureInitialized(ContentManager contentManager)
        {
            if (_fontData == null)
            {
                Map.LocateMappableSpace(out _sharedMemoryBaseAddress, SharedMemorySize);

                KernelStatic.Syscall.CreateSharedMemory(
                    out _sharedMemoryHandle,
                    SharedMemorySize,
                    KMemoryPermission.ReadAndWrite,
                    KMemoryPermission.Read);

                KernelStatic.Syscall.MapSharedMemory(
                    _sharedMemoryHandle,
                    _sharedMemoryBaseAddress,
                    SharedMemorySize,
                    KMemoryPermission.ReadAndWrite);

                uint fontOffset = 0;

                FontInfo CreateFont(string name)
                {
                    if (contentManager.TryGetFontTitle(name, out long fontTitle) &&
                        contentManager.TryGetFontFilename(name, out string fontFilename))
                    {
                        string contentPath = contentManager.GetInstalledContentPath(fontTitle, StorageId.NandSystem, NcaContentType.Data);
                        string fontPath    = _device.FileSystem.SwitchPathToSystemPath(contentPath);

                        if (!string.IsNullOrWhiteSpace(fontPath))
                        {
                            byte[] data;

                            using (IStorage ncaFileStream = new LocalStorage(fontPath, FileAccess.Read, FileMode.Open))
                            {
                                Nca         nca   = new Nca(_device.System.KeySet, ncaFileStream);
                                IFileSystem romfs = nca.OpenFileSystem(NcaSectionType.Data, _device.System.FsIntegrityCheckLevel);

                                romfs.OpenFile(out IFile fontFile, ("/" + fontFilename).ToU8Span(), OpenMode.Read).ThrowIfFailure();

                                data = DecryptFont(fontFile.AsStream());
                            }

                            FontInfo info = new FontInfo((int)fontOffset, data.Length);

                            WriteMagicAndSize(_sharedMemoryBaseAddress + fontOffset, data.Length);

                            fontOffset += 8;

                            uint start = fontOffset;

                            for (; fontOffset - start < data.Length; fontOffset++)
                            {
                                KernelStatic.AddressSpace.Write(_sharedMemoryBaseAddress + fontOffset, data[fontOffset - start]);
                            }

                            return info;
                        }
                        else
                        {
                            if (!contentManager.TryGetSystemTitlesName(fontTitle, out string titleName))
                            {
                                titleName = "Unknown";
                            }

                            throw new InvalidSystemResourceException($"{titleName} ({fontTitle:x8}) system title not found! This font will not work, provide the system archive to fix this error. (See https://github.com/Ryujinx/Ryujinx#requirements for more information)");
                        }
                    }
                    else
                    {
                        throw new ArgumentException($"Unknown font \"{name}\"!");
                    }
                }

                _fontData = new Dictionary<SharedFontType, FontInfo>
                {
                    { SharedFontType.JapanUsEurope,       CreateFont("FontStandard")                  },
                    { SharedFontType.SimplifiedChinese,   CreateFont("FontChineseSimplified")         },
                    { SharedFontType.SimplifiedChineseEx, CreateFont("FontExtendedChineseSimplified") },
                    { SharedFontType.TraditionalChinese,  CreateFont("FontChineseTraditional")        },
                    { SharedFontType.Korean,              CreateFont("FontKorean")                    },
                    { SharedFontType.NintendoEx,          CreateFont("FontNintendoExtended")          }
                };

                if (fontOffset > SharedMemorySize)
                {
                    throw new InvalidSystemResourceException(
                        $"The sum of all fonts size exceed the shared memory size. " +
                        $"Please make sure that the fonts don't exceed {SharedMemorySize} bytes in total. " +
                        $"(actual size: {fontOffset} bytes).");
                }
            }
        }

        private void WriteMagicAndSize(ulong address, int size)
        {
            const int decMagic = 0x18029a7f;
            const int key      = 0x49621806;

            int encryptedSize = BinaryPrimitives.ReverseEndianness(size ^ key);

            KernelStatic.AddressSpace.Write(address + 0, decMagic);
            KernelStatic.AddressSpace.Write(address + 4, encryptedSize);
        }

        public int GetFontSize(SharedFontType fontType)
        {
            EnsureInitialized(_device.System.ContentManager);

            return _fontData[fontType].Size;
        }

        public int GetSharedMemoryAddressOffset(SharedFontType fontType)
        {
            EnsureInitialized(_device.System.ContentManager);

            return _fontData[fontType].Offset + 8;
        }

        public int GetSharedMemoryHandle()
        {
            EnsureInitialized(_device.System.ContentManager);

            return _sharedMemoryHandle;
        }
    }
}
