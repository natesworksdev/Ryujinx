layout(binding = 0) uniform usamplerBuffer texture_list;
layout(binding = 1) uniform usamplerBuffer handle_list;

uvec4 Helper_GetBindlessInfo(int nvHandle)
{
    int textureId = nvHandle & 0xfffff;
    int samplerId = (nvHandle >> 20) & 0xfff;
    int index = (textureId >> 8) | ((samplerId >> 8) << 12);
    int subIdx = (textureId & 0xff) | ((samplerId & 0xff) << 8);
    int hndIdx = int(texelFetch(texture_list, index).x) * 0x10000 + subIdx;
    return texelFetch(handle_list, hndIdx);
}

uvec2 Helper_GetBindlessHandle(int nvHandle)
{
    return Helper_GetBindlessInfo(nvHandle).xy;
}

float Helper_GetBindlessScale(int nvHandle)
{
    return uintBitsToFloat(Helper_GetBindlessInfo(nvHandle).z);
}