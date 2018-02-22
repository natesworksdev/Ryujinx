using ChocolArm64;
using ChocolArm64.Memory;
using ChocolArm64.State;
using NUnit.Framework;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Ryujinx.Tests.Cpu
{
    [TestFixture]
    public class CpuTest
    {
        private const long Position = 0x0;
        private const long Size = 0x1000;

        private const long EntryPoint = Position;

        private IntPtr Ram;
        private AMemoryAlloc Allocator;
        private AMemory Memory;
        private AThread Thread;

        private long Pc;

        private bool BrkRetOpcodesControl;
        private bool SetThreadStateControl;
        private bool ExecuteOpcodesControl;
        private bool SingleOpcodeControl;

        [SetUp]
        public void Setup()
        {
            Ram = Marshal.AllocHGlobal((IntPtr)AMemoryMgr.RamSize);
            Allocator = new AMemoryAlloc();
            Memory = new AMemory(Ram, Allocator);
            Memory.Manager.MapPhys(Position, Size, 2, AMemoryPerm.Read | AMemoryPerm.Write | AMemoryPerm.Execute);
            Thread = new AThread(Memory, ThreadPriority.Normal, EntryPoint);

            Pc = EntryPoint - 4;

            BrkRetOpcodesControl = false;
            SetThreadStateControl = false;
            ExecuteOpcodesControl = false;
            SingleOpcodeControl = false;
        }

        [TearDown]
        public void Teardown()
        {
            Marshal.FreeHGlobal(Ram);
        }

        protected void Opcode(uint Opcode)
        {
            if ((Opcode & 0xFFE0001F) != 0xD4200000 && // BRK #<imm>
                (Opcode & 0xFFE0FC00) != 0xD6400000) // RET {<Xn>}
            {
                Pc += 4;
                Thread.Memory.WriteUInt32(Pc, Opcode);
            }
            else
            {
                Assert.Ignore();
            }
        }

        protected void BrkRetOpcodes()
        {
            BrkRetOpcodesControl = true;

            Pc += 4;
            Thread.Memory.WriteUInt32(Pc, 0xD4200000); // BRK #0
            Pc += 4;
            Thread.Memory.WriteUInt32(Pc, 0xD65F03C0); // RET
        }

        protected void SetThreadState(ulong X0 = 0, ulong X1 = 0, ulong X2 = 0,
                                      AVec V0 = default(AVec), AVec V1 = default(AVec), AVec V2 = default(AVec))
        {
            if (!SetThreadStateControl)
            {
                SetThreadStateControl = true;

                Thread.ThreadState.X0 = X0;
                Thread.ThreadState.X1 = X1;
                Thread.ThreadState.X2 = X2;
                Thread.ThreadState.V0 = V0;
                Thread.ThreadState.V1 = V1;
                Thread.ThreadState.V2 = V2;
            }
            else
            {
                Assert.Ignore();
            }
        }

        protected void ExecuteOpcodes()
        {
            if (!ExecuteOpcodesControl)
            {
                ExecuteOpcodesControl = true;

                if (!BrkRetOpcodesControl)
                {
                    BrkRetOpcodes();
                }

                if (!SetThreadStateControl)
                {
                    SetThreadState();
                }

                ManualResetEvent Wait = new ManualResetEvent(false);

                Thread.ThreadState.Break += (sender, e) => Thread.StopExecution();
                Thread.WorkFinished += (sender, e) => Wait.Set();

                Wait.Reset();
                Thread.Execute();
                Wait.WaitOne();
            }
            else
            {
                Assert.Ignore();
            }
        }

        protected AThreadState GetThreadState()
        {
            if (ExecuteOpcodesControl)
            {
                return Thread.ThreadState;
            }
            else
            {
                Assert.Ignore();

                return null;
            }
        }

        protected AThreadState SingleOpcode(uint Opcode,
                                            ulong X0 = 0, ulong X1 = 0, ulong X2 = 0,
                                            AVec V0 = default(AVec), AVec V1 = default(AVec), AVec V2 = default(AVec))
        {
            if (!SingleOpcodeControl)
            {
                SingleOpcodeControl = true;

                this.Opcode(Opcode);
                BrkRetOpcodes();
                SetThreadState(X0, X1, X2, V0, V1, V2);
                ExecuteOpcodes();

                return GetThreadState();
            }
            else
            {
                Assert.Ignore();

                return null;
            }
        }
    }
}
