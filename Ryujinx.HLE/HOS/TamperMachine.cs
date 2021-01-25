using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.HLE.HOS.Tamper;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.HLE.HOS
{

    // TODO: Disable PTC and Force JIT invalidation when tampering program region of memory?

    public class TamperMachine
    {
        class Tampering
        {
            public long Pid { get; private set; }
            public TamperProgram Program { get; private set; }

            public Tampering(long pid, TamperProgram program)
            {
                Pid = pid;
                Program = program;
            }
        }

        private Switch _device;
        private Thread _tamperThread = null;
        private ConcurrentQueue<Tampering> _tamperings = new ConcurrentQueue<Tampering>();
        private long _pressedKeys = 0;

        public TamperMachine(Switch device)
        {
            _device = device;
        }

        private void Activate()
        {
            if (_tamperThread == null || !_tamperThread.IsAlive)
            {                _tamperThread = new Thread(this.TamperRunner);
                _tamperThread.Start();
            }
        }

        internal void InstallAtmosphereCheat(IEnumerable<string> rawInstructions, ulong exeAddress, long pid)
        {
            if (!CanInstallOnPid(pid))
            {
                return;
            }

            AtmosphereCompiler compiler = new AtmosphereCompiler();
            TamperProgram program = compiler.Compile(rawInstructions, exeAddress, exeAddress /* TODO */);

            if (program != null)
            {
                _tamperings.Enqueue(new Tampering(pid, program));
            }

            Activate();
        }

        private bool CanInstallOnPid(long pid)
        {
            // Do not allow tampering of kernel processes.
            if (pid < KernelConstants.InitialProcessId)
            {
                Logger.Warning?.Print(LogClass.TamperMachine, $"Refusing to tamper kernel process {pid}");

                return false;
            }

            return true;
        }

        private bool IsProcessValid(KProcess process)
        {
            return process.State != ProcessState.Crashed && process.State != ProcessState.Exiting && process.State != ProcessState.Exited;
        }

        private void TamperRunner()
        {
            Logger.Info?.Print(LogClass.TamperMachine, "TamperMachine thread running");

            int sleepCounter = 0;

            while (true)
            {
                // Sleep to not consume too much CPU.
                if (sleepCounter == 0)
                {
                    sleepCounter = _tamperings.Count;
                    Thread.Sleep(1);
                }
                else
                {
                    sleepCounter--;
                }

                if (_tamperings.TryDequeue(out Tampering tampering))
                {
                    // Get the process associated with the tampering and execute it.
                    if (_device.System.KernelContext.Processes.TryGetValue(tampering.Pid, out KProcess process) && IsProcessValid(process))
                    {
                        // Re-enqueue the tampering because the process is still valid.
                        _tamperings.Enqueue(tampering);

                        Logger.Debug?.Print(LogClass.TamperMachine, $"Running tampering program on process {tampering.Pid}");

                        try
                        {
                            long pressedKeys = Thread.VolatileRead(ref _pressedKeys);

                            // TODO: Mechanism to abort execution if the process exits?
                            tampering.Program.Memory.Value = process.CpuMemory;
                            tampering.Program.PressedKeys.Value = pressedKeys;
                            tampering.Program.EntryPoint.Execute();
                        }
                        catch
                        {
                            Logger.Debug?.Print(LogClass.TamperMachine, $"The tampering program crashed, this can happen during while the game is starting");
                        }
                    }
                }
                else
                {
                    // No more work to be done.

                    Logger.Info?.Print(LogClass.TamperMachine, "TamperMachine thread exiting");

                    return;
                }
            }
        }

        public void UpdateInput(List<GamepadInput> gamepadInputs)
        {
            // Look for the input of the player one.
            foreach (GamepadInput input in gamepadInputs)
            {
                if (input.PlayerId == PlayerIndex.Player1)
                {
                    Thread.VolatileWrite(ref _pressedKeys, (long)input.Buttons);
                    return;
                }
            }

            // Clear the input because player one is not conected.
            // TODO: Fallback to another?
            Thread.VolatileWrite(ref _pressedKeys, 0);
        }
    }
}
