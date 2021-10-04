using Ryujinx.Common.Logging;
using Ryujinx.HLE.FileSystem.Content;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Services.Spl.Types;

namespace Ryujinx.HLE.HOS.Services.Spl
{
    [Service("spl:")]
    [Service("spl:es")]
    [Service("spl:fs")]
    [Service("spl:manu")]
    [Service("spl:mig")]
    [Service("spl:ssl")]
    class IGeneralInterface : IpcService
    {
        public IGeneralInterface(ServiceCtx context) { }

        [CommandHipc(0)]
        // GetConfig(u32 config_item) -> u64 config_value
        public ResultCode GetConfig(ServiceCtx context)
        {
            ConfigItem configItem = (ConfigItem)context.RequestData.ReadUInt32();

            // NOTE: Nintendo explicitly blacklists package2 hash here, amusingly.
            //       This is not blacklisted in safemode, but we're never in safe mode...
            if (configItem == ConfigItem.Package2Hash)
            {
                return ResultCode.InvalidArguments;
            }

            // TODO: This should call svcCallSecureMonitor using arg 0xC3000002.
            //       Since it's currently not implemented we can use a private method for now.
            SmcResult result = SmcGetConfig(context, out ulong configValue, configItem);

            // Nintendo has some special handling here for hardware type/is_retail.
            if (result == SmcResult.InvalidArgument)
            {
                switch (configItem)
                {
                    case ConfigItem.HardwareType:
                        configValue = (ulong)HardwareType.Icosa;
                        result = SmcResult.Success;
                        break;
                    case ConfigItem.HardwareState:
                        configValue = (ulong)HardwareState.Development;
                        result = SmcResult.Success;
                        break;
                    default:
                        break;
                }
            }

            context.ResponseData.Write(configValue);

            return (ResultCode)((int)result << 9) | ResultCode.ModuleId;
        }

        private SmcResult SmcGetConfig(ServiceCtx context, out ulong configValue, ConfigItem configItem)
        {
            configValue = default;

            SystemVersion version = context.Device.System.ContentManager.GetCurrentFirmwareVersion();

            switch (configItem)
            {
                case ConfigItem.DisableProgramVerification:
                    configValue = 0;
                    break;
                case ConfigItem.DramId:
                    if (context.Device.Configuration.MemoryConfiguration.ToKernelMemorySize() == MemorySize.MemorySize8GB)
                    {
                        configValue = (ulong)DramId.IowaSamsung8GB;
                    }
                    else if (context.Device.Configuration.MemoryConfiguration.ToKernelMemorySize() == MemorySize.MemorySize6GB)
                    {
                        configValue = (ulong)DramId.IcosaSamsung6GB;
                    }
                    else
                    {
                        configValue = (ulong)DramId.IcosaSamsung4GB;
                    }
                    break;
                case ConfigItem.SecurityEngineInterruptNumber: 
                    return SmcResult.NotImplemented;
                case ConfigItem.FuseVersion: 
                    return SmcResult.NotImplemented;
                case ConfigItem.HardwareType:
                    configValue = (ulong)HardwareType.Icosa;
                    break;
                case ConfigItem.HardwareState:
                    configValue = (ulong)HardwareState.Production;
                    break;
                case ConfigItem.IsRecoveryBoot:
                    configValue = 0;
                    break;
                case ConfigItem.DeviceId: 
                    return SmcResult.NotImplemented;
                case ConfigItem.BootReason:
                    // This was removed in firmware 4.0.0.
                    return SmcResult.InvalidArgument;
                case ConfigItem.MemoryMode:
                    configValue = (ulong)context.Device.Configuration.MemoryConfiguration;
                    break;
                case ConfigItem.IsDevelopmentFunctionEnabled:
                    configValue = 0;
                    break;
                case ConfigItem.KernelConfiguration:
                    return SmcResult.NotImplemented;
                case ConfigItem.IsChargerHiZModeEnabled:
                    return SmcResult.NotImplemented;
                case ConfigItem.QuestState:
                    return SmcResult.NotImplemented;
                case ConfigItem.RegulatorType:
                    return SmcResult.NotImplemented;
                case ConfigItem.DeviceUniqueKeyGeneration:
                    return SmcResult.NotImplemented;
                case ConfigItem.Package2Hash:
                    return SmcResult.NotImplemented;
                case ConfigItem.ExosphereApiVersion:
                    // Get information about the current exosphere version.
                    configValue = ((ulong)(1 & 0xFF)            << 56) |
                                  ((ulong)(1 & 0xFF)            << 48) |
                                  ((ulong)(1 & 0xFF)            << 40) |
                                  ((ulong)0                     << 32) | // KeyGeneration
                                  ((ulong)version.Major         << 24) |
                                  ((ulong)version.Minor         << 16) |
                                  ((ulong)version.Micro         << 8)  |
                                  ((ulong)version.RevisionMajor << 0);
                    break;
                case ConfigItem.ExosphereNeedsReboot:
                    // We are executing, so we aren't in the process of rebooting.
                    configValue = 0;
                    break;
                case ConfigItem.ExosphereNeedsShutdown:
                    // We are executing, so we aren't in the process of shutting down.
                    configValue = 0;
                    break;
                case ConfigItem.ExosphereGitCommitHash:
                    // Get information about the current exosphere git commit hash.
                    configValue = 0;
                    break;
                case ConfigItem.ExosphereHasRcmBugPatch:
                    // Get information about whether this unit has the RCM bug patched.
                    configValue = 1;
                    break;
                case ConfigItem.ExosphereBlankProdInfo:
                    // Get whether this unit should simulate a "blanked" PRODINFO.
                    configValue = 0;
                    break;
                case ConfigItem.ExosphereAllowCalWrites:
                    // Get whether this unit should allow writing to the calibration partition.
                    configValue = 1;
                    break;
                case ConfigItem.ExosphereEmummcType:
                    // Get what kind of emummc this unit has active.
                    configValue = 0;
                    break;
                case ConfigItem.ExospherePayloadAddress:
                    // Gets the physical address of the reboot payload buffer, if one exists.
                    return SmcResult.NotInitialized;
                case ConfigItem.ExosphereLogConfiguration:
                    // Get the log configuration.
                    configValue = 0;
                    break;
                case ConfigItem.ExosphereForceEnableUsb30:
                    // Get whether usb 3.0 should be force-enabled.
                    configValue = 1;
                    break;
                case ConfigItem.ExosphereSupportedHosVersion:
                    // Get information about the supported hos version.
                    configValue = ((ulong)(version.Major & 0xFF) << 24) |
                                  ((ulong)(version.Minor & 0xFF) << 16) |
                                  ((ulong)(version.Micro & 0xFF) << 8);
                    break;
                default:
                    return SmcResult.InvalidArgument;
            }

            return SmcResult.Success;
        }
    }
}