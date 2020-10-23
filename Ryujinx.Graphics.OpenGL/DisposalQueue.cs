using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.OpenGL
{
    class DisposalQueue
    {
        private struct Entry
        {
            public IDisposable Obj { get; }
            public IntPtr Fence { get; }

            public Entry(IDisposable obj, IntPtr fence)
            {
                if (fence == IntPtr.Zero)
                {
                    throw new Exception(GL.GetError().ToString());
                }
                Obj = obj;
                Fence = fence;
            }
        }

        private readonly List<Entry> _entries = new List<Entry>();

        public void Add(IDisposable obj)
        {
            GL.GetError();
            _entries.Add(new Entry(obj, GL.FenceSync(SyncCondition.SyncGpuCommandsComplete, WaitSyncFlags.None)));
        }

        public void Tick()
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];

                if (GL.ClientWaitSync(entry.Fence, ClientWaitSyncFlags.None, 0L) == WaitSyncStatus.AlreadySignaled)
                {
                    _entries.RemoveAt(i--);
                    entry.Obj.Dispose();
                    GL.DeleteSync(entry.Fence);
                }
            }
        }
    }
}
