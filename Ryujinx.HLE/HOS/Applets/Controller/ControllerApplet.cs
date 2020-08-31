using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Hid;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;

using static Ryujinx.HLE.HOS.Services.Hid.HidServer.HidUtils;

namespace Ryujinx.HLE.HOS.Applets
{
    internal class ControllerApplet : IApplet
    {
        private Horizon _system;

        private AppletSession _normalSession;

        public event EventHandler AppletStateChanged;

        public ControllerApplet(Horizon system)
        {
            _system = system;
        }

        unsafe public ResultCode Start(AppletSession normalSession,
                                       AppletSession interactiveSession)
        {
            _normalSession = normalSession;

            byte[] launchParams = _normalSession.Pop();
            byte[] controllerSupportArgPrivate = _normalSession.Pop();
            ControllerSupportArgPrivate privateArg = IApplet.ReadStruct<ControllerSupportArgPrivate>(controllerSupportArgPrivate);

            Logger.Stub?.Print(LogClass.ServiceHid, $"ControllerApplet ArgPriv {privateArg.PrivateSize} {privateArg.ArgSize} {privateArg.Mode} " +
                        $"HoldType:{(NpadJoyHoldType)privateArg.NpadJoyHoldType} StyleSets:{(ControllerType)privateArg.NpadStyleSet}");

            if (privateArg.Mode != ControllerSupportMode.ShowControllerSupport)
            {
                _normalSession.Push(BuildResponse()); // Dummy response for other modes
                AppletStateChanged?.Invoke(this, null);

                return ResultCode.Success;
            }

            byte[] controllerSupportArg = _normalSession.Pop();

            ControllerSupportArgHeader argHeader;
            string[] texts = new string[0];
            uint[] colors = new uint[0];

            if (privateArg.ArgSize == Marshal.SizeOf<ControllerSupportArgV7>())
            {
                ControllerSupportArgV7 arg = IApplet.ReadStruct<ControllerSupportArgV7>(controllerSupportArg);
                argHeader = arg.Header;
                texts = arg.GetExplainTexts();
                colors = arg.GetIdentificationColors();

                Logger.Stub?.Print(LogClass.ServiceHid, $"ControllerSupportArg Version 7 EnableExplainText={arg.EnableExplainText != 0}");
            }
            else if (privateArg.ArgSize == Marshal.SizeOf<ControllerSupportArgVPre7>())
            {
                ControllerSupportArgVPre7 arg = IApplet.ReadStruct<ControllerSupportArgVPre7>(controllerSupportArg);
                argHeader = arg.Header;
                texts = arg.GetExplainTexts();
                colors = arg.GetIdentificationColors();

                Logger.Stub?.Print(LogClass.ServiceHid, $"ControllerSupportArg Version Pre-7 EnableExplainText={arg.EnableExplainText != 0}");
            }
            else
            {
                Logger.Stub?.PrintStub(LogClass.ServiceHid, "ControllerSupportArg Version Unknown");

                argHeader = IApplet.ReadStruct<ControllerSupportArgHeader>(controllerSupportArg); // Read just the header
            }

            bool enableSingleMode = argHeader.EnableSingleMode != 0;
            int playerMin = argHeader.PlayerCountMin;
            int playerMax = argHeader.PlayerCountMax;
            ControllerType supportedStyleSet = (ControllerType)privateArg.NpadStyleSet;

            if (enableSingleMode)
            {
                playerMin = playerMax = 1; // Force single player if enableSingleMode is true
            }

            Logger.Stub?.Print(LogClass.ServiceHid, "ControllerApplet Arg", new { playerMin, playerMax, enableSingleMode, argHeader.EnableTakeOverConnection, argHeader.EnablePermitJoyDual });

            ControllerAppletUiArgs uiArgs = new ControllerAppletUiArgs
            {
                PlayerCountMin = playerMin,
                PlayerCountMax = playerMax,
                SupportedStyles = supportedStyleSet,
                IsSinglePlayer = enableSingleMode,
                PermitJoyDual = argHeader.EnablePermitJoyDual != 0,
                IdentificationColors = colors,
                ExplainTexts = texts
            };

            bool valid = false;
            int configuredCount = 0;
            PlayerIndex primaryIndex = PlayerIndex.Unknown;

            do
            {
                uiArgs.IsDocked = _system.State.DockedMode;
                bool isHHPlayerSupported = _system.Device.Hid.Npads.GetSupportedPlayers()[(int)PlayerIndex.Handheld];

                if (!enableSingleMode || uiArgs.IsDocked || !isHHPlayerSupported)
                {
                    uiArgs.SupportedStyles = supportedStyleSet & ~ControllerType.Handheld; // Remove handheld if it's false
                }

                _system.Device.UiHandler.DisplayControllerApplet(uiArgs);

                // TODO: Add validation to Settings Window
                valid = _system.Device.Hid.Npads.ValidateApplet(playerMin, playerMax, enableSingleMode, uiArgs.SupportedStyles, out configuredCount, out primaryIndex);
            } while (!valid);

            if (!valid || primaryIndex == PlayerIndex.Unknown)
            {
                Logger.Warning?.Print(LogClass.ServiceHid, "Cancelled Controller Applet without resolving issue. Application may crash.");
            }

            if (argHeader.EnableTakeOverConnection == 0)
            {
                _system.Device.Hid.Npads.RequestRemap();
            }

            ControllerSupportResultInfo result = new ControllerSupportResultInfo
            {
                PlayerCount = (sbyte)configuredCount,
                SelectedId = (uint)GetNpadIdTypeFromIndex(primaryIndex)
            };

            Logger.Stub?.PrintStub(LogClass.ServiceHid, $"ControllerApplet ReturnResult", new { result.PlayerCount, result.SelectedId });

            _normalSession.Push(BuildResponse(result));
            AppletStateChanged?.Invoke(this, null);

            return ResultCode.Success;
        }

        public ResultCode GetResult()
        {
            return ResultCode.Success;
        }

        private byte[] BuildResponse(ControllerSupportResultInfo result)
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref result, Unsafe.SizeOf<ControllerSupportResultInfo>())));

                return stream.ToArray();
            }
        }

        private byte[] BuildResponse()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((ulong)ResultCode.Success);

                return stream.ToArray();
            }
        }
    }
}
