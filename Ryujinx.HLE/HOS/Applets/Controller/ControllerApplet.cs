using System;
using System.Collections.Generic;
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

            Logger.Stub?.PrintStub(LogClass.ServiceHid, $"ControllerApplet ArgPriv {privateArg.PrivateSize} {privateArg.ArgSize} {privateArg.Mode} " +
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

                Logger.Stub?.PrintStub(LogClass.ServiceHid, $"ControllerSupportArg Version 7 EnableExplainText={arg.EnableExplainText != 0}");
            }
            else if (privateArg.ArgSize == Marshal.SizeOf<ControllerSupportArgVPre7>())
            {
                ControllerSupportArgVPre7 arg = IApplet.ReadStruct<ControllerSupportArgVPre7>(controllerSupportArg);
                argHeader = arg.Header;
                texts = arg.GetExplainTexts();
                colors = arg.GetIdentificationColors();

                Logger.Stub?.PrintStub(LogClass.ServiceHid, $"ControllerSupportArg Version Pre-7 EnableExplainText={arg.EnableExplainText != 0}");
            }
            else
            {
                Logger.Stub?.PrintStub(LogClass.ServiceHid, $"ControllerSupportArg Version Unknown");

                argHeader = IApplet.ReadStruct<ControllerSupportArgHeader>(controllerSupportArg); // Read just the header
            }

            bool enableSingleMode = argHeader.EnableSingleMode != 0;
            int playerMin = argHeader.PlayerCountMin;
            int playerMax = argHeader.PlayerCountMax;
            ControllerType supportedStyleSet = (ControllerType)privateArg.NpadStyleSet;
            List<PlayerIndex> supportedPlayers = new List<PlayerIndex>(9);

            if (enableSingleMode)
            {
                playerMin = playerMax = 1; // Force single player if enableSingleMode is true
            }
            else
            {
                supportedStyleSet &= ~ControllerType.Handheld; // Remove handheld if it's false
            }

            Logger.Stub?.PrintStub(LogClass.ServiceHid, $"ControllerApplet Arg {playerMin} {playerMax} {enableSingleMode} {argHeader.EnableTakeOverConnection}");

            ControllerAppletUiArgs uiArgs = new ControllerAppletUiArgs
            {
                PlayerCountMin = playerMin,
                PlayerCountMax = playerMax,
                SupportedStyles = supportedStyleSet,
                SupportedPlayers = supportedPlayers,
                IsDocked = _system.State.DockedMode,
                IsSinglePlayer = enableSingleMode,
                IdentificationColors = colors,
                ExplainTexts = texts
            };

            bool valid = false;
            int configuredCount = 0;
            PlayerIndex primaryIndex = PlayerIndex.Unknown;

            do
            {
                _system.Device.UiHandler.DisplayMessageDialog(uiArgs);
                valid = _system.Device.Hid.Npads.Validate(playerMin, playerMax, enableSingleMode, supportedStyleSet, supportedPlayers, out configuredCount, out primaryIndex);
            } while (!valid);

            // TODO: Add validation to Settings Window
            if (!valid || primaryIndex == PlayerIndex.Unknown)
            {
                Logger.Warning?.Print(LogClass.ServiceHid, $"Cancelled Controller Applet without resolving issue. Application may crash.");
            }

            ControllerSupportResultInfo result = new ControllerSupportResultInfo
            {
                PlayerCount = (sbyte)configuredCount,
                SelectedId = (uint)GetNpadIdTypeFromIndex(primaryIndex)
            };

            Logger.Stub?.PrintStub(LogClass.ServiceHid, $"ControllerApplet ReturnResult {result.PlayerCount} {result.SelectedId}");

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
