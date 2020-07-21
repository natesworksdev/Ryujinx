using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.Cpu;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class IUserLocalCommunicationService : IpcService
    {
        private const int RequestId = 90;

        private KEvent _stateChangeEvent;
        private int    _stateChangeEventHandle = 0;

        private NetworkState     _state;
        private DisconnectReason _disconnectReason;
        private bool             _initialized;

        private AccessPoint _accessPoint;
        private Station     _station;

        public IUserLocalCommunicationService(ServiceCtx context)
        {
            _stateChangeEvent = new KEvent(context.Device.System.KernelContext);
            _state            = NetworkState.None;
            _disconnectReason = DisconnectReason.None;
        }

        [Command(0)]
        // GetState() -> s32 state
        public ResultCode GetState(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            // Return ResultCode.InvalidArgument if _state is null, doesn't occur in our case.

            context.ResponseData.Write((int)_state);

            return ResultCode.Success;
        }

        [Command(1)]
        // GetNetworkInfo() -> buffer<unknown<0x480>, 0x1a>
        public ResultCode GetNetworkInfo(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            long bufferPosition = context.Request.RecvListBuff[0].Position;
            long bufferSize = context.Request.RecvListBuff[0].Size;

            MemoryHelper.FillWithZeros(context.Memory, bufferPosition, (int)bufferSize);

            if (_state == NetworkState.AccessPointCreated)
            {
                MemoryHelper.Write(context.Memory, bufferPosition, _accessPoint.NetworkInfo);
            }
            else if (_state == NetworkState.StationConnected)
            {
                MemoryHelper.Write(context.Memory, bufferPosition, _station.CurrentNetworkInfo);
            }
            else
            {
                throw new NotImplementedException();
            }

            return ResultCode.Success;
        }

        [Command(2)]
        // GetIpv4Address() -> (u32, u32)
        public ResultCode GetIpv4Address(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            // Return ResultCode.InvalidArgument if _disconnectReason is null, doesn't occur in our case.

            context.ResponseData.Write((127 << 0) |   (0 << 8) |   (0 << 16) | (1 << 24));
            context.ResponseData.Write((255 << 0) | (255 << 8) | (255 << 16) | (0 << 24));

            return ResultCode.Success;
        }

        [Command(3)]
        // GetDisconnectReason() -> u16
        public ResultCode GetDisconnectReason(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            // Return ResultCode.InvalidArgument if _disconnectReason is null, doesn't occur in our case.

            context.ResponseData.Write((short)_disconnectReason);
            
            return ResultCode.Success;
        }

        [Command(4)]
        // GetSecurityParameter() -> bytes<0x20, 1>
        public ResultCode GetSecurityParameter(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            if (_state == NetworkState.AccessPointCreated)
            {
                SecurityParameter securityParameter = new SecurityParameter()
                {
                    Unknown = new byte[0x10],
                    SessionId = _accessPoint.NetworkInfo.NetworkId.SessionId
                };

                context.ResponseData.WriteStruct(securityParameter);
            }
            else
            {
                throw new NotImplementedException();
            }

            return ResultCode.Success;
        }

        [Command(5)]
        // GetNetworkConfig() -> bytes<0x20, 8>
        public ResultCode GetNetworkConfig(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            if (_state == NetworkState.StationConnected)
            {
                NetworkConfig networkConfig = _station.CurrentNetworkConfig;

                context.ResponseData.WriteStruct(networkConfig);
            }
            else
            {
                throw new NotImplementedException();
            }

            return ResultCode.Success;
        }

        [Command(100)]
        // AttachStateChangeEvent() -> handle<copy>
        public ResultCode AttachStateChangeEvent(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            if (_stateChangeEventHandle == 0)
            {
                if (context.Process.HandleTable.GenerateHandle(_stateChangeEvent.ReadableEvent, out _stateChangeEventHandle) != KernelResult.Success)
                {
                    throw new InvalidOperationException("Out of handles!");
                }
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(_stateChangeEventHandle);

            // Return ResultCode.InvalidArgument if handle is null, doesn't occur in our case since we already throw an Exception.

            return ResultCode.Success;
        }

        [Command(102)]
        // Scan(u16, bytes<0x60, 8>) -> (u16, buffer<unknown, 0x22>)
        public ResultCode Scan(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            if (_initialized && _state == NetworkState.Station)
            {
                return _station.Scan(context);
            }

            return ResultCode.InvalidArgument;
        }

        [Command(200)]
        // OpenAccessPoint()
        public ResultCode OpenAccessPoint(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            if (_initialized)
            {
                _state = NetworkState.AccessPoint;

                _stateChangeEvent.WritableEvent.Signal();

                _accessPoint = new AccessPoint("127.0.0.1", 4242, _stateChangeEvent);

                _state = NetworkState.AccessPointCreated;

                _stateChangeEvent.WritableEvent.Signal();

                return ResultCode.Success;
            }

            return ResultCode.InvalidArgument;
        }

        [Command(202)]
        // CreateNetwork(bytes<0x44, 2>, bytes<0x30, 1>, bytes<0x20, 8>)
        public ResultCode CreateNetwork(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);
            
            // TODO: check state

            _accessPoint.CreateNetwork(context);

            return ResultCode.Success;
        }

        [Command(206)]
        // SetAdvertiseData(buffer<unknown, 0x21>)
        public ResultCode SetAdvertiseData(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            // TODO: check state

            return _accessPoint.SetAdvertiseData(context);
        }

        [Command(300)]
        // OpenStation()
        public ResultCode OpenStation(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            if (_initialized)
            {
                _state            = NetworkState.Station;
                _disconnectReason = DisconnectReason.None;

                _station          = new Station("127.0.0.1", 4242);

                _stateChangeEvent.WritableEvent.Signal();

                return ResultCode.Success;
            }

            return ResultCode.InvalidArgument;
        }

        [Command(301)]
        // CloseStation()
        public ResultCode CloseStation(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            if (_state == NetworkState.Station || _state == NetworkState.StationConnected)
            {
                _station.DisconnectAndStop();

                _state = NetworkState.Initialized;

                _stateChangeEvent.WritableEvent.Signal();
            }

            return ResultCode.Success;
        }

        [Command(302)]
        //Connect(bytes<0x44, 2>, bytes<0x30, 1>, u32, u32, buffer<unknown<0x480>, 0x19>)
        public ResultCode Connect(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            if (_state == NetworkState.Station)
            {
                ResultCode resultCode = _station.Connect(context);

                if (resultCode != ResultCode.Success)
                {
                    return resultCode;
                }

                _state = NetworkState.StationConnected;

                _stateChangeEvent.WritableEvent.Signal();

                return ResultCode.Success;
            }

            return ResultCode.InvalidArgument;
        }

        [Command(400)]
        // InitializeOld(u64, pid)
        public ResultCode InitializeOld(ServiceCtx context)
        {
            return InitializeImpl(RequestId);
        }

        /*
        [Command(401)]
        // Finalize()
        public ResultCode Finalize(ServiceCtx context)
        {
            return _networkInterface.Finalize();
        }

        [Command(402)] // 7.0.0+
        // Initialize(u64 ip_addresses, u64, pid)
        public ResultCode Initialize(ServiceCtx context)
        {
            // TODO(Ac_K): Determine what addresses are.
            IPAddress unknownAddress1 = new IPAddress(context.RequestData.ReadUInt32());
            IPAddress unknownAddress2 = new IPAddress(context.RequestData.ReadUInt32());

            return _networkInterface.Initialize(ClientId, version: 1, unknownAddress1, unknownAddress2);
        }
        */

        public ResultCode InitializeImpl(int requestId)
        {
            Logger.PrintStub(LogClass.ServiceLdn);

            // requestId is related to Nifm, we don't use it.
            if (requestId > 255 || _initialized)
            {
                return ResultCode.InvalidArgument;
            }

            // Initialiaze the sockets here.

            _initialized = true;

            _state = NetworkState.Initialized;

            _stateChangeEvent.WritableEvent.Signal();

            return ResultCode.Success;
        }
    }
}