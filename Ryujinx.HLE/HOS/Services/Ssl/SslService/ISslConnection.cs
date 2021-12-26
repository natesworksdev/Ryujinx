using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Services.Sockets.Bsd;
using Ryujinx.HLE.HOS.Services.Ssl.Types;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Ssl.SslService
{
    class ISslConnection : IpcService
    {
        public ISslConnection() { }

        private uint socketFd;
        private IoMode ioMode;
        private VerifyOption verifyOption;
        private string HostName;
        private BsdSocket Socket;
        private SslStream Stream;
        private byte[] NextAplnProto;
        // I don't think the implementation is entirely correct here...
        private Dictionary<OptionType, bool> Options = new();

        [CommandHipc(0)]
        // SetSocketDescriptor(u32) -> u32
        public ResultCode SetSocketDescriptor(ServiceCtx context)
        {
            socketFd = context.RequestData.ReadUInt32();
            uint duplicateSocketFd = 0; // TODO: properly DuplicateSocket?

            if (Options.ContainsKey(OptionType.DoNotCloseSocket) && Options[OptionType.DoNotCloseSocket] == true)
            {
                context.ResponseData.Write(-1);
            }
            else
            {
                context.ResponseData.Write(duplicateSocketFd);
            }

            Logger.Info?.Print(LogClass.ServiceSsl, $"Creating SSL connection for {socketFd}");

            Socket = IClient.RetrieveSocket((int)socketFd);

            return ResultCode.Success;
        }

        [CommandHipc(1)]
        // SetHostName(buffer<bytes, 5>)
        public ResultCode SetHostName(ServiceCtx context)
        {
            ulong hostNameDataPosition = context.Request.SendBuff[0].Position;
            ulong hostNameDataSize = context.Request.SendBuff[0].Size;

            byte[] hostNameData = new byte[hostNameDataSize];

            context.Memory.Read(hostNameDataPosition, hostNameData);

            HostName = Encoding.ASCII.GetString(hostNameData).Trim('\0');

            Logger.Info?.Print(LogClass.ServiceSsl, HostName);

            return ResultCode.Success;
        }

        [CommandHipc(2)]
        // SetVerifyOption(nn::ssl::sf::VerifyOption)
        public ResultCode SetVerifyOption(ServiceCtx context)
        {
            verifyOption = (VerifyOption)context.RequestData.ReadUInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceSsl, new { verifyOption });

            return ResultCode.Success;
        }

        [CommandHipc(3)]
        // SetIoMode(nn::ssl::sf::IoMode)
        public ResultCode SetIoMode(ServiceCtx context)
        {
            ioMode = (IoMode)context.RequestData.ReadUInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceSsl, new { ioMode });

            return ResultCode.Success;
        }

        [CommandHipc(4)]
        // GetSocketDescriptor() -> u32
        public ResultCode GetSocketDescriptor(ServiceCtx context)
        {
            context.ResponseData.Write(socketFd);

            return ResultCode.Success;
        }

        [CommandHipc(5)]
        // GetHostName(buffer<bytes, 6>) -> u32
        public ResultCode GetHostName(ServiceCtx context)
        {
            ulong hostNameDataPosition = context.Request.ReceiveBuff[0].Position;
            ulong hostNameDataSize = context.Request.ReceiveBuff[0].Size;

            byte[] hostNameData = new byte[hostNameDataSize];

            Encoding.ASCII.GetBytes(HostName, hostNameData);

            context.Memory.Write(hostNameDataPosition, hostNameData);

            context.ResponseData.Write((uint)HostName.Length);

            Logger.Info?.Print(LogClass.ServiceSsl, HostName);

            return ResultCode.Success;
        }

        [CommandHipc(6)]
        // GetVerifyOption() -> nn::ssl::sf::VerifyOption
        public ResultCode GetVerifyOption(ServiceCtx context)
        {
            context.ResponseData.Write((uint)verifyOption);

            Logger.Stub?.PrintStub(LogClass.ServiceSsl, new { verifyOption });

            return ResultCode.Success;
        }

        [CommandHipc(7)]
        // GetIoMode() -> nn::ssl::sf::IoMode
        public ResultCode GetIoMode(ServiceCtx context)
        {
            context.ResponseData.Write((uint)ioMode);

            Logger.Stub?.PrintStub(LogClass.ServiceSsl, new { ioMode });

            return ResultCode.Success;
        }

        [CommandHipc(8)]
        // DoHandshake()
        public ResultCode DoHandshake(ServiceCtx context)
        {
            Logger.Info?.Print(LogClass.ServiceSsl, $"Handshaking as {HostName}");
            try
            {
                Stream = new SslStream(new NetworkStream(((DefaultSocket)Socket.Handle).Socket, false), false, null, null);
                Stream.AuthenticateAsClient(HostName);
            }
            catch (Exception ex)
            {
                Logger.Error?.Print(LogClass.ServiceSsl, $"Failed to handshake SSL connection: {ex}");
                // return error to guest?
            }

            return ResultCode.Success;
        }

        [CommandHipc(9)]
        // DoHandshakeGetServerCert(buffer<bytes, 6>) -> u32, u32
        public ResultCode DoHandshakeGetServerCert(ServiceCtx context)
        {
            // Call DoHandshake
            ResultCode HandshakeResult = DoHandshake(context);
            if (HandshakeResult == ResultCode.Success)
            {
                // TODO: is this actually correct?
                byte[] CertData = Stream.RemoteCertificate.GetRawCertData();
                ulong outputDataPosition = context.Request.ReceiveBuff[0].Position;
                ulong outputDataSize = context.Request.ReceiveBuff[0].Size;

                context.Memory.Write(outputDataPosition, CertData);

                Logger.Stub?.Print(LogClass.ServiceSsl, $"Got cert of {CertData.Length} length");

                context.ResponseData.Write(CertData.Length);
                context.ResponseData.Write(1);
            }

            return ResultCode.Success;
        }

        [CommandHipc(10)]
        // Read(buffer<bytes, 6>) -> u32
        public ResultCode Read(ServiceCtx context)
        {
            ulong outputDataPosition = context.Request.ReceiveBuff[0].Position;
            ulong outputDataSize = context.Request.ReceiveBuff[0].Size;

            byte[] data = new byte[outputDataSize];

            // TODO: catch exceptions and report the error to the guest
            int transferredSize = Stream.Read(data, 0, (int)outputDataSize);

            context.Memory.Write(outputDataPosition, data);

            context.ResponseData.Write(transferredSize);

            return ResultCode.Success;
        }

        [CommandHipc(11)]
        // Write(buffer<bytes, 5>) -> u32
        public ResultCode Write(ServiceCtx context)
        {
            ulong inputDataPosition = context.Request.SendBuff[0].Position;
            ulong inputDataSize = context.Request.SendBuff[0].Size;

            byte[] data = new byte[inputDataSize];

            context.Memory.Read(inputDataPosition, data);

            Logger.Info?.Print(LogClass.ServiceSsl, $"Writing {inputDataSize} bytes to server");

            // TODO: catch exceptions and report the error to the guest
            Stream.Write(data);

            // NOTE: Tell the guest everything is transferred, since SslStream doesn't give us this info
            uint transferredSize = (uint)inputDataSize;

            context.ResponseData.Write(transferredSize);

            return ResultCode.Success;
        }

        [CommandHipc(12)]
        // Pending() -> u32
        public ResultCode Pending(ServiceCtx context)
        {
            Logger.Stub?.PrintStub(LogClass.ServiceSsl);

            context.ResponseData.Write(0);

            return ResultCode.Success;
        }

        [CommandHipc(17)]
        // SetSessionCacheMode(nn::ssl::sf::SessionCacheMode)
        public ResultCode SetSessionCacheMode(ServiceCtx context)
        {
            SessionCacheMode sessionCacheMode = (SessionCacheMode)context.RequestData.ReadUInt32();

            Logger.Stub?.PrintStub(LogClass.ServiceSsl, new { sessionCacheMode });

            return ResultCode.Success;
        }

        [CommandHipc(22)]
        // SetOption(b8, nn::ssl::sf::OptionType)
        public ResultCode SetOption(ServiceCtx context)
        {
            bool optionEnabled = context.RequestData.ReadBoolean();
            OptionType optionType = (OptionType)context.RequestData.ReadUInt32();

            Options[optionType] = optionEnabled;

            Logger.Info?.Print(LogClass.ServiceSsl, $"{optionType} = {optionEnabled}");

            return ResultCode.Success;
        }

        [CommandHipc(23)]
        // GetOption(nn::ssl::sf::OptionType) -> b8
        public ResultCode GetOption(ServiceCtx context)
        {
            OptionType optionType = (OptionType)context.RequestData.ReadUInt32();

            bool optionEnabled = Options.ContainsKey(optionType) ? Options[optionType] : false; // default is false?

            Logger.Info?.Print(LogClass.ServiceSsl, $"{optionType} = {optionEnabled}");

            context.ResponseData.Write(optionEnabled);

            return ResultCode.Success;
        }

        [CommandHipc(26)]
        // SetNextAlpnProto(buffer<bytes, 5>) -> u32
        public ResultCode SetNextAlpnProto(ServiceCtx context)
        {
            ulong inputDataPosition = context.Request.SendBuff[0].Position;
            ulong inputDataSize = context.Request.SendBuff[0].Size;

            NextAplnProto = new byte[inputDataSize];

            context.Memory.Read(inputDataPosition, NextAplnProto);

            Logger.Stub?.PrintStub(LogClass.ServiceSsl, new { inputDataSize });

            return ResultCode.Success;
        }

        [CommandHipc(27)]
        // GetNextAlpnProto(buffer<bytes, 6>) -> u32
        public ResultCode GetNextAlpnProto(ServiceCtx context)
        {
            ulong outputDataPosition = context.Request.ReceiveBuff[0].Position;
            ulong outputDataSize = context.Request.ReceiveBuff[0].Size;

            context.Memory.Write(outputDataPosition, NextAplnProto);

            context.ResponseData.Write(NextAplnProto.Length);

            Logger.Stub?.PrintStub(LogClass.ServiceSsl, new { outputDataSize });

            return ResultCode.Success;
        }
    }
}