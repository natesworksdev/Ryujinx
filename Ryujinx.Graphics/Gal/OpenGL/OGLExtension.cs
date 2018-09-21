using OpenTK.Graphics.OpenGL;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    static class OGLExtension
    {
        private static bool Initialized = false;

        private static bool s_EnhancedLayouts;
        private static bool s_ViewportArray;
        private static bool s_TextureMirrorClamp;

        public static bool EnhancedLayouts    => Query(ref s_EnhancedLayouts);
        public static bool ViewportArray      => Query(ref s_ViewportArray);
        public static bool TextureMirrorClamp => Query(ref s_TextureMirrorClamp);
        
        private static bool Query(ref bool Extension)
        {
            if (!Initialized)
            {
                s_EnhancedLayouts = HasExtension("GL_ARB_enhanced_layouts");
                s_ViewportArray = HasExtension("GL_ARB_viewport_array");
                s_TextureMirrorClamp = HasExtension("GL_EXT_texture_mirror_clamp");

                Initialized = true;
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