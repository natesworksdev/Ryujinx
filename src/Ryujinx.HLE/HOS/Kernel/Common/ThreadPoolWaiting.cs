using Ryujinx.Common.Logging;
using Ryujinx.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.IO;

namespace Ryujinx.HLE.HOS.Kernel.Common
{
    partial class KTimeManager
    {

        class TickTimer
        {
            private readonly long _start = PerformanceCounter.ElapsedTicks;

            public struct Value
            {
                private long _ticks;
                public Value(long ticks)
                {
                    _ticks = ticks;
                }                

                public double AsMs()
                {
                    return AsMcs() / 1000;
                }

                public double AsMcs()
                {
                    return (_ticks * 1000.0) / PerformanceCounter.TicksPerMillisecond;
                }

            }

            public Value Get()
            {
                return new Value(PerformanceCounter.ElapsedTicks - _start);
            }
        }

        class ThreadPoolWaiting : IDisposable
        {
            private const int MaxThreads = 64;  // set to zero to disable feature at all  
            private const int MaxObjPerThread = 8;
            private const int MaxDelayForObjMergeMcs = 250;

            class Job
            {
                public List<WaitingObject> _objs = new List<WaitingObject> 
                    { Capacity = ThreadPoolWaiting.MaxObjPerThread };

                public long _delayMs;

                public long _startTicks = PerformanceCounter.ElapsedTicks; 
            }

            class PoolThread : IDisposable
            {
                private ThreadPoolWaiting _parent;
                private readonly Thread _thread;
                private bool _break;
                private AutoResetEvent _event = new AutoResetEvent(false);

                private Job _curr;

                private const int MaxStack = 64 * 1024;  // we don't need too much stack 

                public Job Current 
                { 
                  get => _curr; 
                  set
                  {                     
                        if (_curr == null)
                            Debug.Assert(value != null);
                        else
                            Debug.Assert(value == null);

                        _curr = value;

                        if (_curr != null)
                            _event.Set();
                    }
                }

                public PoolThread(ThreadPoolWaiting parent, int _threadId) 
                { 
                    _parent = parent;

                    _thread = new Thread(Body, MaxStack)
                    {
                        Name = $"HLE.WaitThread{_threadId}", 
                        Priority = ThreadPriority.AboveNormal
                    };

                    _thread.Start();
                }

                public void Dispose()
                {
                    _break = true;
                    _event.Set();

                    _thread.Join();
                }

                public bool TryMerge(WaitingObject obj, long delayMs)
                {
                    Debug.Assert(_curr != null);

                    if (_curr._delayMs != delayMs)
                        return false;

                    var elapsedMcs = ElapsedMsFrom(_curr._startTicks) * 1000;

                    if (elapsedMcs > ThreadPoolWaiting.MaxDelayForObjMergeMcs)
                        return false; 

                    if (_curr._objs.Capacity == _curr._objs.Count)
                        return false;

                    _curr._objs.Add(obj);

                    return true; 
                }

                private void Body()
                {
                    while(true)
                    {
                        _event.WaitOne();

                        if (_break)
                            break;

                        Debug.Assert(_curr != null);

                        double errorMs;

                        // do the job
                        {
                            var delayMs = (int)_curr._delayMs;

                            var t = new TickTimer();
                            Thread.Sleep(delayMs);
                            var elapsedMs = t.Get().AsMs(); 

                            errorMs = Math.Abs(delayMs - elapsedMs);
                        }

                        // notify
                        _parent.JobCompleted(this, errorMs);
                    }
                }

                static private double ElapsedMsFrom(long startTicks)
                {
                    var endTicks = PerformanceCounter.ElapsedTicks;

                    var elapsedMs = ((double)(endTicks - startTicks)) / PerformanceCounter.TicksPerMillisecond;

                    return elapsedMs; 
                }
            }

            class Stats
            {
                private double _errMsSum;
                private double _objCountSum; 
                private long _compledCount;

                private long _busyThreadSum;
                private long _busyThreadCount;

                private long _addedCount;
                private long _addAttempts;

                public void WaitCompleted(double errorMs, int objCount)
                {
                    _errMsSum += errorMs;
                    _objCountSum += objCount;

                    _compledCount++;
                }

                public void BusyThreadsSample(int sample)
                {
                    _busyThreadSum += sample;
                    _busyThreadCount++;
                }

                public void AddingResult(bool added)
                {
                    _addAttempts++;

                    if (added)
                        _addedCount++;
                }

                public override string ToString()
                {
                    string waitStats = $"avg. error {ToStr(_errMsSum / _compledCount)} ms," +
                           $" avg. obj {ToStr((double)_objCountSum / _compledCount)}," +
                           $" count {_compledCount}; ";

                    string busyStats = $"avg. busy thread {ToStr((double)_busyThreadSum / _busyThreadCount)}; ";

                    double denyPer = (_addAttempts - _addedCount) * 100.0 / _addedCount; 
                    string addStats = $"add deny {_addAttempts-_addedCount}({ToStr(denyPer)}%); ";

                    return waitStats + busyStats + addStats;
                }

                static private string ToStr(double val)
                {
                    return val.ToString("0.00"); 
                }
            }

            private KTimeManager _parent;
            private Stats _stats = new Stats();
            private int _threadCount;
            private readonly bool _disabled; 

            private LinkedList<PoolThread> _busyThreads = new LinkedList<PoolThread>();
            private LinkedList<PoolThread> _freeThreads = new LinkedList<PoolThread>();

            public ThreadPoolWaiting(KTimeManager parent) 
            {
                _parent = parent;
                _disabled = Disabled();
                
            }

            static private bool Disabled()
            {
                var exeDir = AppDomain.CurrentDomain.BaseDirectory;

                string[] paths = { exeDir, "DisableWaitingPool" };
                string fullPath = Path.Combine(paths);

                return Directory.Exists(fullPath);
            }

            public void Dispose()
            {
                lock( GetLock() )
                {
                    foreach (var thread in _busyThreads)
                    {
                        thread.Dispose();
                    }

                    foreach (var thread in _freeThreads)
                    {
                        thread.Dispose();
                    }
                }

                PrintStats();
            }

            [Conditional("DEBUG")]
            private void PrintStats()
            {
                string msg = $"Wait pool threads {_threadCount}; stats {_stats.ToString()}";

                Logger.Notice.Print(LogClass.Application, msg);
            }

            public bool TryAdd(WaitingObject obj, long delayNs)
            {
                if (_disabled)
                    return false;

                Debug.Assert(obj != null);
                Debug.Assert(delayNs > 0);

                lock( GetLock() )
                {
                    bool added = TryAddImpl(obj, delayNs);

                    if (added)
                        obj.AddedToPool = true;

                    _stats.AddingResult(added);

                    return added; 
                }
            }
            private bool TryAddImpl(WaitingObject obj, long delayNs)
            {
                var delayMs = delayNs / (1000 * 1000);

                if ((delayMs <= 0) || (delayMs > 5))
                    return false;

                var busyCount = _busyThreads.Count;

                _stats.BusyThreadsSample(busyCount);

                if ((busyCount > 0) && (_busyThreads.Last.Value.TryMerge(obj, delayMs)))
                {
                    return true;
                }

                var t = GetFreeThread();

                if (t == null)
                    return false;

                var job = new Job
                {
                    _delayMs = delayMs,
                };
                job._objs.Add(obj);

                t.Value.Current = job;

                _busyThreads.AddLast(t);

                return true;
            }


            private LinkedListNode<PoolThread> GetFreeThread()
            {
                LinkedListNode<PoolThread> t;

                if (_freeThreads.Count > 0)
                {
                    t = _freeThreads.Last;
                    _freeThreads.RemoveLast();
                }
                else
                {
                    if (_busyThreads.Count >= MaxThreads)
                        return null;

                    t = new LinkedListNode<PoolThread>( new PoolThread(this, _threadCount++) );
                }

                return t;
            }

            private void JobCompleted(PoolThread t, double errorMs)
            {
                lock( GetLock() )
                {
                    // move thread to free list 
                    {
                        var node = _busyThreads.Find(t);
                        Debug.Assert( node != null );

                        _busyThreads.Remove(node);
                        _freeThreads.AddLast(node);
                    }

                    // add stats
                    _stats.WaitCompleted(errorMs, t.Current._objs.Count);

                    // perform remove/callback for each object 
                    foreach (WaitingObject obj in t.Current._objs)
                    {
                        _parent.WaitIsCompleted(obj);
                    }

                    // clear job for the thread 
                    t.Current = null;
                }

            }

            static private void WaitTicks(int ticks)
            {
                if (ticks == 0)
                    return;

                var start = PerformanceCounter.ElapsedTicks;

                SpinWait spinWait = new();

                while(PerformanceCounter.ElapsedTicks - start < ticks)
                {
                    spinWait.SpinOnce(); 
                }
            }

            private object GetLock()
            {
                return _parent._context.CriticalSection.Lock;
            }

        }

    }

}
