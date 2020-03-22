using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Ryujinx.Common.Logging;

namespace Ryujinx.HLE.HOS.Applets
{
    internal class ControllerApplet : IApplet
    {
        private Horizon _system;

        private AppletSession _normalSession;
        private AppletSession _interactiveSession;

        public event EventHandler AppletStateChanged;

        public ControllerApplet(Horizon system)
        {
            _system = system;
        }

        unsafe public ResultCode Start(AppletSession normalSession,
                                AppletSession interactiveSession)
        {
            _normalSession = normalSession;
            _interactiveSession = interactiveSession;

            var _ = _normalSession.Pop();   // unknown

            var controllerSupportArgPrivate = _normalSession.Pop();
            var c = ReadStruct<HidLaControllerSupportArgPrivate>(controllerSupportArgPrivate);

            Logger.PrintStub(LogClass.ServiceHid, $"ControllerApplet ArgPriv {c.PrivateSize} {c.ArgSize} {c.Mode}"+ 
                        $"HoldType:{(HLE.Input.HidJoyHoldType)c.NpadJoyHoldType} StyleSets:{(HLE.Input.ControllerType)c.NpadStyleSet}");

            if (c.Mode != HidLaControllerSupportMode.ShowControllerSupport)
            {
                _normalSession.Push(BuildResponse());   // Dummy response for other modes
                AppletStateChanged?.Invoke(this, null);

                return ResultCode.Success;
            }

            var controllerSupportArg = _normalSession.Pop();

            HidLaControllerSupportArgHeader h;

            if (c.ArgSize == Marshal.SizeOf<HidLaControllerSupportArg>())
            {
                var arg = ReadStruct<HidLaControllerSupportArg>(controllerSupportArg);
                h = arg.Header;
                // Read enable text here?
            }
            else if (c.ArgSize == Marshal.SizeOf<HidLaControllerSupportArgV3>())
            {
                var arg = ReadStruct<HidLaControllerSupportArgV3>(controllerSupportArg);
                h = arg.Header;
                // Read enable text here?
            }
            else
            {
                Logger.PrintStub(LogClass.ServiceHid, $"Unknown revision of HidLaControllerSupportArg.");
                h = ReadStruct<HidLaControllerSupportArgHeader>(controllerSupportArg); // Read just the header
            }

            Logger.PrintStub(LogClass.ServiceHid, $"ControllerApplet Arg {h.PlayerCountMin} {h.PlayerCountMax} {h.EnableTakeOverConnection}");

            if (h.PlayerCountMin > 1)
            {
                // TODO: Ideally should hook back to Input.HID.Controller
                Logger.PrintWarning(LogClass.ServiceHid, "Game requested more than 1 controller!");
            }

            // Currently, the only purpose of this applet is to help 
            // choose the primary input controller for the game
            var result = new HidLaControllerSupportResultInfo
            {
                PlayerCount = 1,
                SelectedId = (uint)HLE.HOS.Services.Hid.HidServer.HidUtils.GetNpadIdTypeFromIndex(_system.Device.Hid.Npads.PrimaryControllerId)
            };

            Logger.PrintStub(LogClass.ServiceHid, $"ControllerApplet ReturnResult {result.PlayerCount} {result.SelectedId}");

            _normalSession.Push(BuildResponse(result));
            AppletStateChanged?.Invoke(this, null);

            return ResultCode.Success;
        }

        public ResultCode GetResult()
        {
            return ResultCode.Success;
        }

        private byte[] BuildResponse(HidLaControllerSupportResultInfo result)
        {
            UserProfile currentUser = _system.State.Account.LastOpenedUser;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref result, Marshal.SizeOf<HidLaControllerSupportResultInfo>())));

                currentUser.UserId.Write(writer);

                return stream.ToArray();
            }
        }

        private byte[] BuildResponse()
        {
            UserProfile currentUser = _system.State.Account.LastOpenedUser;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write((ulong)PlayerSelectResult.Success);

                currentUser.UserId.Write(writer);

                return stream.ToArray();
            }
        }

        private static T ReadStruct<T>(byte[] data)
            where T : struct
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            try
            {
                return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }
    }
}
