using System.Diagnostics;

namespace Ryujinx.Cpu.LightningJit.Arm64
{
    enum SystemOp
    {
        At,
        Dc,
        Ic,
        Tlbi,
        Sys,
    }

    static class SysUtils
    {
        public static (uint, uint, uint, uint) UnpackOp1CRnCRmOp2(uint encoding)
        {
            uint op1 = (encoding >> 16) & 7;
            uint crn = (encoding >> 12) & 0xf;
            uint crm = (encoding >> 8) & 0xf;
            uint op2 = (encoding >> 5) & 7;

            return (op1, crn, crm, op2);
        }

        public static bool IsCacheInstEl0(uint encoding)
        {
            (uint op1, uint crn, uint crm, uint op2) = UnpackOp1CRnCRmOp2(encoding);

            return ((op1 << 11) | (crn << 7) | (crm << 3) | op2) switch
            {
                0b011_0111_0100_001 => true, // DC ZVA
                0b011_0111_1010_001 => true, // DC CVAC
                0b011_0111_1100_001 => true, // DC CVAP
                0b011_0111_1011_001 => true, // DC CVAU
                0b011_0111_1110_001 => true, // DC CIVAC
                0b011_0111_0101_001 => true, // IC IVAU
                _ => false,
            };
        }

        public static bool IsCacheInstUciTrapped(uint encoding)
        {
            (uint op1, uint crn, uint crm, uint op2) = UnpackOp1CRnCRmOp2(encoding);

            return ((op1 << 11) | (crn << 7) | (crm << 3) | op2) switch
            {
                0b011_0111_1010_001 => true, // DC CVAC
                0b011_0111_1100_001 => true, // DC CVAP
                0b011_0111_1011_001 => true, // DC CVAU
                0b011_0111_1110_001 => true, // DC CIVAC
                0b011_0111_0101_001 => true, // IC IVAU
                _ => false,
            };
        }

        public static SystemOp SysOp(uint encoding)
        {
            (uint op1, uint crn, uint crm, uint op2) = UnpackOp1CRnCRmOp2(encoding);

            return SysOp(op1, crn, crm, op2);
        }

        public static SystemOp SysOp(uint op1, uint crn, uint crm, uint op2)
        {
            return ((op1 << 11) | (crn << 7) | (crm << 3) | op2) switch
            {
                0b000_0111_1000_000 => SystemOp.At, // S1E1R
                0b100_0111_1000_000 => SystemOp.At, // S1E2R
                0b110_0111_1000_000 => SystemOp.At, // S1E3R
                0b000_0111_1000_001 => SystemOp.At, // S1E1W
                0b100_0111_1000_001 => SystemOp.At, // S1E2W
                0b110_0111_1000_001 => SystemOp.At, // S1E3W
                0b000_0111_1000_010 => SystemOp.At, // S1E0R
                0b000_0111_1000_011 => SystemOp.At, // S1E0W
                0b100_0111_1000_100 => SystemOp.At, // S12E1R
                0b100_0111_1000_101 => SystemOp.At, // S12E1W
                0b100_0111_1000_110 => SystemOp.At, // S12E0R
                0b100_0111_1000_111 => SystemOp.At, // S12E0W
                0b011_0111_0100_001 => SystemOp.Dc, // ZVA
                0b000_0111_0110_001 => SystemOp.Dc, // IVAC
                0b000_0111_0110_010 => SystemOp.Dc, // ISW
                0b011_0111_1010_001 => SystemOp.Dc, // CVAC
                0b011_0111_1100_001 => SystemOp.Dc, // CVAP
                0b000_0111_1010_010 => SystemOp.Dc, // CSW
                0b011_0111_1011_001 => SystemOp.Dc, // CVAU
                0b011_0111_1110_001 => SystemOp.Dc, // CIVAC
                0b000_0111_1110_010 => SystemOp.Dc, // CISW
                0b000_0111_0001_000 => SystemOp.Ic, // IALLUIS
                0b000_0111_0101_000 => SystemOp.Ic, // IALLU
                0b011_0111_0101_001 => SystemOp.Ic, // IVAU
                0b100_1000_0000_001 => SystemOp.Tlbi, // IPAS2E1IS
                0b100_1000_0000_101 => SystemOp.Tlbi, // IPAS2LE1IS
                0b000_1000_0011_000 => SystemOp.Tlbi, // VMALLE1IS
                0b100_1000_0011_000 => SystemOp.Tlbi, // ALLE2IS
                0b110_1000_0011_000 => SystemOp.Tlbi, // ALLE3IS
                0b000_1000_0011_001 => SystemOp.Tlbi, // VAE1IS
                0b100_1000_0011_001 => SystemOp.Tlbi, // VAE2IS
                0b110_1000_0011_001 => SystemOp.Tlbi, // VAE3IS
                0b000_1000_0011_010 => SystemOp.Tlbi, // ASIDE1IS
                0b000_1000_0011_011 => SystemOp.Tlbi, // VAAE1IS
                0b100_1000_0011_100 => SystemOp.Tlbi, // ALLE1IS
                0b000_1000_0011_101 => SystemOp.Tlbi, // VALE1IS
                0b100_1000_0011_101 => SystemOp.Tlbi, // VALE2IS
                0b110_1000_0011_101 => SystemOp.Tlbi, // VALE3IS
                0b100_1000_0011_110 => SystemOp.Tlbi, // VMALLS12E1IS
                0b000_1000_0011_111 => SystemOp.Tlbi, // VAALE1IS
                0b100_1000_0100_001 => SystemOp.Tlbi, // IPAS2E1
                0b100_1000_0100_101 => SystemOp.Tlbi, // IPAS2LE1
                0b000_1000_0111_000 => SystemOp.Tlbi, // VMALLE1
                0b100_1000_0111_000 => SystemOp.Tlbi, // ALLE2
                0b110_1000_0111_000 => SystemOp.Tlbi, // ALLE3
                0b000_1000_0111_001 => SystemOp.Tlbi, // VAE1
                0b100_1000_0111_001 => SystemOp.Tlbi, // VAE2
                0b110_1000_0111_001 => SystemOp.Tlbi, // VAE3
                0b000_1000_0111_010 => SystemOp.Tlbi, // ASIDE1
                0b000_1000_0111_011 => SystemOp.Tlbi, // VAAE1
                0b100_1000_0111_100 => SystemOp.Tlbi, // ALLE1
                0b000_1000_0111_101 => SystemOp.Tlbi, // VALE1
                0b100_1000_0111_101 => SystemOp.Tlbi, // VALE2
                0b110_1000_0111_101 => SystemOp.Tlbi, // VALE3
                0b100_1000_0111_110 => SystemOp.Tlbi, // VMALLS12E1
                0b000_1000_0111_111 => SystemOp.Tlbi, // VAALE1
                _ => SystemOp.Sys,
            };
        }
    }
}