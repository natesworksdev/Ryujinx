using ChocolArm64.Exceptions;
using ChocolArm64.Memory;
using Ryujinx.HLE.Logging;
using Ryujinx.HLE.OsHle;
using Ryujinx.HLE.OsHle.Handles;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.Font
{
    public class SharedFontManager
    {
        private Logger Log;

        private object ShMemLock;

        private (AMemory, long, long)[] ShMemPositions;

        private Dictionary<SharedFontType, byte[]> FontEmbeddedPaths;

        private uint[] LoadedFonts;

        public SharedFontManager(Logger Log)
        {
            this.Log          = Log;

            ShMemLock         = new object();

            ShMemPositions    = new(AMemory, long, long)[0];

            FontEmbeddedPaths = new Dictionary<SharedFontType, byte[]>()
            {
                { SharedFontType.JapanUsEurope,       EmbeddedResource.GetData("FontStandard")                  },
                { SharedFontType.SimplifiedChinese,   EmbeddedResource.GetData("FontChineseSimplified")         },
                { SharedFontType.SimplifiedChineseEx, EmbeddedResource.GetData("FontExtendedChineseSimplified") },
                { SharedFontType.TraditionalChinese,  EmbeddedResource.GetData("FontChineseTraditional")        },
                { SharedFontType.Korean,              EmbeddedResource.GetData("FontKorean")                    },
                { SharedFontType.NintendoEx,          EmbeddedResource.GetData("FontNintendoExtended")          }
            };

            LoadedFonts       = new uint[FontEmbeddedPaths.Count];
        }

        public void MapFont(SharedFontType FontType, AMemory Memory, long Position)
        {
            // TODO: find what are the 8 bytes before the font
            Memory.WriteBytes(Position + GetSharedMemoryAddressOffset(FontType), FontEmbeddedPaths[FontType]);
        }

        public void PropagateNewMapFont(SharedFontType Type)
        {
            lock (ShMemLock)
            {
                foreach ((AMemory Memory, long Position, long Size) in ShMemPositions)
                {
                    AMemoryMapInfo MemoryInfo = Memory.Manager.GetMapInfo(Position);

                    if (MemoryInfo == null)
                    {
                        throw new VmmPageFaultException(Position);
                    }

                    // The memory is read only, we need to changes that to add the new font
                    AMemoryPerm originalPerms = MemoryInfo.Perm;
                    Memory.Manager.Reprotect(Position, Size, AMemoryPerm.RW);
                    MapFont(Type, Memory, Position);
                    Memory.Manager.Reprotect(Position, Size, originalPerms);
                }
            }
        }

        internal void ShMemMap(object sender, EventArgs e)
        {
            HSharedMem SharedMem = (HSharedMem)sender;

            lock (ShMemLock)
            {
                ShMemPositions = SharedMem.GetVirtualPositions();

                (AMemory Memory, long Position, long Size) = ShMemPositions[ShMemPositions.Length - 1];

                for (SharedFontType Type = SharedFontType.JapanUsEurope; (int)Type < LoadedFonts.Length; Type++)
                {
                    if (LoadedFonts[(int)Type] == 1)
                    {
                        MapFont(Type, Memory, Position);
                    }
                }
            }
        }

        internal void ShMemUnmap(object sender, EventArgs e)
        {
            HSharedMem SharedMem = (HSharedMem)sender;

            lock (ShMemLock)
            {
                ShMemPositions = SharedMem.GetVirtualPositions();
            }
        }

        public void Load(SharedFontType FontType)
        {
            if (LoadedFonts[(int)FontType] == 0)
            {
                PropagateNewMapFont(FontType);
            }

            LoadedFonts[(int)FontType] = 1;
        }

        public uint GetLoadState(SharedFontType FontType)
        {
            if (LoadedFonts[(int)FontType] != 1)
            {
                // Some games don't request a load, so we need to load it here.
                Load(FontType);
                return 0;
            }
            return LoadedFonts[(int)FontType];
        }

        public uint GetFontSize(SharedFontType FontType)
        {
            return Convert.ToUInt32(FontEmbeddedPaths[FontType].Length);
        }

        public uint GetSharedMemoryAddressOffset(SharedFontType FontType)
        {
            uint Pos = 0x8;

            for (SharedFontType Type = SharedFontType.JapanUsEurope; Type < FontType; Type++)
            {
                Pos += GetFontSize(Type);
                Pos += 0x8;
            }

            return Pos;
        }

        public int Count()
        {
            return FontEmbeddedPaths.Count;
        }
    }
}
