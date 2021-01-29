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
        private bool isInitialized;
        private readonly IDictionary<Module, ModuleState> moduleStates;
        private readonly IDictionary<PowerDomain, PowerDomainState> powerDomainStates;
        private readonly IDictionary<PowerControlTarget, (bool isPoweredOn, int microVolt)> powerControlTargetPoweredOnStatus;

        public IPcvService(ServiceCtx context)
        {
            isInitialized = false;
            moduleStates = new Dictionary<Module, ModuleState>();
            powerDomainStates = new Dictionary<PowerDomain, PowerDomainState>();
            powerControlTargetPoweredOnStatus = new Dictionary<PowerControlTarget, (bool, int)>();
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

        [Command(15)]
        // Initialize()
        public ResultCode Initialize(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServicePcv);

            isInitialized = true;

            return ResultCode.Success;
        }

        [Command(16)]
        // IsInitialized() -> b8
        public ResultCode IsInitialized(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServicePcv);

            context.ResponseData.Write(isInitialized);

            return ResultCode.Success;
        }

        [Command(17)]
        // Finalize()
        public ResultCode Finalize(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServicePcv);

            moduleStates.Clear();
            powerDomainStates.Clear();
            powerControlTargetPoweredOnStatus.Clear();
            isInitialized = false;

            return ResultCode.Success;
        }

        [Command(18)]
        // PowerOn(nn::pcv::PowerControlTarget, u32)
        public ResultCode PowerOn(ServiceCtx context)
        {
            PowerControlTarget powerControlTarget = (PowerControlTarget)context.RequestData.ReadUInt32();
            int microVolt = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerControlTarget, microVolt });

            if (powerControlTargetPoweredOnStatus.TryGetValue(powerControlTarget, out (bool isPoweredOn, int microVolt) t))
            {
                t.isPoweredOn = true;
            }
            else
            {
                t = (true, default);
            }

            powerControlTargetPoweredOnStatus[powerControlTarget] = t;

            return ResultCode.Success;
        }

        [Command(19)]
        // PowerOff(nn::pcv::PowerControlTarget)
        public ResultCode PowerOff(ServiceCtx context)
        {
            PowerControlTarget powerControlTarget = (PowerControlTarget)context.RequestData.ReadUInt32();

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerControlTarget });

            if (powerControlTargetPoweredOnStatus.TryGetValue(powerControlTarget, out (bool isPoweredOn, int microVolt) t))
            {
                t.isPoweredOn = false;
            }
            else
            {
                t = (false, default);
            }

            powerControlTargetPoweredOnStatus[powerControlTarget] = t;

            return ResultCode.Success;
        }

        [Command(20)]
        // ChangeVoltage(nn::pcv::PowerControlTarget, u32)
        public ResultCode ChangeVoltage(ServiceCtx context)
        {
            PowerControlTarget powerControlTarget = (PowerControlTarget)context.RequestData.ReadUInt32();
            int microVolt = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerControlTarget, microVolt });

            if (powerControlTargetPoweredOnStatus.TryGetValue(powerControlTarget, out (bool isPoweredOn, int microVolt) t))
            {
                t.microVolt = microVolt;
            }
            else
            {
                t = (default, microVolt);
            }

            powerControlTargetPoweredOnStatus[powerControlTarget] = t;

            return ResultCode.Success;
        }

        [Command(24)]
        // GetModuleStateTable(u32) -> (u32, buffer<nn::pcv::ModuleState, 0xa>)
        public ResultCode GetModuleStateTable(ServiceCtx context)
        {
            int maxCount = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { maxCount });

            // [7.0.0+] The type-0xA output buffer was replaced with a type-0x22 output buffer.
            (long bufferPosition, long bufferSize) = context.Response.GetBufferType0x22(0);

            int outCount = 0;
            foreach ((_, ModuleState moduleState) in moduleStates)
            {
                int structSize = Marshal.SizeOf(moduleState);

                if (bufferSize < outCount * structSize)
                {
                    Logger.Error?.Print(LogClass.ServicePcv, $"Output buffer size {bufferSize} too small!");
                    break;
                }

                if (outCount >= maxCount)
                {
                    break;
                }

                context.Memory.Write((ulong)bufferPosition, moduleState);
                bufferPosition += structSize;

                outCount++;
            }

            context.ResponseData.Write(outCount);

            return ResultCode.Success;
        }

        [Command(25)]
        // GetPowerDomainStateTable(u32) -> (u32, buffer<nn::pcv::PowerDomainState, 0xa>)
        public ResultCode GetPowerDomainStateTable(ServiceCtx context)
        {
            int maxCount = context.RequestData.ReadInt32();

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { maxCount });

            // [7.0.0+] The type-0xA output buffer was replaced with a type-0x22 output buffer.
            (long bufferPosition, long bufferSize) = context.Response.GetBufferType0x22(0);

            int outCount = 0;
            foreach ((_, PowerDomainState powerDomainState) in powerDomainStates)
            {
                int structSize = Marshal.SizeOf(powerDomainState);

                if (bufferSize < outCount * structSize)
                {
                    Logger.Error?.Print(LogClass.ServicePcv, $"Output buffer size {bufferSize} too small!");
                    break;
                }

                if (outCount >= maxCount)
                {
                    break;
                }

                context.Memory.Write((ulong)bufferPosition, powerDomainState);
                bufferPosition += structSize;

                outCount++;
            }

            context.ResponseData.Write(outCount);

            return ResultCode.Success;
        }

        [Command(28)]
        // IsPoweredOn(nn::pcv::PowerControlTarget) -> b8
        public ResultCode IsPoweredOn(ServiceCtx context)
        {
            PowerControlTarget powerControlTarget = (PowerControlTarget)context.RequestData.ReadUInt32();

            Logger.Stub?.PrintStub(LogClass.ServicePcv, new { powerControlTarget });

            bool isPoweredOn;
            if (powerControlTargetPoweredOnStatus.TryGetValue(powerControlTarget, out (bool isPoweredOn, int microVolt) t))
            {
                // Able to get the power on status.
                isPoweredOn = t.isPoweredOn;
            }
            else
            {
                // Power on status has not been set before, just give false.
                isPoweredOn = false;
            }

            context.ResponseData.Write(isPoweredOn);

            return ResultCode.Success;
        }
    }
}