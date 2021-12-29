using OpenTK;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;

namespace Ryujinx.Ava.Ui.Controls
{
    public class OpenToolkitBindingsContext : IBindingsContext
    {
        private Func<string, IntPtr> _getProcAddress;

        public OpenToolkitBindingsContext(Func<string, IntPtr> getProcAddress)
        {
            _getProcAddress = getProcAddress;
        }

        public IntPtr GetProcAddress(string procName)
        {
            return _getProcAddress(procName);
        }
    }
}