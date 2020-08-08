using Ryujinx.Common;
using Ryujinx.Cpu;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using System;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class Station : IDisposable
    {
        public NetworkConfig CurrentNetworkConfig;
        public NetworkInfo   CurrentNetworkInfo;

        private IUserLocalCommunicationService _parent;

        public bool Connected { get; private set; }

        public Station(IUserLocalCommunicationService parent)
        {
            _parent = parent;

            _parent.NetworkClient.NetworkChange += NetworkChanged;
        }

        private void NetworkChanged(object sender, RyuLdn.NetworkChangeEventArgs e)
        {
            CurrentNetworkInfo = e.Info;

            if (Connected != e.Connected)
            {
                Connected = e.Connected;

                if (Connected)
                {
                    CurrentNetworkConfig = new NetworkConfig
                    {
                        IntentId = CurrentNetworkInfo.NetworkId.IntentId,
                        Channel = CurrentNetworkInfo.Common.Channel,
                        NodeCountMax = CurrentNetworkInfo.Ldn.NodeCountMax,
                        Unknown1 = 0x00,
                        LocalCommunicationVersion = (ushort)CurrentNetworkInfo.NetworkId.IntentId.LocalCommunicationId,
                        Unknown2 = new byte[10]
                    };

                    _parent.SetState(NetworkState.StationConnected);
                }
                else
                {
                    _parent.SignalDisconnect(DisconnectReason.SignalLost);
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

        public ResultCode Scan(ServiceCtx context)
        {
            uint       channel     = context.RequestData.ReadUInt32();
            uint       bufferCount = context.RequestData.ReadUInt32();
            ScanFilter scanFilter  = context.RequestData.ReadStruct<ScanFilter>();

            long bufferPosition = context.Request.ReceiveBuff[0].Position;
            long bufferSize     = context.Request.ReceiveBuff[0].Size;

            long networkInfoSize = Marshal.SizeOf(typeof(NetworkInfo));
            long maxGames = bufferSize / networkInfoSize;

            MemoryHelper.FillWithZeros(context.Memory, bufferPosition, (int)bufferSize);

            NetworkInfo[] availableGames = _parent.NetworkClient.Scan(channel, bufferCount, scanFilter);

            int counter = 0;

            foreach (NetworkInfo networkInfo in availableGames)
            {
                MemoryHelper.Write(context.Memory, bufferPosition + (networkInfoSize * counter), networkInfo);

                if (++counter >= maxGames)
                {
                    break;
                }
            }

            context.ResponseData.Write((long)counter);

            return ResultCode.Success;
        }

        public ResultCode Connect(ServiceCtx context)
        {
            ConnectNetworkData connectNetworkData = context.RequestData.ReadStruct<ConnectNetworkData>();

            long bufferPosition = context.Request.PtrBuff[0].Position;
            long bufferSize     = context.Request.PtrBuff[0].Size;

            byte[] networkInfoBytes = new byte[bufferSize];

            context.Memory.Read((ulong)bufferPosition, networkInfoBytes);

            NetworkInfo networkInfo = LdnHelper.FromBytes<NetworkInfo>(networkInfoBytes);

            ConnectRequest request = new ConnectRequest
            {
                Data = connectNetworkData,
                Info = networkInfo
            };

            return _parent.NetworkClient.Connect(request) ? ResultCode.Success : ResultCode.InvalidState;
        }
    }
}
