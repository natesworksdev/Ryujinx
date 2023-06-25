using OpenTK;
using System;

namespace Ryujinx.Ava.UI.Renderer
{
    internal class OpenTkBindingsContext : IBindingsContext
    {
        private readonly Func<string, IntPtr> _getProcAddress;

        public OpenTkBindingsContext(Func<string, IntPtr> getProcAddress)
        {
            _getProcAddress = getProcAddress;
        }

        public IntPtr GetProcAddress(string procName)
        {
            return _getProcAddress(procName);
        }
    }
}
