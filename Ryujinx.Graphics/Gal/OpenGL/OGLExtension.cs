using OpenTK.Graphics.OpenGL;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    static class OglExtension
    {
        private static Lazy<bool> _EnhancedLayouts    = new Lazy<bool>(() => HasExtension("GL_ARB_enhanced_layouts"));
        private static Lazy<bool> _TextureMirrorClamp = new Lazy<bool>(() => HasExtension("GL_EXT_texture_mirror_clamp"));
        private static Lazy<bool> _ViewportArray      = new Lazy<bool>(() => HasExtension("GL_ARB_viewport_array"));

        public static bool EnhancedLayouts    => _EnhancedLayouts.Value;
        public static bool TextureMirrorClamp => _TextureMirrorClamp.Value;
        public static bool ViewportArray      => _ViewportArray.Value;

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