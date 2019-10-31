using Ryujinx.HLE.HOS.Services.Am.AppletAE;
using System;
using System.Collections.Generic;
using System.IO;

namespace Ryujinx.HLE.HOS.Applets
{
    internal class PlayerSelect : IApplet
    {
        private Horizon _system;
        private Stack<IStorage> _inputStack;
        private Stack<IStorage> _outputStack;

        public event EventHandler AppletStateChanged;

        public PlayerSelect(Horizon system)
        {
            _system      = system;
            _inputStack  = new Stack<IStorage>();
            _outputStack = new Stack<IStorage>();
        }

        public ResultCode Start()
        {
            _outputStack.Push(new IStorage(BuildResponse()));

            AppletStateChanged?.Invoke(this, null);

            return ResultCode.Success;
        }

        public ResultCode GetResult()
        {
            return ResultCode.Success;
        }

        public ResultCode PushInData(IStorage data)
        {
            _inputStack.Push(data);

            return ResultCode.Success;
        }

        public ResultCode PopOutData(out IStorage data)
        {
            if (_outputStack.Count > 0)
            {
                data = _outputStack.Pop();
            }
            else
            {
                data = null;
            }

            return ResultCode.Success;
        }

        private byte[] BuildResponse()
        {
            var currentUser = _system.State.Account.LastOpenedUser;

            using (var ms = new MemoryStream())
            {
                var writer = new BinaryWriter(ms);

                // Result (0 = Success, 2 = Failure)
                writer.Write(0UL);
                // UserID Low (long) High (long)
                currentUser.UserId.Write(writer);

                return ms.ToArray();
            }
        }
    }
}
