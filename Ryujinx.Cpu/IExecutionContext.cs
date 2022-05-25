using ARMeilleure.State;
using System;

namespace Ryujinx.Cpu
{
    public interface IExecutionContext : IDisposable
    {
        ulong Pc { get; }

        long TpidrEl0 { get; set; }
        long TpidrroEl0 { get; set; }

        uint Pstate { get; set; }

        uint Fpcr { get; set; }
        uint Fpsr { get; set; }

        bool IsAarch32 { get; set; }

        bool Running { get; }

        ulong GetX(int index);
        void SetX(int index, ulong value);

        V128 GetV(int index);
        void SetV(int index, V128 value);

        void RequestInterrupt();
        void StopRunning();
    }
}