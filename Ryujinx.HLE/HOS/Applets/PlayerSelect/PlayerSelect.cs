using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ryujinx.HLE.HOS.Applets
{
    internal class PlayerSelect : IApplet
    {
        private Horizon         _system;
        private Queue<IStorage> _inputQueue;
        private Queue<IStorage> _outputQueue;

        public event EventHandler AppletStateChanged;

        public PlayerSelect(Horizon system)
        {
            _system      = system;
            _inputQueue  = new Queue<IStorage>();
            _outputQueue = new Queue<IStorage>();
        }

        public ResultCode Start()
        {
            _outputQueue.Enqueue(new IStorage(BuildResponse()));

            AppletStateChanged?.Invoke(this, null);

            return ResultCode.Success;
        }

        public ResultCode GetResult()
        {
            return ResultCode.Success;
        }

        public ResultCode PushInData(IStorage data)
        {
            _inputQueue.Enqueue(data);

            return ResultCode.Success;
        }

        public ResultCode PopOutData(out IStorage data)
        {
            if (_outputQueue.Count > 0)
            {
                data = _outputQueue.Dequeue();
            }
            else
            {
                data = null;
            }

            return ResultCode.Success;
        }

        private byte[] BuildResponse()
        {
            UserProfile currentUser = _system.State.Account.LastOpenedUser;

            using (MemoryStream ms = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(ms);

                writer.Write((ulong)PlayerSelectResult.Success);
                // UserID Low (long) High (long)
                currentUser.UserId.Write(writer);

                return ms.ToArray();
            }
        }
    }
}
