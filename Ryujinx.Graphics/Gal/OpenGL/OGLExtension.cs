using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    static class OglExtension
    {
        private static Lazy<bool> _sEnhancedLayouts    = new Lazy<bool>(() => HasExtension("GL_ARB_enhanced_layouts"));
        private static Lazy<bool> _sTextureMirrorClamp = new Lazy<bool>(() => HasExtension("GL_EXT_texture_mirror_clamp"));
        private static Lazy<bool> _sViewportArray      = new Lazy<bool>(() => HasExtension("GL_ARB_viewport_array"));

        public static bool EnhancedLayouts    => _sEnhancedLayouts.Value;
        public static bool TextureMirrorClamp => _sTextureMirrorClamp.Value;
        public static bool ViewportArray      => _sViewportArray.Value;

        private static bool HasExtension(string name)
        {
            int numExtensions = GL.GetInteger(GetPName.NumExtensions);

            for (int extension = 0; extension < numExtensions; extension++)
            {
                if (GL.GetString(StringNameIndexed.Extensions, extension) == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}