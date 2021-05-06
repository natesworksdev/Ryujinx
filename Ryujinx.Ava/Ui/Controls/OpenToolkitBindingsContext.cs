using OpenTK;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace Ryujinx.Ava.Ui.Controls
{
    public class OpenToolkitBindingsContext : IBindingsContext
    {
        public IntPtr GetProcAddress(string procName)
        {
            return GLFW.GetProcAddress(procName);
        }
    }
}