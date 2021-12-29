using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform.WGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Ava.Ui.Controls
{
    [SupportedOSPlatform("linux")]
    public class AvaloniaGlxContext : SPB.Platform.GLX.GLXOpenGLContext
    {
        public AvaloniaGlxContext(IntPtr handle) 
            : base(FramebufferFormat.Default, 0, 0, 0, false, null)
        {
            ContextHandle = handle;
        }
    }
}
