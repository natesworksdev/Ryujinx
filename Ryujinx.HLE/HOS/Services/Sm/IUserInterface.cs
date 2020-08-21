using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.Horizon.Kernel;
using System.Collections.Concurrent;
using System.IO;

namespace Ryujinx.HLE.HOS.Services.Sm
{
    class IUserInterface : IpcService
    {
        private readonly ConcurrentDictionary<string, int> _registeredServices;

        private bool _isInitialized;

        public IUserInterface(Switch device)
        {
            _registeredServices = new ConcurrentDictionary<string, int>();

            TrySetServer(new ServerBase(device, "SmServer") { SmObject = this });
        }

        [Command(0)]
        // Initialize(pid, u64 reserved)
        public ResultCode Initialize(ServiceCtx context)
        {
            _isInitialized = true;

            return ResultCode.Success;
        }

        [Command(1)]
        // GetService(ServiceName name) -> handle<move, session>
        public ResultCode GetService(ServiceCtx context)
        {
            if (!_isInitialized)
            {
                return ResultCode.NotInitialized;
            }

            string name = ReadName(context);

            if (string.IsNullOrEmpty(name))
            {
                return ResultCode.InvalidName;
            }

            if (_registeredServices.TryGetValue(name, out int clientPortHandle))
            {
                KernelStatic.Syscall.ConnectToPort(clientPortHandle, out int clientSessionHandle);

                context.Response.HandleDesc = IpcHandleDesc.MakeMove(clientSessionHandle);
            }
            else
            {
                return ResultCode.NotRegistered;
            }

            return ResultCode.Success;
        }

        [Command(2)]
        // RegisterService(ServiceName name, u8, u32 maxHandles) -> handle<move, port>
        public ResultCode RegisterService(ServiceCtx context)
        {
            if (!_isInitialized)
            {
                return ResultCode.NotInitialized;
            }

            long namePosition = context.RequestData.BaseStream.Position;

            string name = ReadName(context);

            context.RequestData.BaseStream.Seek(namePosition + 8, SeekOrigin.Begin);

            bool isLight = (context.RequestData.ReadInt32() & 1) != 0;

            int maxSessions = context.RequestData.ReadInt32();

            if (string.IsNullOrEmpty(name))
            {
                return ResultCode.InvalidName;
            }

            Logger.Info?.Print(LogClass.ServiceSm, $"Register \"{name}\".");

            KernelStatic.Syscall.CreatePort(maxSessions, isLight, 0, out int serverPortHandle, out int clientPortHandle);

            if (!_registeredServices.TryAdd(name, clientPortHandle))
            {
                return ResultCode.AlreadyRegistered;
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeMove(serverPortHandle);

            return ResultCode.Success;
        }

        [Command(3)]
        // UnregisterService(ServiceName name)
        public ResultCode UnregisterService(ServiceCtx context)
        {
            if (!_isInitialized)
            {
                return ResultCode.NotInitialized;
            }

            string name = ReadName(context);

            if (string.IsNullOrEmpty(name))
            {
                return ResultCode.InvalidName;
            }

            if (!_registeredServices.TryRemove(name, out _))
            {
                return ResultCode.NotRegistered;
            }

            return ResultCode.Success;
        }

        private static string ReadName(ServiceCtx context)
        {
            string name = string.Empty;

            for (int index = 0; index < 8 &&
                context.RequestData.BaseStream.Position <
                context.RequestData.BaseStream.Length; index++)
            {
                byte chr = context.RequestData.ReadByte();

                if (chr >= 0x20 && chr < 0x7f)
                {
                    name += (char)chr;
                }
            }

            return name;
        }
    }
}