using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices;
using System;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Nv.Types
{
    class NvIoctlNotImplementedException : Exception
    {
        public ServiceCtx   Context    { get; }
        public NvFileDevice FileDevice { get; }
        public NvIoctl      Command    { get; }

        public NvIoctlNotImplementedException(ServiceCtx context, NvFileDevice fileDevice, NvIoctl command)
            : this(context, fileDevice, command, "The ioctl is not implemented.")
        { }

        public NvIoctlNotImplementedException(ServiceCtx context, NvFileDevice fileDevice, NvIoctl command, string message)
            : base(message)
        {
            Context    = context;
            FileDevice = fileDevice;
            Command    = command;
        }

        public override string Message
        {
            get
            {
                return base.Message +
                        Environment.NewLine +
                        Environment.NewLine +
                        BuildMessage();
            }
        }

        private string BuildMessage()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Ioctl (0x{Command.RawValue:x8})");
            sb.AppendLine($"\tNumber: 0x{Command.GetNumberValue():x8}");
            sb.AppendLine($"\tType: 0x{Command.GetTypeValue():x8}");
            sb.AppendLine($"\tSize: 0x{Command.GetSizeValue():x8}");
            sb.AppendLine($"\tDirection: {Command.GetDirectionValue()}");

            sb.AppendLine("Guest Stack Trace:");
            sb.AppendLine(Context.Thread.GetGuestStackTrace());

            return sb.ToString();
        }
    }
}
