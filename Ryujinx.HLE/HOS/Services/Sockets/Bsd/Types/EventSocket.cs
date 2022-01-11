using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.Sockets.Bsd
{
    class EventSocket : IBsdSocket
    {
        private ulong _value;
        private readonly EventFdFlags _flags;
        private AutoResetEvent _event;

        private object _lock = new object();

        public bool Blocking => !_flags.HasFlag(EventFdFlags.NonBlocking);

        public ManualResetEvent WriteEvent { get; }
        private ManualResetEvent ReadEvent { get; }

        public EventSocket(ulong value, EventFdFlags flags)
        {
            _value = value;
            _flags = flags;
            _event = new AutoResetEvent(false);

            WriteEvent = new ManualResetEvent(true);
            ReadEvent = new ManualResetEvent(true);
        }

        public int Refcount { get; set; }

        public void Dispose()
        {
            _event.Dispose();
            WriteEvent.Dispose();
            ReadEvent.Dispose();
        }

        public LinuxError Read(out int readSize, Span<byte> buffer)
        {
            if (buffer.Length < sizeof(ulong))
            {
                readSize = 0;

                return LinuxError.EINVAL;
            }

            ReadEvent.Reset();

            lock (_lock)
            {
                ref ulong count = ref MemoryMarshal.Cast<byte, ulong>(buffer)[0];

                if (_value == 0)
                {
                    if (Blocking)
                    {
                        while (_value == 0)
                        {
                            _event.WaitOne();
                        }
                    }
                    else
                    {
                        readSize = 0;

                        return LinuxError.EAGAIN;
                    }
                }

                readSize = sizeof(ulong);

                if (_flags.HasFlag(EventFdFlags.Semaphore))
                {
                    --_value;

                    count = 1;
                }
                else
                {
                    count = _value;

                    _value = 0;
                }

                ReadEvent.Set();

                return LinuxError.SUCCESS;
            }
        }

        public LinuxError Write(out int writeSize, ReadOnlySpan<byte> buffer)
        {
            if (!MemoryMarshal.TryRead(buffer, out ulong count) || count == ulong.MaxValue)
            {
                writeSize = 0;

                return LinuxError.EINVAL;
            }

            WriteEvent.Reset();

            lock (_lock)
            {
                if (_value > _value + count)
                {
                    if (Blocking)
                    {
                        _event.WaitOne();
                    }
                    else
                    {
                        writeSize = 0;

                        return LinuxError.EAGAIN;
                    }
                }

                writeSize = sizeof(ulong);

                _value += count;
                _event.Set();

                WriteEvent.Set();

                return LinuxError.SUCCESS;
            }
        }

        public static LinuxError Poll(List<PollEvent> events, int timeoutMilliseconds, out int updatedCount)
        {
            updatedCount = 0;

            List<ManualResetEvent> waiters = new List<ManualResetEvent>();

            for (int i = 0; i < events.Count; i++)
            {
                PollEvent evnt = events[i];

                EventSocket socket = (EventSocket)evnt.Socket;

                bool isValidEvent = false;

                if (evnt.Data.InputEvents.HasFlag(PollEventTypeMask.Input) ||
                    evnt.Data.InputEvents.HasFlag(PollEventTypeMask.UrgentInput))
                {
                    waiters.Add(socket.ReadEvent);

                    isValidEvent = true;
                }
                if (evnt.Data.InputEvents.HasFlag(PollEventTypeMask.Output))
                {
                    waiters.Add(socket.WriteEvent);

                    isValidEvent = true;
                }

                if (!isValidEvent)
                {
                    Logger.Warning?.Print(LogClass.ServiceBsd, $"Unsupported Poll input event type: {evnt.Data.InputEvents}");

                    return LinuxError.EINVAL;
                }
            }

            int index = WaitHandle.WaitAny(waiters.ToArray(), timeoutMilliseconds);

            if (index != WaitHandle.WaitTimeout)
            {
                for (int i = 0; i < events.Count; i++)
                {
                    PollEvent evnt = events[i];

                    EventSocket socket = (EventSocket)evnt.Socket;

                    if ((evnt.Data.InputEvents.HasFlag(PollEventTypeMask.Input) ||
                        evnt.Data.InputEvents.HasFlag(PollEventTypeMask.UrgentInput))
                        && socket.ReadEvent.WaitOne(0))
                    {
                        waiters.Add(socket.ReadEvent);
                    }
                    if ((evnt.Data.InputEvents.HasFlag(PollEventTypeMask.Output))
                        && socket.WriteEvent.WaitOne(0))
                    {
                        waiters.Add(socket.WriteEvent);
                    }
                }
            }
            else
            {
                return LinuxError.ETIMEDOUT;
            }

            return LinuxError.SUCCESS;
        }
    }
}
