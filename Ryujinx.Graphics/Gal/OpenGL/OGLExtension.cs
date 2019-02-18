using OpenTK.Graphics.OpenGL;
using Ryujinx.Common.Logging;
using System;

namespace Ryujinx.Graphics.Gal.OpenGL
{
    public static class OGLExtension
    {
        // Private lazy backing variables
        private static Lazy<bool> s_EnhancedLayouts    = new Lazy<bool>(() => HasExtension("GL_ARB_enhanced_layouts"));
        private static Lazy<bool> s_TextureMirrorClamp = new Lazy<bool>(() => HasExtension("GL_EXT_texture_mirror_clamp"));
        private static Lazy<bool> s_ViewportArray      = new Lazy<bool>(() => HasExtension("GL_ARB_viewport_array"));

        // Public accessors
        public static bool EnhancedLayouts    => s_EnhancedLayouts.Value;
        public static bool TextureMirrorClamp => s_TextureMirrorClamp.Value;
        public static bool ViewportArray      => s_ViewportArray.Value;

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

        public static class Strict
        {
            // Strict enabled
            public static void SetStrictOpenGL(bool enabled) => _strictOpenGL = enabled;

            private static bool _strictOpenGL;

            // Public accessors
            public static bool EnhancedLayouts    => s_EnhancedLayoutsStrict.Value;
            public static bool TextureMirrorClamp => s_TextureMirrorClampStrict.Value;
            public static bool ViewportArray      => s_ViewportArrayStrict.Value;

            // Private lazy backing variables
            private static Lazy<bool> s_EnhancedLayoutsStrict    = new Lazy<bool>(() => HasExtensionStrict(OGLExtension.EnhancedLayouts,    "GL_ARB_enhanced_layouts"));
            private static Lazy<bool> s_TextureMirrorClampStrict = new Lazy<bool>(() => HasExtensionStrict(OGLExtension.TextureMirrorClamp, "GL_EXT_texture_mirror_clamp"));
            private static Lazy<bool> s_ViewportArrayStrict      = new Lazy<bool>(() => HasExtensionStrict(OGLExtension.ViewportArray,      "GL_ARB_viewport_array"));

            private static bool HasExtensionStrict(bool Value, string Name)
            {
                if (Value)
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
}