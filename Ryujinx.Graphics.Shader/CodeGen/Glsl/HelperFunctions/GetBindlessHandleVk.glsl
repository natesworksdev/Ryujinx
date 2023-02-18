#define HAS_BINDLESS

layout (set = 4, binding = 0, std140) uniform u_bindless_table
{
    uvec2 bindless_table[0x1000];
};

layout (set = 4, binding = 1) uniform texture1D bindless_textures1D[];
layout (set = 4, binding = 1) uniform texture2D bindless_textures2D[];
layout (set = 4, binding = 1) uniform texture3D bindless_textures3D[];
layout (set = 4, binding = 1) uniform textureCube bindless_texturesCube[];
layout (set = 4, binding = 1) uniform texture1DArray bindless_textures1DArray[];
layout (set = 4, binding = 1) uniform texture2DArray bindless_textures2DArray[];
layout (set = 4, binding = 1) uniform texture2DMS bindless_textures2DMS[];
layout (set = 4, binding = 1) uniform texture2DMSArray bindless_textures2DMSArray[];
layout (set = 4, binding = 1) uniform textureCubeArray bindless_texturesCubeArray[];
layout (set = 5, binding = 0) uniform sampler bindless_samplers[];
layout (set = 6, binding = 0) uniform image1D bindless_images1D[];
layout (set = 6, binding = 0) uniform image2D bindless_images2D[];
layout (set = 6, binding = 0) uniform image3D bindless_images3D[];
layout (set = 6, binding = 0) uniform imageCube bindless_imagesCube[];
layout (set = 6, binding = 0) uniform image1DArray bindless_images1DArray[];
layout (set = 6, binding = 0) uniform image2DArray bindless_images2DArray[];
layout (set = 6, binding = 0) uniform image2DMS bindless_images2DMS[];
layout (set = 6, binding = 0) uniform image2DMSArray bindless_images2DMSArray[];
layout (set = 6, binding = 0) uniform imageCubeArray bindless_imagesCubeArray[];

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
    return 1.0;
}