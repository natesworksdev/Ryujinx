using ARMeilleure.Memory;
using System;

namespace ARMeilleure.State
{
    public class ExecutionContext
    {
        private const int MinCountForCheck = 4000;

        private NativeContext _nativeContext;

        internal IntPtr NativeContextPtr => _nativeContext.BasePtr;

        private bool _interrupted;

        private readonly ICounter _counter;

        public ulong Pc => _nativeContext.GetPc();

        public uint CtrEl0 => 0x8444c004;
        public uint DczidEl0 => 0x00000004;

        public ulong CntfrqEl0 => _counter.Frequency;
        public ulong CntpctEl0 => _counter.Counter;

        // CNTVCT_EL0 = CNTPCT_EL0 - CNTVOFF_EL2
        // Since EL2 isn't implemented, CNTVOFF_EL2 = 0
        public ulong CntvctEl0 => CntpctEl0;

        public long TpidrEl0 { get; set; }
        public long TpidrroEl0 { get; set; }

        public uint Pstate
        {
            get => _nativeContext.GetPstate();
            set => _nativeContext.SetPstate(value);
        }

        public FPCR Fpcr { get; set; }
        public FPSR Fpsr { get; set; }
        public FPCR StandardFpcrValue => (Fpcr & (FPCR.Ahp)) | FPCR.Dn | FPCR.Fz;

        public bool IsAarch32 { get; set; }

        internal ExecutionMode ExecutionMode
        {
            get
            {
                if (IsAarch32)
                {
                    return GetPstateFlag(PState.TFlag)
                        ? ExecutionMode.Aarch32Thumb
                        : ExecutionMode.Aarch32Arm;
                }
                else
                {
                    return ExecutionMode.Aarch64;
                }
            }
        }

        public bool Running
        {
            get => _nativeContext.GetRunning();
            private set => _nativeContext.SetRunning(value);
        }

        public event EventHandler<EventArgs> Interrupt;
        public event EventHandler<InstExceptionEventArgs> Break;
        public event EventHandler<InstExceptionEventArgs> SupervisorCall;
        public event EventHandler<InstUndefinedEventArgs> Undefined;

        public ExecutionContext(IJitMemoryAllocator allocator, ICounter counter)
        {
            _nativeContext = new NativeContext(allocator);
            _counter = counter;

            Running = true;

            _nativeContext.SetCounter(MinCountForCheck);
        }

        public ulong GetX(int index) => _nativeContext.GetX(index);
        public void SetX(int index, ulong value) => _nativeContext.SetX(index, value);

        public V128 GetV(int index) => _nativeContext.GetV(index);
        public void SetV(int index, V128 value) => _nativeContext.SetV(index, value);

        public bool GetPstateFlag(PState flag) => _nativeContext.GetPstateFlag(flag);
        public void SetPstateFlag(PState flag, bool value) => _nativeContext.SetPstateFlag(flag, value);

        public bool GetFPstateFlag(FPState flag) => _nativeContext.GetFPStateFlag(flag);
        public void SetFPstateFlag(FPState flag, bool value) => _nativeContext.SetFPStateFlag(flag, value);

        internal void CheckInterrupt()
        {
            if (_interrupted)
            {
                _interrupted = false;

                Interrupt?.Invoke(this, EventArgs.Empty);
            }

            _nativeContext.SetCounter(MinCountForCheck);
        }

        public void RequestInterrupt()
        {
            _interrupted = true;
        }

        internal void OnBreak(ulong address, int imm)
        {
            Break?.Invoke(this, new InstExceptionEventArgs(address, imm));
        }

        internal void OnSupervisorCall(ulong address, int imm)
        {
            SupervisorCall?.Invoke(this, new InstExceptionEventArgs(address, imm));
        }

        internal void OnUndefined(ulong address, int opCode)
        {
            Undefined?.Invoke(this, new InstUndefinedEventArgs(address, opCode));
        }

        public void StopRunning()
        {
            Running = false;

            _nativeContext.SetCounter(0);
        }

        public void Dispose()
        {
            _nativeContext.Dispose();
        }
    }
}