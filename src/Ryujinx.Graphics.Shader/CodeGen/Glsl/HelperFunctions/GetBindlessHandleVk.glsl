uint Helper_GetBindlessTextureIndex(int nvHandle)
{
    int id = nvHandle & 0xfffff;
    return bindless_table[id >> 8].x | uint(id & 0xff);
}

uint Helper_GetBindlessSamplerIndex(int nvHandle)
{
    int id = (nvHandle >> 20) & 0xfff;
    return bindless_table[id >> 8].y | uint(id & 0xff);
}

float Helper_GetBindlessScale(int nvHandle)
{
    return bindless_scales[Helper_GetBindlessTextureIndex(nvHandle)];
}