namespace Ryujinx.Graphics.Gpu
{
    enum NvGpuEngine3dReg
    {
        ShaderAddress  = 0x582,
        QueryAddress   = 0x6c0,
        QuerySequence  = 0x6c2,
        QueryControl   = 0x6c3,
        ShaderControl  = 0x800,
        ShaderOffset   = 0x801,
        ShaderMaxGprs  = 0x803,
        ShaderType     = 0x804,
        CbSize         = 0x8e0,
        CbAddress      = 0x8e1,
        CbOffset       = 0x8e3,
        TextureCbIndex = 0x982
    }
}