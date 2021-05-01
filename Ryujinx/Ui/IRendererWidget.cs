using Ryujinx.HLE;
using Ryujinx.Input.HLE;
using System;
using System.Threading;

namespace Ryujinx.Ui
{
    public interface IRendererWidget : IDisposable
    {
        NpadManager NpadManager { get; }

        ManualResetEvent WaitEvent { get; set; }

        void Initialize(Switch device);
        void Start();
        void Exit();
    }
}
