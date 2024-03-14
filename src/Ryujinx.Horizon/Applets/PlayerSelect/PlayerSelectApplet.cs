using Ryujinx.Common.Memory;
using Ryujinx.Horizon.Common;
using Ryujinx.Horizon.Sdk.Am;
using System;
using System.IO;

namespace Ryujinx.Horizon.Applets.PlayerSelect
{
    internal class PlayerSelectApplet : IApplet
    {
        private AppletSession _normalSession;
#pragma warning disable IDE0052 // Remove unread private member
        private AppletSession _interactiveSession;
#pragma warning restore IDE0052

        public event EventHandler AppletStateChanged;

        public Result Start(AppletSession normalSession, AppletSession interactiveSession)
        {
            _normalSession = normalSession;
            _interactiveSession = interactiveSession;

            // TODO(jduncanator): Parse PlayerSelectConfig from input data
            _normalSession.Push(BuildResponse());

            AppletStateChanged?.Invoke(this, null);

            _system.ReturnFocus();

            return Result.Success;
        }

        public Result GetResult()
        {
            return Result.Success;
        }

        private byte[] BuildResponse()
        {
            UserProfile currentUser = _system.AccountManager.LastOpenedUser;

            using MemoryStream stream = MemoryStreamManager.Shared.GetStream();
            using BinaryWriter writer = new(stream);

            writer.Write((ulong)PlayerSelectResult.Success);

            currentUser.UserId.Write(writer);

            return stream.ToArray();
        }
    }
}
