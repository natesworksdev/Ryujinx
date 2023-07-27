using System;
using System.Threading;

namespace Ryujinx.Common.Extensions
{
    public static class ReaderWriterLockSlimExtensions
    {
        public static IDisposable Read(this ReaderWriterLockSlim rwl)
        {
            rwl.EnterReadLock();
            return new ActionDisposable(rwl.ExitReadLock);
        }

        public static IDisposable Write(this ReaderWriterLockSlim rwl)
        {
            rwl.EnterWriteLock();
            return new ActionDisposable(rwl.ExitWriteLock);
        }
        
        public static IDisposable UpgradeableReadLock(this ReaderWriterLockSlim rwl)
        {
            rwl.EnterUpgradeableReadLock();
            return new ActionDisposable(rwl.ExitUpgradeableReadLock);
        }


        private sealed class ActionDisposable : IDisposable
        {
            private readonly Action _action;

            public ActionDisposable(Action action)
            {
                _action = action;
            }

            public void Dispose()
            {
                _action();
            }
        }
    }

}
