namespace Ryujinx.Graphics.Shader.CodeGen.Glsl
{
    static class DefaultNames
    {
        public const string LocalNamePrefix = "temp";

        public const string SamplerNamePrefix = "tex";
        public const string ImageNamePrefix   = "img";

        public const string BindlessTextureArray1DName = "bindless_textures1D";
        public const string BindlessTextureArray2DName = "bindless_textures2D";
        public const string BindlessTextureArray3DName = "bindless_textures3D";
        public const string BindlessTextureArrayCubeName = "bindless_texturesCube";
        public const string BindlessTextureArray1DArrayName = "bindless_textures1DArray";
        public const string BindlessTextureArray2DArrayName = "bindless_textures2DArray";
        public const string BindlessTextureArray2DMSName = "bindless_textures2DMS";
        public const string BindlessTextureArray2DMSArrayName = "bindless_textures2DMSArray";
        public const string BindlessTextureArrayCubeArrayName = "bindless_texturesCubeArray";
        public const string BindlessSamplerArrayName = "bindless_samplers";
        public const string BindlessImageArray1DName = "bindless_images1D";
        public const string BindlessImageArray2DName = "bindless_images2D";
        public const string BindlessImageArray3DName = "bindless_images3D";
        public const string BindlessImageArrayCubeName = "bindless_imagesCube";
        public const string BindlessImageArray1DArrayName = "bindless_images1DArray";
        public const string BindlessImageArray2DArrayName = "bindless_images2DArray";
        public const string BindlessImageArray2DMSName = "bindless_images2DMS";
        public const string BindlessImageArray2DMSArrayName = "bindless_images2DMSArray";
        public const string BindlessImageArrayCubeArrayName = "bindless_imagesCubeArray";

        public const string PerPatchAttributePrefix = "patch_attr_";
        public const string IAttributePrefix = "in_attr";
        public const string OAttributePrefix = "out_attr";

        public const string StorageNamePrefix = "s";

        public const string DataName = "data";

        public const string SupportBlockName = "support_block";
        public const string SupportBlockAlphaTestName = "s_alpha_test";
        public const string SupportBlockIsBgraName = "s_is_bgra";
        public const string SupportBlockViewportInverse = "s_viewport_inverse";
        public const string SupportBlockFragmentScaleCount = "s_frag_scale_count";
        public const string SupportBlockRenderScaleName = "s_render_scale";

        public const string BlockSuffix = "block";

        public const string UniformNamePrefix = "c";
        public const string UniformNameSuffix = "data";

        public const string LocalMemoryName  = "local_mem";
        public const string SharedMemoryName = "shared_mem";

        public const string ArgumentNamePrefix = "a";

        public const string UndefinedName = "undef";
    }
}