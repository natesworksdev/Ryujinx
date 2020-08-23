using Ryujinx.Cpu;
using Ryujinx.Cpu.Jit;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn.Types;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class Station : IDisposable
    {
        public NetworkInfo NetworkInfo;

        private IUserLocalCommunicationService _parent;

        public bool Connected { get; private set; }

        public Station(IUserLocalCommunicationService parent)
        {
            _parent = parent;

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
                    _parent.SetState(NetworkState.StationConnected);
                }
                else
                {
                    _parent.SetDisconnectReason(DisconnectReason.SignalLost);
                }
            }
            else
            {
                _parent.SetState();
            }
        }

        public void Dispose()
        {
            _parent.NetworkClient.DisconnectNetwork();

            _parent.NetworkClient.NetworkChange -= NetworkChanged;
        }

        public ResultCode Scan(MemoryManager memory, ushort channel, ScanFilter scanFilter, long bufferPosition, long bufferSize, out long counter)
        {
            long networkInfoSize = Marshal.SizeOf(typeof(NetworkInfo));
            long maxGames        = bufferSize / networkInfoSize;

            MemoryHelper.FillWithZeros(memory, bufferPosition, (int)bufferSize);

            NetworkInfo[] availableGames = _parent.NetworkClient.Scan(channel, scanFilter);

            counter = 0;

            foreach (NetworkInfo networkInfo in availableGames)
            {
                MemoryHelper.Write(memory, bufferPosition + (networkInfoSize * counter), networkInfo);

                if (++counter >= maxGames)
                {
                    break;
                }
            }

            return ResultCode.Success;
        }

        public ResultCode Connect(SecurityConfig securityConfig, UserConfig userConfig, uint localCommunicationVersion, uint optionUnknown, NetworkInfo networkInfo)
        {
            ConnectRequest request = new ConnectRequest
            {
                SecurityConfig            = securityConfig,
                UserConfig                = userConfig,
                LocalCommunicationVersion = localCommunicationVersion,
                OptionUnknown             = optionUnknown,
                NetworkInfo               = networkInfo
            };

            return _parent.NetworkClient.Connect(request) switch
            {
                NetworkError.None           => ResultCode.Success,
                NetworkError.VersionTooLow  => ResultCode.VersionTooLow,
                NetworkError.VersionTooHigh => ResultCode.VersionTooHigh,
                NetworkError.TooManyPlayers => ResultCode.TooManyPlayers,
                _                           => ResultCode.DeviceNotAvailable
            };
        }
    }
}
