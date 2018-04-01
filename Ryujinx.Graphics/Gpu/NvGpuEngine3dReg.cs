namespace Ryujinx.Graphics.Gpu
{
    enum NvGpuEngine3dReg
    {
        VertexAttribNFormat = 0x458,
        ShaderAddress       = 0x582,
        QueryAddress        = 0x6c0,
        QuerySequence       = 0x6c2,
        QueryControl        = 0x6c3,
        VertexArrayNControl = 0x700,
        VertexArrayNAddress = 0x701,
        VertexArrayNDivisor = 0x703,
        VertexArrayNEndAddr = 0x7c0,
        ShaderNControl      = 0x800,
        ShaderNOffset       = 0x801,
        ShaderNMaxGprs      = 0x803,
        ShaderNType         = 0x804,
        ConstBufferNSize    = 0x8e0,
        ConstBufferNAddress = 0x8e1,
        ConstBufferNOffset  = 0x8e3,
        TextureCbIndex      = 0x982
    }
}