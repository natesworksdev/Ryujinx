ivec2 Helper_TexelFetchScale(ivec2 inputVec, int samplerIndex)
{
    float scale = s_render_scale[samplerIndex];
    if (scale == 1.0)
    {
        return inputVec;
    }
    return ivec2(vec2(inputVec) * scale);
}

int Helper_TextureSizeUnscale(int size, int samplerIndex)
{
    float scale = s_render_scale[samplerIndex];
    if (scale == 1.0)
    {
        return size;
    }
    return int(float(size) / scale);
}

#ifdef HAS_BINDLESS
ivec2 Helper_TexelFetchScaleBindless(ivec2 inputVec, int samplerIndex, int nvHandle)
{
    float scale = Helper_GetBindlessScale(nvHandle);
    if (scale == 1.0)
    {
        return inputVec;
    }
    return ivec2(vec2(inputVec) * scale);
}

int Helper_TextureSizeUnscaleBindless(int size, int samplerIndex, int nvHandle)
{
    float scale = Helper_GetBindlessScale(nvHandle);
    if (scale == 1.0)
    {
        return size;
    }
    return int(float(size) / scale);
}
#endif