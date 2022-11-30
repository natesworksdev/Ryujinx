using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using System;
using System.IO;

namespace Ryujinx.HLE.HOS.Applets
{
    internal class PlayerSelectApplet : IApplet
    {
        private readonly Horizon _system;

        private AppletSession _normalSession;

        public event EventHandler AppletStateChanged;

        public PlayerSelectApplet(Horizon system)
        {
            _system = system;
        }

        public ResultCode Start(AppletSession normalSession, AppletSession interactiveSession)
        {
            _normalSession = normalSession;

            // TODO(jduncanator): Parse PlayerSelectConfig from input data
            _normalSession.Push(BuildResponse());

            AppletStateChanged?.Invoke(this, null);

            _system.ReturnFocus();

            return ResultCode.Success;
        }

        public ResultCode GetResult()
        {
            return ResultCode.Success;
        }

        private byte[] BuildResponse()
        {
            UserProfile currentUser = _system.AccountManager.LastOpenedUser;

            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write((ulong)PlayerSelectResult.Success);

            currentUser.UserId.Write(writer);

            return stream.ToArray();
        }
    }
}