using Silk.NET.OpenGL.Legacy;
using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.OpenGL
{
    class Sync : IDisposable
    {
        private class SyncHandle
        {
            public ulong ID;
            public IntPtr Handle;
        }

        private ulong _firstHandle = 0;
        private static SyncObjectMask SyncFlags => HwCapabilities.RequiresSyncFlush ? 0 : SyncObjectMask.Bit;

        private readonly List<SyncHandle> _handles = new();
        private readonly GL _api;

        public Sync(GL api)
        {
            _api = api;
        }

        public void Create(ulong id)
        {
            SyncHandle handle = new()
            {
                ID = id,
                Handle = _api.FenceSync(SyncCondition.SyncGpuCommandsComplete, SyncBehaviorFlags.None),
            };


            if (HwCapabilities.RequiresSyncFlush)
            {
                // Force commands to flush up to the syncpoint.
                _api.ClientWaitSync(handle.Handle, SyncObjectMask.Bit, 0);
            }

            lock (_handles)
            {
                _handles.Add(handle);
            }
        }

        public ulong GetCurrent()
        {
            lock (_handles)
            {
                ulong lastHandle = _firstHandle;

                foreach (SyncHandle handle in _handles)
                {
                    lock (handle)
                    {
                        if (handle.Handle == IntPtr.Zero)
                        {
                            continue;
                        }

                        if (handle.ID > lastHandle)
                        {
                            GLEnum syncResult = _api.ClientWaitSync(handle.Handle, SyncFlags, 0);

                            if (syncResult == GLEnum.AlreadySignaled)
                            {
                                lastHandle = handle.ID;
                            }
                        }
                    }
                }

                return lastHandle;
            }
        }

        public void Wait(ulong id)
        {
            SyncHandle result = null;

            lock (_handles)
            {
                if ((long)(_firstHandle - id) > 0)
                {
                    return; // The handle has already been signalled or deleted.
                }

                foreach (SyncHandle handle in _handles)
                {
                    if (handle.ID == id)
                    {
                        result = handle;
                        break;
                    }
                }
            }

            if (result != null)
            {
                lock (result)
                {
                    if (result.Handle == IntPtr.Zero)
                    {
                        return;
                    }

                    GLEnum syncResult = _api.ClientWaitSync(result.Handle, SyncFlags, 1000000000);

                    if (syncResult == GLEnum.TimeoutExpired)
                    {
                        Logger.Error?.PrintMsg(LogClass.Gpu, $"GL Sync Object {result.ID} failed to signal within 1000ms. Continuing...");
                    }
                }
            }
        }

        public void Cleanup()
        {
            // Iterate through handles and remove any that have already been signalled.

            while (true)
            {
                SyncHandle first = null;
                lock (_handles)
                {
                    first = _handles.FirstOrDefault();
                }

                if (first == null)
                {
                    break;
                }

                GLEnum syncResult = _api.ClientWaitSync(first.Handle, SyncFlags, 0);

                if (syncResult == GLEnum.AlreadySignaled)
                {
                    // Delete the sync object.
                    lock (_handles)
                    {
                        lock (first)
                        {
                            _firstHandle = first.ID + 1;
                            _handles.RemoveAt(0);
                            _api.DeleteSync(first.Handle);
                            first.Handle = IntPtr.Zero;
                        }
                    }
                }
                else
                {
                    // This sync handle and any following have not been reached yet.
                    break;
                }
            }
        }

        public void Dispose()
        {
            lock (_handles)
            {
                foreach (SyncHandle handle in _handles)
                {
                    lock (handle)
                    {
                        _api.DeleteSync(handle.Handle);
                        handle.Handle = IntPtr.Zero;
                    }
                }

                _handles.Clear();
            }
        }
    }
}
