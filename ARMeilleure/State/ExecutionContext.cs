using System;

namespace ARMeilleure.State
{
    public class ExecutionContext : IDisposable
    {
        private NativeContext _nativeContext;

        internal IntPtr NativeContextPtr => _nativeContext.BasePtr;

        public ExecutionContext()
        {
            _nativeContext = new NativeContext();
        }

        public ulong GetX(int index)              => _nativeContext.GetX(index);
        public void  SetX(int index, ulong value) => _nativeContext.SetX(index, value);

        public V128 GetV(int index)             => _nativeContext.GetV(index);
        public void SetV(int index, V128 value) => _nativeContext.SetV(index, value);

        public bool GetPstateFlag(PState flag)             => _nativeContext.GetPstateFlag(flag);
        public void SetPstateFlag(PState flag, bool value) => _nativeContext.SetPstateFlag(flag, value);

        public void Dispose()
        {
            _nativeContext.Dispose();
        }
    }
}