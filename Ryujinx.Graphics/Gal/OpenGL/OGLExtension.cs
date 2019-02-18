using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Logging;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    public static class OGLExtension
    {
        private static bool _strictOpenGL;

        private static Lazy<bool> s_EnhancedLayouts    = new Lazy<bool>(() => HasExtension("GL_ARB_enhanced_layouts"));
        private static Lazy<bool> s_TextureMirrorClamp = new Lazy<bool>(() => HasExtension("GL_EXT_texture_mirror_clamp"));
        private static Lazy<bool> s_ViewportArray      = new Lazy<bool>(() => HasExtension("GL_ARB_viewport_array"));

        public static bool EnhancedLayouts    => s_EnhancedLayouts.Value;
        public static bool TextureMirrorClamp => s_TextureMirrorClamp.Value;
        public static bool ViewportArray      => s_ViewportArray.Value;

        // Strict
        private static Lazy<bool> s_ViewportArrayStrict = new Lazy<bool>(() => HasExtensionStrict("GL_ARB_viewport_array"));

        public static bool ViewportArrayStrict => s_ViewportArrayStrict.Value;

        public static void SetStrictOpenGL(bool enabled) => _strictOpenGL = enabled;

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

            Logger.PrintWarning(LogClass.Gpu, $"OpenGL extension {Name} unavailable. You may experience some rendering issues or performance degredation");

            return false;
        }

        private static bool HasExtensionStrict(string Name)
        {
            bool available = HasExtension(Name);

            if (available)
            {
                return true;
            }

            if (_strictOpenGL)
            {
                throw new Exception($"Required OpenGL extension {Name} unavailable. You can ignore this message by disabling 'enable_strict_opengl' " +
                                    $"in the config however you may experience some rendering issues or performance degredation");
            }

            return false;
        }
    }
}