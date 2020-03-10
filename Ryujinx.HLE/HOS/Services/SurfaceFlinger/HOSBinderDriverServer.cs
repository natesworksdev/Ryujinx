using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    class HOSBinderDriverServer : IHOSBinderDriver
    {
        private static Dictionary<int, IBinder> RegisteredBinderObjects = new Dictionary<int, IBinder>();

        private static int LastBinderId = 0;

        private static object Lock = new object();

        public static long RegisterBinderObject(IBinder binder)
        {
            lock (Lock)
            {
                LastBinderId++;

                RegisteredBinderObjects.Add(LastBinderId, binder);

                return LastBinderId;
            }
        }

        public static int GetBinderId(IBinder binder)
        {
            lock (Lock)
            {
                foreach (KeyValuePair<int, IBinder> pair in RegisteredBinderObjects)
                {
                    if (ReferenceEquals(binder, pair.Value))
                    {
                        return pair.Key;
                    }
                }

                return -1;
            }
        }

        private static IBinder GetBinderObjectById(int binderId)
        {
            lock (Lock)
            {
                if (RegisteredBinderObjects.TryGetValue(binderId, out IBinder binder))
                {
                    return binder;
                }

                return null;
            }
        }

        protected override ResultCode AdjustRefcount(int binderId, int addVal, int type)
        {
            IBinder binder = GetBinderObjectById(binderId);

            if (binder == null)
            {
                Logger.PrintError(LogClass.SurfaceFlinger, $"Invalid binder id {binderId}");

                return ResultCode.Success;
            }

            return binder.AdjustRefcount(addVal, type);
        }

        protected override void GetNativeHandle(int binderId, uint typeId, out KReadableEvent readableEvent)
        {
            IBinder binder = GetBinderObjectById(binderId);

            if (binder == null)
            {
                readableEvent = null;

                Logger.PrintError(LogClass.SurfaceFlinger, $"Invalid binder id {binderId}");

                return;
            }

            binder.GetNativeHandle(typeId, out readableEvent);
        }

        protected override ResultCode OnTransact(int binderId, uint code, uint flags, ReadOnlySpan<byte> inputParcel, Span<byte> outputParcel)
        {
            IBinder binder = GetBinderObjectById(binderId);

            if (binder == null)
            {
                Logger.PrintError(LogClass.SurfaceFlinger, $"Invalid binder id {binderId}");

                return ResultCode.Success;
            }

            return binder.OnTransact(code, flags, inputParcel, outputParcel);
        }
    }
}
