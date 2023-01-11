using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Versioning;

namespace ARMeilleure.CodeGen.Arm64
{
    static partial class HardwareCapabilities
    {
        static HardwareCapabilities()
        {
            if (!ArmBase.Arm64.IsSupported)
            {
                return;
            }

            if (OperatingSystem.IsLinux())
            {
                LinuxFeatureInfoHwCap = (LinuxFeatureFlagsHwCap)getauxval(AT_HWCAP);
                LinuxFeatureInfoHwCap2 = (LinuxFeatureFlagsHwCap2)getauxval(AT_HWCAP2);
            }

            if (OperatingSystem.IsMacOS())
            {
                foreach ((string flagName, MacOsFeatureFlags flag) in Enumerable.Zip(Enum.GetNames<MacOsFeatureFlags>(), Enum.GetValues<MacOsFeatureFlags>()))
                {
                    string sysctlName = typeof(MacOsFeatureFlags).GetField(flagName)!.GetCustomAttributes(false).OfType<SysctlName>().Single().Name;

                    if (CheckSysctlName(sysctlName))
                    {
                        MacOsFeatureInfo |= flag;
                    }
                }
            }
        }

        private const ulong AT_HWCAP = 16;
        private const ulong AT_HWCAP2 = 26;

        [LibraryImport("libc", SetLastError = true)]
        private static partial ulong getauxval(ulong type);

        [Flags]
        public enum LinuxFeatureFlagsHwCap : ulong
        {
            Fp        = 1 << 0,
            Asimd     = 1 << 1,
            Evtstrm   = 1 << 2,
            Aes       = 1 << 3,
            Pmull     = 1 << 4,
            Sha1      = 1 << 5,
            Sha2      = 1 << 6,
            Crc32     = 1 << 7,
            Atomics   = 1 << 8,
            FpHp      = 1 << 9,
            AsimdHp   = 1 << 10,
            CpuId     = 1 << 11,
            AsimdRdm  = 1 << 12,
            Jscvt     = 1 << 13,
            Fcma      = 1 << 14,
            Lrcpc     = 1 << 15,
            DcpOp     = 1 << 16,
            Sha3      = 1 << 17,
            Sm3       = 1 << 18,
            Sm4       = 1 << 19,
            AsimdDp   = 1 << 20,
            Sha512    = 1 << 21,
            Sve       = 1 << 22,
            AsimdFhm  = 1 << 23,
            Dit       = 1 << 24,
            Uscat     = 1 << 25,
            Ilrcpc    = 1 << 26,
            FlagM     = 1 << 27,
            Ssbs      = 1 << 28,
            Sb        = 1 << 29,
            Paca      = 1 << 30,
            Pacg      = (ulong)1 << 31
        }

        [Flags]
        public enum LinuxFeatureFlagsHwCap2 : ulong
        {
            Dcpodp      = 1 << 0,
            Sve2        = 1 << 1,
            SveAes      = 1 << 2,
            SvePmull    = 1 << 3,
            SveBitperm  = 1 << 4,
            SveSha3     = 1 << 5,
            SveSm4      = 1 << 6,
            FlagM2      = 1 << 7,
            Frint       = 1 << 8,
            SveI8mm     = 1 << 9,
            SveF32mm    = 1 << 10,
            SveF64mm    = 1 << 11,
            SveBf16     = 1 << 12,
            I8mm        = 1 << 13,
            Bf16        = 1 << 14,
            Dgh         = 1 << 15,
            Rng         = 1 << 16,
            Bti         = 1 << 17,
            Mte         = 1 << 18,
            Ecv         = 1 << 19,
            Afp         = 1 << 20,
            Rpres       = 1 << 21,
            Mte3        = 1 << 22,
            Sme         = 1 << 23,
            Sme_i16i64  = 1 << 24,
            Sme_f64f64  = 1 << 25,
            Sme_i8i32   = 1 << 26,
            Sme_f16f32  = 1 << 27,
            Sme_b16f32  = 1 << 28,
            Sme_f32f32  = 1 << 29,
            Sme_fa64    = 1 << 30,
            Wfxt        = (ulong)1 << 31,
            Ebf16       = (ulong)1 << 32,
            Sve_Ebf16   = (ulong)1 << 33,
            Cssc        = (ulong)1 << 34,
            Rprfm       = (ulong)1 << 35,
            Sve2p1      = (ulong)1 << 36
        }

        public static LinuxFeatureFlagsHwCap LinuxFeatureInfoHwCap { get; } = 0;
        public static LinuxFeatureFlagsHwCap2 LinuxFeatureInfoHwCap2 { get; } = 0;

        [LibraryImport("libSystem.dylib", SetLastError = true)]
        private static unsafe partial int sysctlbyname([MarshalAs(UnmanagedType.LPStr)] string name, int* oldValue, ref ulong oldSize, IntPtr newValue, ulong newValueSize);

        [SupportedOSPlatform("macos")]
        private static bool CheckSysctlName(string name)
        {
            unsafe
            {
                int[] value = {0};
                fixed (int* valuePtr = &value[0])
                {
                    ulong size = (ulong)Unsafe.SizeOf<int>();
                    if (sysctlbyname(name, valuePtr, ref size, IntPtr.Zero, 0) == 0 && size == (ulong)Unsafe.SizeOf<int>())
                    {
                        return *valuePtr != 0;
                    }
                    return false;
                }
            }
        }

        private class SysctlName : Attribute
        {
            internal SysctlName(string name)
            {
                Name = name;
            }

            internal string Name { get; }
        }

        [Flags]
        public enum MacOsFeatureFlags
        {
            [SysctlName("hw.optional.arm.FEAT_AES")]
            Aes          = 1 << 0,
            [SysctlName("hw.optional.arm.FEAT_PMULL")]
            Pmull        = 1 << 1,
            [SysctlName("hw.optional.arm.FEAT_SHA1")]
            Sha1         = 1 << 2,
            [SysctlName("hw.optional.arm.FEAT_SHA256")]
            Sha256       = 1 << 3,
            [SysctlName("hw.optional.arm.FEAT_SHA512")]
            Sha512       = 1 << 4,
            [SysctlName("hw.optional.armv8_crc32")]
            Crc32        = 1 << 5,
            [SysctlName("hw.optional.arm.FEAT_LSE")]
            Lse          = 1 << 6,
            [SysctlName("hw.optional.arm.FEAT_RDM")]
            Rdm          = 1 << 7,
            [SysctlName("hw.optional.arm.FEAT_SHA3")]
            Sha3         = 1 << 8,
            [SysctlName("hw.optional.arm.FEAT_DotProd")]
            DotProd      = 1 << 9,
            [SysctlName("hw.optional.arm.FEAT_FHM")]
            Fhm          = 1 << 10,
            [SysctlName("hw.optional.arm.FEAT_FlagM")]
            FlagM        = 1 << 11,
            [SysctlName("hw.optional.arm.FEAT_FlagM2")]
            FlagM2       = 1 << 12,
            [SysctlName("hw.optional.floatingpoint")]
            Fp           = 1 << 13,
            [SysctlName("hw.optional.arm.FEAT_FP16")]
            Fp16         = 1 << 14,
            [SysctlName("hw.optional.AdvSIMD")]
            Asimd        = 1 << 15,
            [SysctlName("hw.optional.AdvSIMD_HPFPCvt")]
            AsimdHpfpcvt = 1 << 16,
            [SysctlName("hw.optional.arm.FEAT_CSV2")]
            Csv2         = 1 << 17,
            [SysctlName("hw.optional.arm.FEAT_CSV3")]
            Csv3         = 1 << 18,
            [SysctlName("hw.optional.arm.FEAT_DPB")]
            Dpb          = 1 << 19,
            [SysctlName("hw.optional.arm.FEAT_DPB2")]
            Dpb2         = 1 << 20,
            [SysctlName("hw.optional.arm.FEAT_JSCVT")]
            Jscvt        = 1 << 21,
            [SysctlName("hw.optional.arm.FEAT_FCMA")]
            Fcma         = 1 << 22,
            [SysctlName("hw.optional.arm.FEAT_LRCPC")]
            Lrcpc        = 1 << 23,
            [SysctlName("hw.optional.arm.FEAT_LRCPC2")]
            Lrcpc2       = 1 << 24,
            [SysctlName("hw.optional.arm.FEAT_FRINTTS")]
            Frintts      = 1 << 25,
            [SysctlName("hw.optional.arm.FEAT_SB")]
            Sb           = 1 << 26,
            [SysctlName("hw.optional.arm.FEAT_BF16")]
            Bf16         = 1 << 27,
            [SysctlName("hw.optional.arm.FEAT_I8MM")]
            I8mm         = 1 << 28
        }

        public static MacOsFeatureFlags MacOsFeatureInfo { get; } = 0;

        public static bool SupportsPmull = LinuxFeatureInfoHwCap.HasFlag(LinuxFeatureFlagsHwCap.Pmull) || MacOsFeatureInfo.HasFlag(MacOsFeatureFlags.Pmull);
    }
}