using OpenTK.Graphics.OpenGL;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    static class OGLExtension
    {
        private static bool Initialized = false;

        private static bool EnhancedLayouts;
        private static bool ViewportArray;
        private static bool TextureMirrorClamp;

        public static bool HasEnhancedLayouts()    => Query(ref EnhancedLayouts);
        public static bool HasViewportArray()      => Query(ref ViewportArray);
        public static bool HasTextureMirrorClamp() => Query(ref TextureMirrorClamp);
        
        private static bool Query(ref bool Extension)
        {
            if (!Initialized)
            {
                EnhancedLayouts    = HasExtension("GL_ARB_enhanced_layouts");
                ViewportArray      = HasExtension("GL_ARB_viewport_array");
                TextureMirrorClamp = HasExtension("GL_EXT_texture_mirror_clamp");
            }

            return Extension;
        }

        private static bool HasExtension(string Name)
        {
            int NumExtensions = GL.GetInteger(GetPName.NumExtensions);

            for (int Extension = 0; Extension < NumExtensions; Extension++)
            {
                if (GL.GetString(StringNameIndexed.Extensions, Extension) == Name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}