using ChocolArm64.Events;
using ChocolArm64.Memory;
using ChocolArm64.State;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Kernel
{
    partial class SvcHandler
    {
        private delegate void SvcFunc(AThreadState threadState);

        private Dictionary<int, SvcFunc> _svcFuncs;

        private Switch  _device;
        private Process _process;
        private Horizon _system;
        private AMemory _memory;

        private struct HleIpcMessage
        {
            public KThread    Thread     { get; private set; }
            public KSession   Session    { get; private set; }
            public IpcMessage Message    { get; private set; }
            public long       MessagePtr { get; private set; }

            public HleIpcMessage(
                KThread    thread,
                KSession   session,
                IpcMessage message,
                long       messagePtr)
            {
                Thread     = thread;
                Session    = session;
                Message    = message;
                MessagePtr = messagePtr;
            }
        }

        private static Random _rng;

        public SvcHandler(Switch device, Process process)
        {
            _svcFuncs = new Dictionary<int, SvcFunc>()
            {
                { 0x01, SvcSetHeapSize                   },
                { 0x03, SvcSetMemoryAttribute            },
                { 0x04, SvcMapMemory                     },
                { 0x05, SvcUnmapMemory                   },
                { 0x06, SvcQueryMemory                   },
                { 0x07, SvcExitProcess                   },
                { 0x08, SvcCreateThread                  },
                { 0x09, SvcStartThread                   },
                { 0x0a, SvcExitThread                    },
                { 0x0b, SvcSleepThread                   },
                { 0x0c, SvcGetThreadPriority             },
                { 0x0d, SvcSetThreadPriority             },
                { 0x0e, SvcGetThreadCoreMask             },
                { 0x0f, SvcSetThreadCoreMask             },
                { 0x10, SvcGetCurrentProcessorNumber     },
                { 0x11, SignalEvent64                    },
                { 0x12, ClearEvent64                     },
                { 0x13, SvcMapSharedMemory               },
                { 0x14, SvcUnmapSharedMemory             },
                { 0x15, SvcCreateTransferMemory          },
                { 0x16, SvcCloseHandle                   },
                { 0x17, ResetSignal64                    },
                { 0x18, SvcWaitSynchronization           },
                { 0x19, SvcCancelSynchronization         },
                { 0x1a, SvcArbitrateLock                 },
                { 0x1b, SvcArbitrateUnlock               },
                { 0x1c, SvcWaitProcessWideKeyAtomic      },
                { 0x1d, SvcSignalProcessWideKey          },
                { 0x1e, SvcGetSystemTick                 },
                { 0x1f, SvcConnectToNamedPort            },
                { 0x21, SvcSendSyncRequest               },
                { 0x22, SvcSendSyncRequestWithUserBuffer },
                { 0x25, SvcGetThreadId                   },
                { 0x26, SvcBreak                         },
                { 0x27, SvcOutputDebugString             },
                { 0x29, SvcGetInfo                       },
                { 0x2c, SvcMapPhysicalMemory             },
                { 0x2d, SvcUnmapPhysicalMemory           },
                { 0x32, SvcSetThreadActivity             },
                { 0x33, SvcGetThreadContext3             },
                { 0x34, SvcWaitForAddress                },
                { 0x35, SvcSignalToAddress               },
                { 0x45, CreateEvent64                    }
            };

            _device  = device;
            _process = process;
            _system  = process.Device.System;
            _memory  = process.Memory;
        }

        static SvcHandler()
        {
            _rng = new Random();
        }

        public void SvcCall(object sender, AInstExceptionEventArgs e)
        {
            AThreadState threadState = (AThreadState)sender;

            _process.GetThread(threadState.Tpidr).LastPc = e.Position;

            if (_svcFuncs.TryGetValue(e.Id, out SvcFunc func))
            {
                Logger.PrintDebug(LogClass.KernelSvc, $"{func.Method.Name} called.");

                func(threadState);

                Logger.PrintDebug(LogClass.KernelSvc, $"{func.Method.Name} ended.");
            }
            else
            {
                _process.PrintStackTrace(threadState);

                throw new NotImplementedException($"0x{e.Id:x4}");
            }
        }
    }
}