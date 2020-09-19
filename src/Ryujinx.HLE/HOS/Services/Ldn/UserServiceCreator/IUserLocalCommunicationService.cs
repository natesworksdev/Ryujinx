using LibHac.Ns;
using Ryujinx.Common;
using Ryujinx.Common.Configuration.Multiplayer;
using Ryujinx.Common.Logging;
using Ryujinx.Common.Utilities;
using Ryujinx.Cpu;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using Ryujinx.HLE.HOS.Services.Ldn.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.Network.Types;
using Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator.RyuLdn;
using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Ldn.UserServiceCreator
{
    class IUserLocalCommunicationService : IpcService
    {
        public INetworkClient NetworkClient { get; private set; }

        private const int    NIFM_REQUEST_ID     = 90;
        private const string DEFAULT_IP_ADDRESS  = "127.0.0.1";
        private const string DEFAULT_SUBNET_MASK = "255.255.255.0";
        private const bool   IS_DEVELOPMENT      = false;

        private KEvent _stateChangeEvent;

        private NetworkState     _state;
        private DisconnectReason _disconnectReason;
        private ResultCode       _nifmResultCode;
        private long             _currentPid;

        private AccessPoint _accessPoint;
        private Station     _station;

        public IUserLocalCommunicationService(ServiceCtx context)
        {
            _stateChangeEvent = new KEvent(context.Device.System.KernelContext);
            _state            = NetworkState.None;
            _disconnectReason = DisconnectReason.None;
        }

        private ushort CheckDevelopmentChannel(ushort channel)
        {
            return (ushort)(!IS_DEVELOPMENT ? 0 : channel);
        }

        private SecurityMode CheckDevelopmentSecurityMode(SecurityMode securityMode)
        {
            return !IS_DEVELOPMENT ? SecurityMode.Retail : securityMode;
        }

        private bool CheckLocalCommunicationIdPermission(ServiceCtx context, ulong localCommunicationIdChecked)
        {
            // TODO: Call nn::arp::GetApplicationControlProperty here when implemented.
            ApplicationControlProperty controlProperty = context.Device.Application.ControlData.Value;

            bool isValid = false;

            foreach (ulong localCommunicationId in controlProperty.LocalCommunicationIds)
            {
                if (localCommunicationId == localCommunicationIdChecked)
                {
                    isValid = true;
                }
            }

            return isValid;
        }

        [Command(0)]
        // GetState() -> s32 state
        public ResultCode GetState(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                context.ResponseData.Write((int)NetworkState.Error);

                return ResultCode.Success;
            }

            // NOTE: Return ResultCode.InvalidArgument if _state is null, doesn't occur in our case.
            context.ResponseData.Write((int)_state);

            return ResultCode.Success;
        }

        public void SetState()
        {
            _stateChangeEvent.WritableEvent.Signal();
        }

        public void SetState(NetworkState state)
        {
            _state = state;

            SetState();
        }

        [Command(1)]
        // GetNetworkInfo() -> buffer<network_info<0x480>, 0x1a>
        public ResultCode GetNetworkInfo(ServiceCtx context)
        {
            long bufferPosition = context.Request.RecvListBuff[0].Position;
            long bufferSize     = context.Request.RecvListBuff[0].Size;

            MemoryHelper.FillWithZeros(context.Memory, bufferPosition, (int)bufferSize);

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            ResultCode resultCode = GetNetworkInfoImpl(out NetworkInfo networkInfo);
            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            MemoryHelper.Write(context.Memory, bufferPosition, networkInfo);

            return ResultCode.Success;
        }

        private ResultCode GetNetworkInfoImpl(out NetworkInfo networkInfo)
        {
            if (_state == NetworkState.StationConnected)
            {
                networkInfo = _station.NetworkInfo;
            }
            else if (_state == NetworkState.AccessPointCreated)
            {
                networkInfo = _accessPoint.NetworkInfo;
            }
            else
            {
                networkInfo = new NetworkInfo();

                return ResultCode.InvalidState;
            }

            return ResultCode.Success;
        }

        [Command(2)]
        // GetIpv4Address() -> (u32 ip_address, u32 subnet_mask)
        public ResultCode GetIpv4Address(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            // NOTE: Return ResultCode.InvalidArgument if ip_address and subnet_mask are null, doesn't occur in our case.

            (_, UnicastIPAddressInformation unicastAddress) = NetworkHelpers.GetLocalInterface();

            if (unicastAddress == null)
            {
                context.ResponseData.Write(NetworkHelpers.ConvertIpv4Address(DEFAULT_IP_ADDRESS));
                context.ResponseData.Write(NetworkHelpers.ConvertIpv4Address(DEFAULT_SUBNET_MASK));
            }
            else
            {
                Logger.Info?.Print(LogClass.ServiceLdn, $"Console's LDN IP is \"{unicastAddress.Address}\".");

                context.ResponseData.Write(NetworkHelpers.ConvertIpv4Address(unicastAddress.Address));
                context.ResponseData.Write(NetworkHelpers.ConvertIpv4Address(DEFAULT_SUBNET_MASK));
            }

            return ResultCode.Success;
        }

        [Command(3)]
        // GetDisconnectReason() -> u16 disconnect_reason
        public ResultCode GetDisconnectReason(ServiceCtx context)
        {
            // NOTE: Return ResultCode.InvalidArgument if _disconnectReason is null, doesn't occur in our case.

            context.ResponseData.Write((short)_disconnectReason);
            
            return ResultCode.Success;
        }

        public void SetDisconnectReason(DisconnectReason reason)
        {
            if (_state != NetworkState.Initialized)
            {
                _disconnectReason = reason;

                SetState(NetworkState.Initialized);
            }
        }

        [Command(4)]
        // GetSecurityParameter() -> bytes<0x20, 1> security_parameter
        public ResultCode GetSecurityParameter(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            ResultCode resultCode = GetNetworkInfoImpl(out NetworkInfo networkInfo);
            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            SecurityParameter securityParameter = new SecurityParameter()
            {
                Data      = new byte[0x10],
                SessionId = networkInfo.NetworkId.SessionId
            };

            context.ResponseData.WriteStruct(securityParameter);

            return ResultCode.Success;
        }

        [Command(5)]
        // GetNetworkConfig() -> bytes<0x20, 8> network_config
        public ResultCode GetNetworkConfig(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            ResultCode resultCode = GetNetworkInfoImpl(out NetworkInfo networkInfo);
            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            NetworkConfig networkConfig = new NetworkConfig
            {
                IntentId                  = networkInfo.NetworkId.IntentId,
                Channel                   = networkInfo.Common.Channel,
                NodeCountMax              = networkInfo.Ldn.NodeCountMax,
                LocalCommunicationVersion = networkInfo.Ldn.Nodes[0].LocalCommunicationVersion,
                Reserved2                 = new byte[10]
            };

            context.ResponseData.WriteStruct(networkConfig);

            return ResultCode.Success;
        }

        [Command(100)]
        // AttachStateChangeEvent() -> handle<copy>
        public ResultCode AttachStateChangeEvent(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(_stateChangeEvent.ReadableEvent, out int stateChangeEventHandle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(stateChangeEventHandle);

            // Return ResultCode.InvalidArgument if handle is null, doesn't occur in our case since we already throw an Exception.

            return ResultCode.Success;
        }

        [Command(101)]
        // GetNetworkInfoLatestUpdate() -> (buffer<network_info<0x480>, 0x1a>, buffer<node_latest_update, 0xa>)
        public ResultCode GetNetworkInfoLatestUpdate(ServiceCtx context)
        {
            throw new NotImplementedException();
        }

        [Command(102)]
        // Scan(u16 channel, bytes<0x60, 8> scan_filter) -> (u16 count, buffer<network_info, 0x22>)
        public ResultCode Scan(ServiceCtx context)
        {
            return ScanImpl(context);
        }

        [Command(103)]
        // ScanPrivate(u16 channel, bytes<0x60, 8> scan_filter) -> (u16 count, buffer<network_info, 0x22>)
        public ResultCode ScanPrivate(ServiceCtx context)
        {
            return ScanImpl(context, true);
        }

        private ResultCode ScanImpl(ServiceCtx context, bool isPrivate = false)
        {
            ushort     channel    = (ushort)context.RequestData.ReadUInt64();
            ScanFilter scanFilter = context.RequestData.ReadStruct<ScanFilter>();

            long bufferPosition = context.Request.ReceiveBuff[0].Position;
            long bufferSize     = context.Request.ReceiveBuff[0].Size;

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (!isPrivate)
            {
                channel = CheckDevelopmentChannel(channel);
            }

            ResultCode resultCode = ResultCode.InvalidArgument;

            if (bufferSize != 0)
            {
                if (bufferPosition != 0)
                {
                    ScanFilterFlag scanFilterFlag = scanFilter.Flag;

                    if (!scanFilterFlag.HasFlag(ScanFilterFlag.NetworkType) || scanFilter.NetworkType <= NetworkType.All)
                    {
                        if (scanFilterFlag.HasFlag(ScanFilterFlag.Ssid))
                        {
                            if (scanFilter.Ssid.Length <= 31)
                            {
                                return resultCode;
                            }
                        }

                        if (!scanFilterFlag.HasFlag(ScanFilterFlag.MacAddress))
                        {
                            if (scanFilterFlag > ScanFilterFlag.All)
                            {
                                return resultCode;
                            }

                            if (_state - 3 >= NetworkState.AccessPoint)
                            {
                                resultCode = ResultCode.InvalidState;
                            }
                            else
                            {
                                resultCode = ScanInternal(context.Memory, channel, scanFilter, bufferPosition, bufferSize, out long counter);

                                context.ResponseData.Write(counter);
                            }
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
                }
            }

            return resultCode;
        }

        private ResultCode ScanInternal(MemoryManager memory, ushort channel, ScanFilter scanFilter, long bufferPosition, long bufferSize, out long counter)
        {
            long networkInfoSize = Marshal.SizeOf(typeof(NetworkInfo));
            long maxGames        = bufferSize / networkInfoSize;

            MemoryHelper.FillWithZeros(memory, bufferPosition, (int)bufferSize);

            NetworkInfo[] availableGames = NetworkClient.Scan(channel, scanFilter);

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

        [Command(104)] // 5.0.0+
        // SetWirelessControllerRestriction(u32 wireless_controller_restriction)
        public ResultCode SetWirelessControllerRestriction(ServiceCtx context)
        {
            // NOTE: Return ResultCode.InvalidArgument if an internal IPAddress is null, doesn't occur in our case.

            uint wirelessControllerRestriction = context.RequestData.ReadUInt32();

            if (wirelessControllerRestriction > 1)
            {
                return ResultCode.InvalidArgument;
            }

            if (_state != NetworkState.Initialized)
            {
                return ResultCode.InvalidState;
            }

            // NOTE: WirelessControllerRestriction value is used for the btm service in SetWlanMode call.
            //       Since we use our own implementation we can do nothing here.

            return ResultCode.Success;
        }

        [Command(200)]
        // OpenAccessPoint()
        public ResultCode OpenAccessPoint(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (_state != NetworkState.Initialized)
            {
                return ResultCode.InvalidState;
            }

            SetState(NetworkState.AccessPoint);

            _accessPoint = new AccessPoint(this);

            // NOTE: Calls nifm service and return related result codes.
            //       Since we use our own implementation we can return ResultCode.Success.

            return ResultCode.Success;
        }

        [Command(201)]
        // CloseAccessPoint()
        public ResultCode CloseAccessPoint(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (_state == NetworkState.AccessPoint || _state == NetworkState.AccessPointCreated)
            {
                DestroyNetworkImpl(DisconnectReason.DestroyedByUser);
            }
            else
            {
                return ResultCode.InvalidState;
            }

            SetState(NetworkState.Initialized);

            return ResultCode.Success;
        }

        private void CloseAccessPoint()
        {
            _accessPoint?.Dispose();

            _accessPoint = null;
        }

        [Command(202)]
        // CreateNetwork(bytes<0x44, 2> security_config, bytes<0x30, 1> user_config, bytes<0x20, 8> network_config)
        public ResultCode CreateNetwork(ServiceCtx context)
        {
            return CreateNetworkImpl(context);
        }

        [Command(203)]
        // CreateNetworkPrivate(bytes<0x44, 2> security_config, bytes<0x20, 1> security_parameter, bytes<0x30, 1>, bytes<0x20, 8> network_config, buffer<unknown, 9> address_entry, int count)
        public ResultCode CreateNetworkPrivate(ServiceCtx context)
        {
            return CreateNetworkImpl(context, true);
        }

        public ResultCode CreateNetworkImpl(ServiceCtx context, bool isPrivate = false)
        {
            SecurityConfig    securityConfig    = context.RequestData.ReadStruct<SecurityConfig>();
            SecurityParameter securityParameter = isPrivate ? context.RequestData.ReadStruct<SecurityParameter>() : new SecurityParameter();

            UserConfig userConfig = context.RequestData.ReadStruct<UserConfig>();
            context.RequestData.ReadUInt32(); // Alignment?
            NetworkConfig networkConfig = context.RequestData.ReadStruct<NetworkConfig>();

            bool isLocalCommunicationIdValid = CheckLocalCommunicationIdPermission(context, networkConfig.IntentId.LocalCommunicationId);
            if (!isLocalCommunicationIdValid)
            {
                return ResultCode.InvalidObject;
            }

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            networkConfig.Channel       = CheckDevelopmentChannel(networkConfig.Channel);
            securityConfig.SecurityMode = CheckDevelopmentSecurityMode(securityConfig.SecurityMode);

            if (networkConfig.NodeCountMax <= 8)
            {
                if ((((ulong)networkConfig.LocalCommunicationVersion) & 0x80000000) == 0)
                {
                    if (securityConfig.SecurityMode <= SecurityMode.Retail)
                    {
                        if (securityConfig.Passphrase.Length <= 0x40)
                        {
                            if (_state == NetworkState.AccessPoint)
                            {
                                if (isPrivate)
                                {
                                    long bufferPosition = context.Request.PtrBuff[0].Position;
                                    long bufferSize = context.Request.PtrBuff[0].Size;

                                    byte[] addressListBytes = new byte[bufferSize];

                                    context.Memory.Read((ulong)bufferPosition, addressListBytes);

                                    AddressList addressList = LdnHelper.FromBytes<AddressList>(addressListBytes);

                                    _accessPoint.CreateNetworkPrivate(securityConfig, securityParameter, userConfig, networkConfig, addressList);
                                }
                                else
                                {
                                    _accessPoint.CreateNetwork(securityConfig, userConfig, networkConfig);
                                }

                                return ResultCode.Success;
                            }
                            else
                            {
                                return ResultCode.InvalidState;
                            }
                        }
                    }
                }
            }

            return ResultCode.InvalidArgument;
        }

        [Command(204)]
        // DestroyNetwork()
        public ResultCode DestroyNetwork(ServiceCtx context)
        {
            return DestroyNetworkImpl(DisconnectReason.DestroyedByUser);
        }

        private ResultCode DestroyNetworkImpl(DisconnectReason disconnectReason)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (disconnectReason - 3 <= DisconnectReason.DisconnectedByUser)
            {
                if (_state == NetworkState.AccessPointCreated)
                {
                    CloseAccessPoint();

                    SetState(NetworkState.AccessPoint);

                    return ResultCode.Success;
                }

                return ResultCode.InvalidState;
            }

            return ResultCode.InvalidArgument;
        }

        [Command(205)]
        // Reject(u32 node_id)
        public ResultCode Reject(ServiceCtx context)
        {
            uint nodeId = context.RequestData.ReadUInt32();

            return RejectImpl(DisconnectReason.Rejected, nodeId);
        }

        private ResultCode RejectImpl(DisconnectReason disconnectReason, uint nodeId)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (_state != NetworkState.AccessPointCreated)
            {
                return ResultCode.InvalidState; // Must be network host to reject nodes.
            }

            return NetworkClient.Reject(disconnectReason, nodeId);
        }

        [Command(206)]
        // SetAdvertiseData(buffer<advertise_data, 0x21>)
        public ResultCode SetAdvertiseData(ServiceCtx context)
        {
            long bufferPosition = context.Request.PtrBuff[0].Position;
            long bufferSize     = context.Request.PtrBuff[0].Size;

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (bufferSize == 0 || bufferSize > 0x180)
            {
                return ResultCode.InvalidArgument;
            }

            if (_state == NetworkState.AccessPoint || _state == NetworkState.AccessPointCreated)
            {
                byte[] advertiseData = new byte[bufferSize];

                context.Memory.Read((ulong)bufferPosition, advertiseData);

                return _accessPoint.SetAdvertiseData(advertiseData);
            }
            else
            {
                return ResultCode.InvalidState;
            }
        }

        [Command(207)]
        // SetStationAcceptPolicy(u8 accept_policy)
        public ResultCode SetStationAcceptPolicy(ServiceCtx context)
        {
            AcceptPolicy acceptPolicy = (AcceptPolicy)context.RequestData.ReadByte();

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (acceptPolicy > AcceptPolicy.WhiteList)
            {
                return ResultCode.InvalidArgument;
            }

            if (_state == NetworkState.AccessPoint || _state == NetworkState.AccessPointCreated)
            {
                return _accessPoint.SetStationAcceptPolicy(acceptPolicy);
            }
            else
            {
                return ResultCode.InvalidState;
            }
        }

        [Command(208)]
        // AddAcceptFilterEntry(bytes<6, 1> mac_address)
        public ResultCode AddAcceptFilterEntry(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            throw new NotImplementedException();
        }

        [Command(209)]
        // ClearAcceptFilter()
        public ResultCode ClearAcceptFilter(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            throw new NotImplementedException();
        }

        [Command(300)]
        // OpenStation()
        public ResultCode OpenStation(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (_state != NetworkState.Initialized)
            {
                return ResultCode.InvalidState;
            }

            SetState(NetworkState.Station);

            _station = new Station(this);

            // NOTE: Calls nifm service and return related result codes.
            //       Since we use our own implementation we can return ResultCode.Success.

            return ResultCode.Success;
        }

        [Command(301)]
        // CloseStation()
        public ResultCode CloseStation(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (_state == NetworkState.Station || _state == NetworkState.StationConnected)
            {
                DisconnectImpl(DisconnectReason.DisconnectedByUser);
            }
            else
            {
                return ResultCode.InvalidState;
            }

            SetState(NetworkState.Initialized);

            return ResultCode.Success;
        }

        private void CloseStation()
        {
            _station.Dispose();

            _station = null;
        }

        [Command(302)]
        // Connect(bytes<0x44, 2> security_config, bytes<0x30, 1> user_config, u32 local_communication_version, u32 option_unknown, buffer<network_info<0x480>, 0x19>)
        public ResultCode Connect(ServiceCtx context)
        {
            return ConnectImpl(context);
        }

        [Command(303)]
        // ConnectPrivate(bytes<0x44, 2> security_config, bytes<0x20, 1> security_parameter, bytes<0x30, 1> user_config, u32 local_communication_version, u32 option_unknown, bytes<0x20, 8> network_config)
        public ResultCode ConnectPrivate(ServiceCtx context)
        {
            return ConnectImpl(context, true);
        }

        private ResultCode ConnectImpl(ServiceCtx context, bool isPrivate = false)
        {
            SecurityConfig    securityConfig    = context.RequestData.ReadStruct<SecurityConfig>();
            SecurityParameter securityParameter = isPrivate ? context.RequestData.ReadStruct<SecurityParameter>() : new SecurityParameter();

            UserConfig userConfig                = context.RequestData.ReadStruct<UserConfig>();
            uint       localCommunicationVersion = context.RequestData.ReadUInt32();
            uint       optionUnknown             = context.RequestData.ReadUInt32();

            NetworkConfig networkConfig = new NetworkConfig();
            NetworkInfo   networkInfo   = new NetworkInfo();

            if (isPrivate)
            {
                context.RequestData.ReadUInt32(); // Padding.

                networkConfig = context.RequestData.ReadStruct<NetworkConfig>();
            }
            else
            {
                long bufferPosition = context.Request.PtrBuff[0].Position;
                long bufferSize     = context.Request.PtrBuff[0].Size;

                byte[] networkInfoBytes = new byte[bufferSize];

                context.Memory.Read((ulong)bufferPosition, networkInfoBytes);

                networkInfo = LdnHelper.FromBytes<NetworkInfo>(networkInfoBytes);
            }

            bool isLocalCommunicationIdValid = CheckLocalCommunicationIdPermission(context, networkInfo.NetworkId.IntentId.LocalCommunicationId);
            if (!isLocalCommunicationIdValid)
            {
                return ResultCode.InvalidObject;
            }

            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            securityConfig.SecurityMode = CheckDevelopmentSecurityMode(securityConfig.SecurityMode);

            ResultCode resultCode = ResultCode.InvalidArgument;

            if (securityConfig.SecurityMode - 1 <= SecurityMode.Debug)
            {
                if (optionUnknown <= 1 && (localCommunicationVersion >> 15) == 0 && securityConfig.PassphraseSize <= 48)
                {
                    resultCode = ResultCode.VersionTooLow;
                    if (localCommunicationVersion >= 0)
                    {
                        resultCode = ResultCode.VersionTooHigh;
                        if (localCommunicationVersion <= short.MaxValue)
                        {
                            if (_state != NetworkState.Station)
                            {
                                resultCode = ResultCode.InvalidState;
                            }
                            else
                            {
                                if (isPrivate)
                                {
                                    resultCode = _station.ConnectPrivate(securityConfig, securityParameter, userConfig, localCommunicationVersion, optionUnknown, networkConfig);
                                }
                                else
                                {
                                    resultCode = _station.Connect(securityConfig, userConfig, localCommunicationVersion, optionUnknown, networkInfo);
                                }
                            }
                        }
                    }
                }
            }

            return resultCode;
        }

        [Command(304)]
        // Disconnect()
        public ResultCode Disconnect(ServiceCtx context)
        {
            return DisconnectImpl(DisconnectReason.DisconnectedByUser);
        }

        private ResultCode DisconnectImpl(DisconnectReason disconnectReason)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            if (disconnectReason <= DisconnectReason.DisconnectedBySystem)
            {
                if (_state == NetworkState.StationConnected)
                {
                    SetState(NetworkState.Station);

                    CloseStation();

                    _disconnectReason = disconnectReason;

                    return ResultCode.Success;
                }

                return ResultCode.InvalidState;
            }

            return ResultCode.InvalidArgument;
        }

        [Command(400)]
        // InitializeOld(pid)
        public ResultCode InitializeOld(ServiceCtx context)
        {
            return InitializeImpl(context, context.Process.Pid, NIFM_REQUEST_ID);
        }

        [Command(401)]
        // Finalize()
        public ResultCode Finalize(ServiceCtx context)
        {
            if (_nifmResultCode != ResultCode.Success)
            {
                return _nifmResultCode;
            }

            // NOTE: Use true when its called in nn::ldn::detail::ISystemLocalCommunicationService
            ResultCode resultCode = FinalizeImpl(false);
            if (resultCode == ResultCode.Success)
            {
                SetDisconnectReason(DisconnectReason.None);
            }

            return resultCode;
        }

        private ResultCode FinalizeImpl(bool isCausedBySystem)
        {
            DisconnectReason disconnectReason;

            switch (_state)
            {
                case NetworkState.None: return ResultCode.Success;
                case NetworkState.AccessPoint:
                    {
                        CloseAccessPoint();

                        break;
                    }
                case NetworkState.AccessPointCreated:
                    {
                        if (isCausedBySystem)
                        {
                            disconnectReason = DisconnectReason.DestroyedBySystem;
                        }
                        else
                        {
                            disconnectReason = DisconnectReason.DestroyedByUser;
                        }

                        DestroyNetworkImpl(disconnectReason);

                        break;
                    }
                case NetworkState.Station:
                    {
                        CloseStation();

                        break;
                    }
                case NetworkState.StationConnected:
                    {
                        if (isCausedBySystem)
                        {
                            disconnectReason = DisconnectReason.DisconnectedBySystem;
                        }
                        else
                        {
                            disconnectReason = DisconnectReason.DisconnectedByUser;
                        }

                        DisconnectImpl(disconnectReason);

                        break;
                    }
            }

            SetState(NetworkState.None);

            NetworkClient?.DisconnectAndStop();
            NetworkClient = null;

            return ResultCode.Success;
        }

        [Command(402)] // 7.0.0+
        // Initialize(u64 ip_addresses, pid)
        public ResultCode Initialize(ServiceCtx context)
        {
            IPAddress ipAddress  = new IPAddress(context.RequestData.ReadUInt32());
            IPAddress subnetMask = new IPAddress(context.RequestData.ReadUInt32());

            // NOTE: It seems the guest can get ip_address and subnet_mask from nifm service and pass it through the initialize.
            //       This call twice InitializeImpl(): A first time with NIFM_REQUEST_ID, if its failed a second time with nifm_request_id = 1.
            //       Since we proxy connections, we don't needs to get the ip_address, subnet_mask and nifm_request_id.

            return InitializeImpl(context, context.Process.Pid, NIFM_REQUEST_ID);
        }

        public ResultCode InitializeImpl(ServiceCtx context, long pid, int nifmRequestId)
        {
            ResultCode resultCode = ResultCode.InvalidArgument;

            if (nifmRequestId <= 255)
            {
                if (_state != NetworkState.Initialized)
                {
                    // NOTE: Service calls nn::ldn::detail::NetworkInterfaceManager::NetworkInterfaceMonitor::Initialize() with nifmRequestId as argument.
                    //       Then it stores the result code of it in a global field. Since we use our own implementation, we can just check the connection
                    //       and return related error codes.
                    if (System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable())
                    {
                        MultiplayerMode mode = ConfigurationState.Instance.Multiplayer.Mode;
                        switch (mode)
                        {
                            case MultiplayerMode.Disabled:
                                NetworkClient = new DisabledLdnClient();
                                break;
                        }

                        NetworkClient.SetGameVersion(context.Device.Application.ControlData.Value.DisplayVersion.Value.ToArray());

                        resultCode = ResultCode.Success;

                        _nifmResultCode = resultCode;
                        _currentPid     = pid;

                        SetState(NetworkState.Initialized);
                    }
                    else
                    {
                        // NOTE: Service returns differents ResultCode here related to the nifm ResultCode.
                        resultCode = ResultCode.DeviceDisabled;

                        _nifmResultCode = resultCode;
                    }
                }
            }

            return resultCode;
        }
    }
}
