using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    internal static class OGLExtension
    {
        private static Lazy<bool> _enhancedLayouts    = new Lazy<bool>(() => HasExtension("GL_ARB_enhanced_layouts"));
        private static Lazy<bool> _textureMirrorClamp = new Lazy<bool>(() => HasExtension("GL_EXT_texture_mirror_clamp"));
        private static Lazy<bool> _viewportArray      = new Lazy<bool>(() => HasExtension("GL_ARB_viewport_array"));

        public static bool EnhancedLayouts    => _enhancedLayouts.Value;
        public static bool TextureMirrorClamp => _textureMirrorClamp.Value;
        public static bool ViewportArray      => _viewportArray.Value;

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