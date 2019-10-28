using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Nv
{
    [StructLayout(LayoutKind.Sequential)]
    struct NvIoctl
    {
        private const int NumberBits    = 8;
        private const int TypeBits      = 8;
        private const int SizeBits      = 14;
        private const int DirectionBits = 2;

        private const int NumberShift    = 0;
        private const int TypeShift      = NumberShift + NumberBits;
        private const int SizeShift      = TypeShift + TypeBits; 
        private const int DirectionShift = SizeShift + SizeBits;

        private const int NumberMask    = (1 << NumberBits) - 1;
        private const int TypeMask      = (1 << TypeBits) - 1;
        private const int SizeMask      = (1 << SizeBits) - 1;
        private const int DirectionMask = (1 << DirectionBits) - 1;

        [Flags]
        public enum Direction : uint
        {
            None  = 0,
            Read  = 1,
            Write = 2,
        }

        public uint RawValue;

        public uint GetNumberValue()
        {
            return (RawValue >> NumberShift) & NumberMask;
        }

        public uint GetTypeValue()
        {
            return (RawValue >> TypeShift) & TypeMask;
        }

        public uint GetSizeValue()
        {
            return (RawValue >> SizeShift) & SizeMask;
        }

        public Direction GetDirectionValue()
        {
            return (Direction)((RawValue >> DirectionShift) & DirectionMask);
        }
    }
}
