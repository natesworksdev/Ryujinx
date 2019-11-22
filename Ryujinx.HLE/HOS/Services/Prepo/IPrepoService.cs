using Ryujinx.Common;
using Ryujinx.Common.Logging;
using Ryujinx.HLE.Utilities;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Ryujinx.HLE.HOS.Services.Prepo
{
    [Service("prepo:a")]
    [Service("prepo:a2")]
    [Service("prepo:u")]
    class IPrepoService : IpcService
    {
        private bool _withUserId;

        public IPrepoService(ServiceCtx context) { }

        [Command(10100)] // 1.0.0-5.1.0
        // SaveReport(u64, pid, buffer<u8, 9>, buffer<bytes, 5>)
        public ResultCode SaveReportOld(ServiceCtx context)
        {
            _withUserId = false;

            // We don't care about the differences since we don't use the play report.
            return SaveReportWithUser(context);
        }

        [Command(10101)] // 1.0.0-5.1.0
        // SaveReportWithUserOld(nn::account::Uid, u64, pid, buffer<u8, 9>, buffer<bytes, 5>)
        public ResultCode SaveReportWithUserOld(ServiceCtx context)
        {
            _withUserId = true;

            // We don't care about the differences since we don't use the play report.
            return SaveReportWithUser(context);
        }

        [Command(10102)] // 6.0.0+
        // SaveReport(u64, pid, buffer<u8, 9>, buffer<bytes, 5>)
        public ResultCode SaveReport(ServiceCtx context)
        {
            _withUserId = false;

            // We don't care about the differences since we don't use the play report.
            return SaveReportWithUser(context);
        }

        [Command(10103)] // 6.0.0+
        // SaveReportWithUser(nn::account::Uid, u64, pid, buffer<u8, 9>, buffer<bytes, 5>)
        public ResultCode SaveReportWithUser(ServiceCtx context)
        {
            UInt128 userId   = _withUserId ? new UInt128(context.RequestData.ReadBytes(0x10)) : new UInt128();
            string  gameRoom = StringUtils.ReadUtf8String(context);

            if (_withUserId)
            {
                if (userId.IsNull)
                {
                    return ResultCode.InvalidArgument;
                }
            }

            if (gameRoom == "")
            {
                return ResultCode.InvalidState;
            }

            long inputPosition = context.Request.SendBuff[0].Position;
            long inputSize     = context.Request.SendBuff[0].Size;

            if (inputSize == 0)
            {
                return ResultCode.InvalidBufferSize;
            }

            byte[] inputBuffer = context.Memory.ReadBytes(inputPosition, inputSize);

            Logger.PrintInfo(LogClass.ServicePrepo, ReadReportBuffer(inputBuffer, gameRoom, userId));

            return ResultCode.Success;
        }

        public string ReadReportBuffer(byte[] buffer, string room, UInt128 userId)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("\nPlayReport log:");

            if (_withUserId)
            {
                sb.AppendLine($" UserId: {userId.ToString()}");
            }

            sb.AppendLine($" Room: {room}");

            using (MemoryStream stream = new MemoryStream(buffer))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                byte  unknown1 = reader.ReadByte();  // Version ?
                short unknown2 = reader.ReadInt16(); // Size ?

                bool isValue = false;

                string fieldStr = "";

                while (stream.Position != stream.Length)
                {
                    byte flag = reader.ReadByte();

                    if (!isValue)
                    {
                        byte[] key = reader.ReadBytes(flag - 0xA0);

                        fieldStr = $"  Key: {Encoding.ASCII.GetString(key)}";

                        isValue = true;
                    }
                    else
                    {
                        if (flag > 0xD0) // Int value.
                        {
                            if (flag - 0xD0 == 1)
                            {
                                fieldStr += $", Value: {EndianSwap.Swap16(reader.ReadUInt16())}";
                            }
                            else if (flag - 0xD0 == 2)
                            {
                                fieldStr += $", Value: {EndianSwap.Swap32(reader.ReadInt32())}";
                            }
                            else if (flag - 0xD0 == 4)
                            {
                                fieldStr += $", Value: {reader.ReadInt64()}";
                            }
                            else
                            {
                                // Unknown.
                                break;
                            }
                        }
                        else if (flag > 0xA0 && flag < 0xD0) // String value, max size = 0x20 bytes ?
                        {
                            byte[] valueBuffer = reader.ReadBytes(flag - 0xA0);
                            string value       = Encoding.ASCII.GetString(valueBuffer);

                            // TODO: Find why there is no alpha-numeric value sometimes.
                            fieldStr += $", Value: {Regex.Replace(value, "[^A-Za-z0-9 -~]", "")}";
                        }
                        else // Byte value.
                        {
                            fieldStr += $", Value: {flag}";
                        }

                        sb.AppendLine(fieldStr);

                        isValue = false;
                    }
                }
            }

            return sb.ToString();
        }
    }
}