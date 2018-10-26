namespace Ryujinx.Tests.Unicorn.Native
{
    public enum UnicornMode
    {
        UcModeLittleEndian = 0,    // little-endian mode (default mode)
        UcModeBigEndian = 1 << 30, // big-endian mode
        // arm / arm64
        UcModeArm = 0,              // ARM mode
        UcModeThumb = 1 << 4,       // THUMB mode (including Thumb-2)
        UcModeMclass = 1 << 5,      // ARM's Cortex-M series (currently unsupported)
        UcModeV8 = 1 << 6,          // ARMv8 A32 encodings for ARM (currently unsupported)
        // mips
        UcModeMicro = 1 << 4,       // MicroMips mode (currently unsupported)
        UcModeMips3 = 1 << 5,       // Mips III ISA (currently unsupported)
        UcModeMips32R6 = 1 << 6,    // Mips32r6 ISA (currently unsupported)
        UcModeMips32 = 1 << 2,      // Mips32 ISA
        UcModeMips64 = 1 << 3,      // Mips64 ISA
        // x86 / x64
        UcMode16 = 1 << 1,          // 16-bit mode
        UcMode32 = 1 << 2,          // 32-bit mode
        UcMode64 = 1 << 3,          // 64-bit mode
        // ppc
        UcModePpc32 = 1 << 2,       // 32-bit mode (currently unsupported)
        UcModePpc64 = 1 << 3,       // 64-bit mode (currently unsupported)
        UcModeQpx = 1 << 4,         // Quad Processing eXtensions mode (currently unsupported)
        // sparc
        UcModeSparc32 = 1 << 2,     // 32-bit mode
        UcModeSparc64 = 1 << 3,     // 64-bit mode
        UcModeV9 = 1 << 4,          // SparcV9 mode (currently unsupported)
        // m68k
    }
}
