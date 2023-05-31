using SPB.Graphics;
using System;

namespace Ryujinx.Ui
{
    public sealed class OpenToolkitBindingsContext : OpenTK.IBindingsContext
    {
        private IBindingsContext _bindingContext;

        public OpenToolkitBindingsContext(IBindingsContext bindingsContext)
        {
            _bindingContext = bindingsContext;
        }

        public IntPtr GetProcAddress(string procName)
        {
            return _bindingContext.GetProcAddress(procName);
        }
    }
}
