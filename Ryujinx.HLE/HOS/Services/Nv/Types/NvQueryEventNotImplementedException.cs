using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices;
using System;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Nv.Types
{
    class NvQueryEventlNotImplementedException : Exception
    {
        public ServiceCtx   Context    { get; }
        public NvFileDevice FileDevice { get; }
        public uint         EventId    { get; }

        public NvQueryEventlNotImplementedException(ServiceCtx context, NvFileDevice fileDevice, uint eventId)
            : this(context, fileDevice, eventId, "The ioctl is not implemented.")
        { }

        public NvQueryEventlNotImplementedException(ServiceCtx context, NvFileDevice fileDevice, uint eventId, string message)
            : base(message)
        {
            Context    = context;
            FileDevice = fileDevice;
            EventId    = eventId;
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

            sb.AppendLine($"Device File: {FileDevice.GetType().Name}");
            sb.AppendLine();

            sb.AppendLine($"Event ID: (0x{EventId:x8})");

            sb.AppendLine("Guest Stack Trace:");
            sb.AppendLine(Context.Thread.GetGuestStackTrace());

            return sb.ToString();
        }
    }
}
