using Gtk;
using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Windowing;
using System;
using System.ComponentModel;

namespace Ryujinx.Ui
{
    // TODO: actual implementation
    [ToolboxItem(true)]
    public class GLWidget : DrawingArea
    {
        public event EventHandler Initialized;
        public event EventHandler ShuttingDown;

        public OpenGLContextBase OpenGLContext { get; private set; }
        public NativeWindowBase NativeWindow { get; private set; }


        public GLWidget(FramebufferFormat framebufferFormat, int major, int minor, OpenGLContextFlags flags = OpenGLContextFlags.Default, bool directRendering = true, OpenGLContextBase shareContext = null)
        {

        }
    }
}
