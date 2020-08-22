using System;

namespace Ryujinx.Horizon.Kernel.Common
{
    struct OnScopeExit : IDisposable
    {
        private readonly Action _action;
        public OnScopeExit(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            _action();
        }
    }
}
