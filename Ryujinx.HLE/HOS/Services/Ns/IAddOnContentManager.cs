using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Ns
{
    class IAddOnContentManager : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _commands;

        public IAddOnContentManager()
        {
            _commands = new Dictionary<int, ServiceProcessRequest>
            {
                { 2, CountAddOnContent     },
                { 3, ListAddOnContent      },
                { 5, GetAddOnContentBaseId },
                { 7, PrepareAddOnContent   }
            };
        }

        public static long CountAddOnContent(ServiceCtx context)
        {
            context.ResponseData.Write(context.Device.System.ContentManager.GetCurrentApplicationAocDataCount());

            return 0;
        }

        public static long ListAddOnContent(ServiceCtx context)
        {
            int[] aocIndices = context.Device.System.ContentManager.GetCurrentApplicationAocDataIndices();

            for (int index = 0; index < aocIndices.Length; index++)
            {
                long address = context.Request.ReceiveBuff[0].Position + index * 4;

                context.Memory.WriteInt32(address, aocIndices[index]);
            }

            context.ResponseData.Write(aocIndices.Length);

            return 0;
        }

        public static long GetAddOnContentBaseId(ServiceCtx context)
        {
            context.ResponseData.Write(context.Process.TitleId + 0x1000);

            return 0;
        }

        public static long PrepareAddOnContent(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceNs);

            return 0;
        }
    }
}