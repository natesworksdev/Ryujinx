using System;
using System.Collections.Concurrent;

namespace Ryujinx.Horizon.Sdk.Am
{
    interface IAppletFifo<T> : IProducerConsumerCollection<T>
    {
        event EventHandler DataAvailable;
    }
}
