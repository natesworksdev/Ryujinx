namespace Ryujinx.Tests.Unicorn.Native
{
    public enum UnicornArch
    {
        UcArchArm = 1,    // ARM architecture (including Thumb, Thumb-2)
        UcArchArm64,      // ARM-64, also called AArch64
        UcArchMips,       // Mips architecture
        UcArchX86,        // X86 architecture (including x86 & x86-64)
        UcArchPpc,        // PowerPC architecture (currently unsupported)
        UcArchSparc,      // Sparc architecture
        UcArchM68K,       // M68K architecture
        UcArchMax,
    }
}
