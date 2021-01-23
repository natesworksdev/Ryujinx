using Ryujinx.Common;
using Ryujinx.Common.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Pcv
{
    [Service("pcv")]
    class IPcvService : IpcService
    {
        private readonly IDictionary<Module, ModuleState> moduleStates;
        private readonly IDictionary<PowerDomain, PowerDomainState> powerDomainStates;

        public IPcvService(ServiceCtx context)
        {
            moduleStates = new Dictionary<Module, ModuleState>();
            powerDomainStates = new Dictionary<PowerDomain, PowerDomainState>();
        }

        [Command(0)]
        // SetPowerEnabled(b8, u32)
        public ResultCode SetPowerEnabled(ServiceCtx context)
        {
            Module moduleId = (Module)context.RequestData.ReadUInt32();
            bool enabled = context.RequestData.ReadByte() != 0;

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { moduleId, enabled });

            if (moduleStates.TryGetValue(moduleId, out ModuleState moduleState))
            {
                moduleState.PowerEnabled = enabled;
            }
            else
            {
                moduleState = new ModuleState
                {
                    PowerEnabled = enabled
                };
            }
            moduleStates[moduleId] = moduleState;

            return ResultCode.Success;
        }

        [Command(1)]
        // SetClockEnabled(b8, u32)
        public ResultCode SetClockEnabled(ServiceCtx context)
        {
            Module moduleId = (Module)context.RequestData.ReadUInt32();
            bool enabled = context.RequestData.ReadByte() != 0;

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { moduleId, enabled });

            if (moduleStates.TryGetValue(moduleId, out ModuleState moduleState))
            {
                moduleState.ClockEnabled = enabled;
            }
            else
            {
                moduleState = new ModuleState
                {
                    ClockEnabled = enabled
                };
            }
            moduleStates[moduleId] = moduleState;

            return ResultCode.Success;
        }

        [Command(2)]
        // SetClockRate(u32, u32)
        public ResultCode SetClockRate(ServiceCtx context)
        {
            Module moduleId = (Module)context.RequestData.ReadUInt32();
            uint clockRateHz = context.RequestData.ReadUInt32();

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { moduleId, clockRateHz });

            if (moduleStates.TryGetValue(moduleId, out ModuleState moduleState))
            {
                moduleState.ClockFrequency = clockRateHz;
            }
            else
            {
                moduleState = new ModuleState
                {
                    ClockFrequency = clockRateHz
                };
            }
            moduleStates[moduleId] = moduleState;

            return ResultCode.Success;
        }

        [Command(3)]
        // GetClockRate(u32) -> u32
        public ResultCode GetClockRate(ServiceCtx context)
        {
            Module moduleId = (Module)context.RequestData.ReadUInt32();

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { moduleId });

            uint clockRateHz;
            if (moduleStates.TryGetValue(moduleId, out ModuleState moduleState))
            {
                // Able to get the clock rate.
                clockRateHz = moduleState.ClockFrequency;
            }
            else
            {
                // Clock rate has not been set before, just give 0.
                clockRateHz = 0;
            }
            context.ResponseData.Write(clockRateHz);

            return ResultCode.Success;
        }

        [Command(4)]
        // GetState(u32) -> nn::pcv::ModuleState
        public ResultCode GetState(ServiceCtx context)
        {
            Module moduleId = (Module)context.RequestData.ReadUInt32();
            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { moduleId });

            ModuleState moduleState;
            if (moduleStates.TryGetValue(moduleId, out moduleState))
            {
                // Able to get the module state.
            }
            else
            {
                moduleState = new ModuleState();
                moduleStates[moduleId] = moduleState;
            }

            context.ResponseData.WriteStruct(moduleState);

            return ResultCode.Success;
        }

        // [Command(5)]
        // GetPossibleClockRates(u32, u32) -> (u32, u32, buffer<u32, 0xa>)

        [Command(6)]
        // SetMinVClockRate(u32, u32)
        public ResultCode SetMinVClockRate(ServiceCtx context)
        {
            Module moduleId = (Module)context.RequestData.ReadUInt32();
            uint clockRateHz = context.RequestData.ReadUInt32();
            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { moduleId, clockRateHz });

            if (moduleStates.TryGetValue(moduleId, out ModuleState moduleState))
            {
                moduleState.MinVClockRate = clockRateHz;
            }
            else
            {
                moduleState = new ModuleState
                {
                    MinVClockRate = clockRateHz
                };
            }
            moduleStates[moduleId] = moduleState;

            return ResultCode.Success;
        }

        [Command(7)]
        // SetReset(b8, u32)
        public ResultCode SetReset(ServiceCtx context)
        {
            Module moduleId = (Module)context.RequestData.ReadUInt32();
            bool asserted = context.RequestData.ReadByte() != 0;
            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { moduleId, asserted });

            if (moduleStates.TryGetValue(moduleId, out ModuleState moduleState))
            {
                moduleState.ResetAsserted = asserted;
            }
            else
            {
                moduleState = new ModuleState
                {
                    ResetAsserted = asserted
                };
            }
            moduleStates[moduleId] = moduleState;

            return ResultCode.Success;
        }

        [Command(8)]
        // SetVoltageEnabled(b8, u32)
        public ResultCode SetVoltageEnabled(ServiceCtx context)
        {
            PowerDomain powerDomain = (PowerDomain)context.RequestData.ReadUInt32();
            bool enabled = context.RequestData.ReadByte() != 0;

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerDomain, enabled });

            if (powerDomainStates.TryGetValue(powerDomain, out PowerDomainState powerDomainState))
            {
                powerDomainState.Enabled = enabled;
            }
            else
            {
                powerDomainState = new PowerDomainState
                {
                    Enabled = enabled
                };
            }
            powerDomainStates[powerDomain] = powerDomainState;

            return ResultCode.Success;
        }

        [Command(9)]
        // GetVoltageEnabled(u32) -> b8
        public ResultCode GetVoltageEnabled(ServiceCtx context)
        {
            PowerDomain powerDomain = (PowerDomain)context.RequestData.ReadUInt32();

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerDomain });

            bool enabled;
            if (powerDomainStates.TryGetValue(powerDomain, out PowerDomainState powerDomainState))
            {
                // Able to get the enabled state.
                enabled = powerDomainState.Enabled;
            }
            else
            {
                // Enabled state has not been set before, just give false.
                enabled = false;
            }
            context.ResponseData.Write(enabled);

            return ResultCode.Success;
        }

        // [Command(10)]
        // GetVoltageRange(u32) -> (u32, u32, u32)

        [Command(11)]
        // SetVoltageValue(u32, u32)
        public ResultCode SetVoltageValue(ServiceCtx context)
        {
            PowerDomain powerDomain = (PowerDomain)context.RequestData.ReadUInt32();
            int microVolt = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerDomain, microVolt });

            if (powerDomainStates.TryGetValue(powerDomain, out PowerDomainState powerDomainState))
            {
                powerDomainState.Voltage = microVolt;
            }
            else
            {
                powerDomainState = new PowerDomainState
                {
                    Voltage = microVolt
                };
            }
            powerDomainStates[powerDomain] = powerDomainState;

            return ResultCode.Success;
        }

        [Command(12)]
        // GetVoltageValue(u32) -> u32
        public ResultCode GetVoltageValue(ServiceCtx context)
        {
            PowerDomain powerDomain = (PowerDomain)context.RequestData.ReadUInt32();

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerDomain });

            int microVolt;
            if (powerDomainStates.TryGetValue(powerDomain, out PowerDomainState powerDomainState))
            {
                // Able to get the voltage.
                microVolt = powerDomainState.Voltage;
            }
            else
            {
                // Voltage has not been set before, just give 0.
                microVolt = 0;
            }
            context.ResponseData.Write(microVolt);

            return ResultCode.Success;
        }

        // [Command(13)]
        // GetTemperatureThresholds(u32) -> (u32, buffer<nn::pcv::TemperatureThreshold, 0xa>)

        // [Command(14)]
        // SetTemperature(u32)

        [Command(15)]
        // Initialize()
        public ResultCode Initialize(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServicePcv);
            return ResultCode.Success;
        }

        [Command(16)]
        // IsInitialized() -> b8
        public ResultCode IsInitialized(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServicePcv);
            context.ResponseData.Write(true);
            return ResultCode.Success;
        }

        [Command(17)]
        // Finalize()
        public ResultCode Finalize(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServicePcv);
            return ResultCode.Success;
        }

        [Command(18)]
        // PowerOn(nn::pcv::PowerControlTarget, u32)
        public ResultCode PowerOn(ServiceCtx context)
        {
            PowerControlTarget powerControlTarget = (PowerControlTarget)context.RequestData.ReadUInt32();
            int microVolt = context.RequestData.ReadInt32();
            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerControlTarget, microVolt });
            return ResultCode.Success;
        }

        [Command(19)]
        // PowerOff(nn::pcv::PowerControlTarget)
        public ResultCode PowerOff(ServiceCtx context)
        {
            PowerControlTarget powerControlTarget = (PowerControlTarget)context.RequestData.ReadUInt32();
            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerControlTarget });
            return ResultCode.Success;
        }

        [Command(20)]
        // ChangeVoltage(nn::pcv::PowerControlTarget, u32)
        public ResultCode ChangeVoltage(ServiceCtx context)
        {
            PowerControlTarget powerControlTarget = (PowerControlTarget)context.RequestData.ReadUInt32();
            int microVolt = context.RequestData.ReadInt32();
            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerControlTarget, microVolt });
            return ResultCode.Success;
        }

        // [Command(21)]
        // GetPowerClockInfoEvent() -> handle<copy>

        // [Command(22)]
        // GetOscillatorClock() -> u32

        // [Command(23)]
        // GetDvfsTable(u32, u32) -> (u32, buffer<u32, 0xa>, buffer<u32, 0xa>)

        [Command(24)]
        // GetModuleStateTable(u32) -> (u32, buffer<nn::pcv::ModuleState, 0xa>)
        public unsafe ResultCode GetModuleStateTable(ServiceCtx context)
        {
            int maxCount = context.RequestData.ReadInt32();
            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { maxCount });

            // [7.0.0+] The type-0xA output buffer was replaced with a type-0x22 output buffer.
            (long position, long size) = context.Response.GetBufferType0x22(0);
            byte[] outputBuffer = new byte[size];
            Span<byte> outputBufferSpan = outputBuffer;

            int outCount = 0;
            foreach ((_, ModuleState moduleState) in moduleStates)
            {
                int structSize = Marshal.SizeOf(moduleState);

                if (size < outCount * structSize)
                {
                    Logger.Error?.Print(LogClass.ServicePcv, $"Output buffer size {size} too small!");
                    break;
                }

                if (outCount >= maxCount)
                {
                    break;
                }

                fixed (byte* ptr = outputBufferSpan.Slice(outCount * structSize, structSize))
                {
                    Marshal.StructureToPtr(moduleState, (IntPtr)ptr, false);
                }

                outCount++;
            }
            context.ResponseData.Write(outCount);
            context.Memory.Write((ulong)position, outputBuffer);

            return ResultCode.Success;
        }

        [Command(25)]
        // GetPowerDomainStateTable(u32) -> (u32, buffer<nn::pcv::PowerDomainState, 0xa>)
        public unsafe ResultCode GetPowerDomainStateTable(ServiceCtx context)
        {
            int maxCount = context.RequestData.ReadInt32();
            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { maxCount });

            // [7.0.0+] The type-0xA output buffer was replaced with a type-0x22 output buffer.
            (long position, long size) = context.Response.GetBufferType0x22(0);
            byte[] outputBuffer = new byte[size];
            Span<byte> outputBufferSpan = outputBuffer;

            int outCount = 0;
            foreach ((_, PowerDomainState powerDomainState) in powerDomainStates)
            {
                int structSize = Marshal.SizeOf(powerDomainState);

                if (size < outCount * structSize)
                {
                    Logger.Error?.Print(LogClass.ServicePcv, $"Output buffer size {size} too small!");
                    break;
                }

                if (outCount >= maxCount)
                {
                    break;
                }

                fixed (byte* ptr = outputBufferSpan.Slice(outCount * structSize, structSize))
                {
                    Marshal.StructureToPtr(powerDomainState, (IntPtr)ptr, false);
                }

                outCount++;
            }
            context.ResponseData.Write(outCount);
            context.Memory.Write((ulong)position, outputBuffer);

            return ResultCode.Success;
        }

        // [Command(26)]
        // GetFuseInfo(u32) -> (u32, buffer<u32, 0xa>)

        // [Command(27)]
        // GetDramId()

        [Command(28)]
        // Takes an input PowerControlTarget. Returns an output bool.
        public ResultCode IsPoweredOn(ServiceCtx context)
        {
            PowerControlTarget powerControlTarget = (PowerControlTarget)context.RequestData.ReadUInt32();
            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerControlTarget });

            context.ResponseData.Write(true);

            return ResultCode.Success;
        }

        // [Command(29)]
        // GetVoltage()
    }
}