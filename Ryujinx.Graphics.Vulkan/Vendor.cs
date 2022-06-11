namespace Ryujinx.Graphics.Vulkan
{
    enum Vendor
    {
        Amd,
        Intel,
        Nvidia,
        Qualcomm,
        Unknown
    }

    static class VendorUtils
    {
        public static Vendor FromId(uint id)
        {
            return id switch
            {
                0x1002 => Vendor.Amd,
                0x10DE => Vendor.Nvidia,
                0x8086 => Vendor.Intel,
                0x5143 => Vendor.Qualcomm,
                _ => Vendor.Unknown
            };
        }
    }
}
