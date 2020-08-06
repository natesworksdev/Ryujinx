using Ryujinx.Common;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class AccessPoint : IDisposable
    {
        private byte[] _advertiseData;
        private bool _networkCreated;

        private IUserLocalCommunicationService _parent;

        public NetworkInfo NetworkInfo;

        public bool Connected { get; private set; }

        public AccessPoint(IUserLocalCommunicationService parent)
        {
            _parent = parent;

            _parent.NetworkClient.NetworkChange += NetworkChanged;
        }

        public void Dispose()
        {
            _parent.NetworkClient.DisconnectNetwork();

            _parent.NetworkClient.NetworkChange += NetworkChanged;
        }

        private void NetworkChanged(object sender, RyuLdn.NetworkChangeEventArgs e)
        {
            NetworkInfo = e.Info;

            if (Connected != e.Connected)
            {
                Connected = e.Connected;

                _parent.SetState(Connected ? NetworkState.AccessPointCreated : NetworkState.Initialized);
            }
            else
            {
                _parent.SetState();
            }
        }

        public ResultCode SetAdvertiseData(ServiceCtx context)
        {
            long bufferPosition = context.Request.PtrBuff[0].Position;
            long bufferSize     = context.Request.PtrBuff[0].Size;

            if (bufferSize > 0x180)
            {
                return ResultCode.InvalidArgument;
            }

            _advertiseData = new byte[bufferSize];

            context.Memory.Read((ulong)bufferPosition, _advertiseData);

            return ResultCode.Success;
        }

        public ResultCode SetStationAcceptPolicy(ServiceCtx context)
        {
            byte acceptPolicy = context.RequestData.ReadByte();

            _parent.NetworkClient.SetStationAcceptPolicy(acceptPolicy);

            return ResultCode.Success;
        }

        public ResultCode CreateNetwork(ServiceCtx context)
        {
            SecurityConfig securityConfig = context.RequestData.ReadStruct<SecurityConfig>();
            UserConfig     userConfig     = context.RequestData.ReadStruct<UserConfig>();
            uint           reserved       = context.RequestData.ReadUInt32();
            NetworkConfig  networkConfig  = context.RequestData.ReadStruct<NetworkConfig>();

            CreateAccessPointRequest request = new CreateAccessPointRequest
            {
                SecurityConfig = securityConfig,
                UserConfig = userConfig,
                NetworkConfig = networkConfig
            };

            bool success = _parent.NetworkClient.CreateNetwork(request, _advertiseData);

            return success ? ResultCode.Success : ResultCode.InvalidState;
        }
    }
}
