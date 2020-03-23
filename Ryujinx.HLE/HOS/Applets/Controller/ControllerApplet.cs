using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Hid;

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
            var c = ReadStruct<ControllerSupportArgPrivate>(controllerSupportArgPrivate);

            Logger.PrintStub(LogClass.ServiceHid, $"ControllerApplet ArgPriv {c.PrivateSize} {c.ArgSize} {c.Mode}"+ 
                        $"HoldType:{(HidJoyHoldType)c.NpadJoyHoldType} StyleSets:{(ControllerType)c.NpadStyleSet}");

            if (c.Mode != ControllerSupportMode.ShowControllerSupport)
            {
                _normalSession.Push(BuildResponse());   // Dummy response for other modes
                AppletStateChanged?.Invoke(this, null);

                return ResultCode.Success;
            }

            var controllerSupportArg = _normalSession.Pop();

            ControllerSupportArgHeader h;

            if (c.ArgSize == Marshal.SizeOf<ControllerSupportArg>())
            {
                var arg = ReadStruct<ControllerSupportArg>(controllerSupportArg);
                h = arg.Header;
                // Read enable text here?
            }
            else
            {
                Logger.PrintStub(LogClass.ServiceHid, $"Unknown revision of ControllerSupportArg.");
                h = ReadStruct<ControllerSupportArgHeader>(controllerSupportArg); // Read just the header
            }

            Logger.PrintStub(LogClass.ServiceHid, $"ControllerApplet Arg {h.PlayerCountMin} {h.PlayerCountMax} {h.EnableTakeOverConnection}");

            // Currently, the only purpose of this applet is to help 
            // choose the primary input controller for the game
            // TODO: Ideally should hook back to HID.Controller. When applet is called, can choose appropriate controller and attach to appropriate id.
            if (h.PlayerCountMin > 1)
            {
                Logger.PrintWarning(LogClass.ServiceHid, "Game requested more than 1 controller!");
            }

            var result = new ControllerSupportResultInfo
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

        private byte[] BuildResponse(ControllerSupportResultInfo result)
        {
            UserProfile currentUser = _system.State.Account.LastOpenedUser;

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                writer.Write(MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref result, Marshal.SizeOf<ControllerSupportResultInfo>())));

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
