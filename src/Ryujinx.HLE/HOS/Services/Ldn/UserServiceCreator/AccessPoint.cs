using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using System;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class AccessPoint : IDisposable
    {
        private byte[] _advertiseData;

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

                if (Connected)
                {
                    _parent.SetState(NetworkState.AccessPointCreated);
                } 
                else
                {
                    _parent.SetDisconnectReason(DisconnectReason.DestroyedBySystem);
                }
            }
            else
            {
                _parent.SetState();
            }
        }

        public ResultCode SetAdvertiseData(byte[] advertiseData)
        {
            _advertiseData = advertiseData;

            _parent.NetworkClient.SetAdvertiseData(_advertiseData);

            return ResultCode.Success;
        }

        public ResultCode SetStationAcceptPolicy(AcceptPolicy acceptPolicy)
        {
            _parent.NetworkClient.SetStationAcceptPolicy(acceptPolicy);

            return ResultCode.Success;
        }

        public ResultCode CreateNetwork(SecurityConfig securityConfig, UserConfig userConfig, NetworkConfig networkConfig)
        {
            CreateAccessPointRequest request = new CreateAccessPointRequest
            {
                SecurityConfig = securityConfig,
                UserConfig     = userConfig,
                NetworkConfig  = networkConfig
            };

            bool success = _parent.NetworkClient.CreateNetwork(request, _advertiseData);

            return success ? ResultCode.Success : ResultCode.InvalidState;
        }
    }
}
