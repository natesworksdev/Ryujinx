using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl.Types;
using Ryujinx.HLE.HOS.Services.Settings;
using System;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvHostCtrl
{
    internal class NvHostCtrlFileDevice : NvFileDevice
    {
        private bool _isProductionMode;

        public NvHostCtrlFileDevice(KProcess owner) : base(owner)
        {
            if (NxSettings.Settings.TryGetValue("nv!rmos_set_production_mode", out object productionModeSetting))
            {
                _isProductionMode = ((string)productionModeSetting) != "0"; // Default value is ""
            }
        }

        public override NvInternalResult Ioctl(NvIoctl command, Span<byte> arguments)
        {
            NvInternalResult result = NvInternalResult.NotImplemented;

            switch (command.GetNumberValue())
            {
                case 0x1b:
                    // As Marshal cannot handle unaligned arrays, we do everything by hand here.
                    NvHostCtrlGetConfigurationArgument configArgument = NvHostCtrlGetConfigurationArgument.FromSpan(arguments);
                    result = GetConfig(configArgument);

                    if (result == NvInternalResult.Success)
                    {
                        configArgument.CopyTo(arguments);
                    }
                    break;
                default:
                    break;
            }

            return result;
        }

        private NvInternalResult GetConfig(NvHostCtrlGetConfigurationArgument arguments)
        {
            if (!_isProductionMode && NxSettings.Settings.TryGetValue($"{arguments.Domain}!{arguments.Parameter}".ToLower(), out object nvSetting))
            {
                byte[] settingBuffer = new byte[0x101];

                if (nvSetting is string stringValue)
                {
                    if (stringValue.Length > 0x100)
                    {
                        Logger.PrintError(LogClass.ServiceNv, $"{arguments.Domain}!{arguments.Parameter} String value size is too big!");
                    }
                    else
                    {
                        settingBuffer = Encoding.ASCII.GetBytes(stringValue + "\0");
                    }
                }
                else if (nvSetting is int intValue)
                {
                    settingBuffer = BitConverter.GetBytes(intValue);
                }
                else if (nvSetting is bool boolValue)
                {
                    settingBuffer[0] = boolValue ? (byte)1 : (byte)0;
                }
                else
                {
                    throw new NotImplementedException(nvSetting.GetType().Name);
                }

                Logger.PrintDebug(LogClass.ServiceNv, $"Got setting {arguments.Domain}!{arguments.Parameter}");

                arguments.Configuration = settingBuffer;

                return NvInternalResult.Success;
            }

            // NOTE: This actually return NotAvailableInProduction but this is directly translated as a InvalidInput before returning the ioctl.
            //return NvInternalResult.NotAvailableInProduction;
            return NvInternalResult.InvalidInput;
        }

        public override void Close()
        {
            // TODO
        }
    }
}
