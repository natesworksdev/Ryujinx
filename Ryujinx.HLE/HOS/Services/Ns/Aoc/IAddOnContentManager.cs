using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Ns.Aoc
{
    [Service("aoc:u")]
    class IAddOnContentManager : IpcService
    {
        private readonly KEvent _addOnContentListChangedEvent;

        private ulong _addOnContentBaseId;

        public IAddOnContentManager(ServiceCtx context)
        {
            _addOnContentListChangedEvent = new KEvent(context.Device.System.KernelContext);
        }

        [CommandHipc(0)] // 1.0.0-6.2.0
        [CommandHipc(2)]
        // CountAddOnContent(pid) -> u32
        public ResultCode CountAddOnContent(ServiceCtx context)
        {
            // NOTE: Call 0 uses the TitleId instead of the Pid.
            long pid = context.Process.Pid;

            // NOTE: Service call sys:set GetQuestFlag and store it internally.
            //       If QuestFlag is true, counts some extra titles.

            ResultCode resultCode = GetAddOnContentBaseIdImpl(context, pid);

            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            bool runtimeAddOnContentInstall = context.Device.Application.ControlData.Value.RuntimeAddOnContentInstall != 0;
            if (runtimeAddOnContentInstall)
            {
                // TODO: This should use _addOnContentBaseId;
                uint aocCount = (uint)context.Device.System.ContentManager.GetAocCount();

                context.ResponseData.Write(aocCount);
            }

            return ResultCode.Success;
        }

        [CommandHipc(1)] // 1.0.0-6.2.0
        [CommandHipc(3)]
        // ListAddOnContent(u32 start_index, u32 buffer_size, pid) -> (u32 count, buffer<u32>)
        public ResultCode ListAddOnContent(ServiceCtx context)
        {
            // NOTE: Call 1 uses the TitleId instead of the Pid.
            long pid = context.Process.Pid;

            // NOTE: Service call sys:set GetQuestFlag and store it internally.
            //       If QuestFlag is true, counts some extra titles.

            uint  startIndex     = context.RequestData.ReadUInt32();
            uint  bufferSize     = context.RequestData.ReadUInt32();
            ulong bufferPosition = context.Request.ReceiveBuff[0].Position;

            // TODO: This should use _addOnContentBaseId;
            uint aocCount = (uint)context.Device.System.ContentManager.GetAocCount();

            if (aocCount - startIndex > bufferSize)
            {
                return ResultCode.InvalidBufferSize;
            }

            if (aocCount <= startIndex)
            {
                context.ResponseData.Write(0);

                return ResultCode.Success;
            }

            IList<ulong> aocTitleIds = context.Device.System.ContentManager.GetAocTitleIds();

            GetAddOnContentBaseIdImpl(context, pid);

            for (int i = 0; i < aocCount; ++i)
            {
                context.Memory.Write(bufferPosition + (ulong)i * 4, (int)(aocTitleIds[i + (int)startIndex] - _addOnContentBaseId));
            }

            context.ResponseData.Write(aocCount);

            return ResultCode.Success;
        }

        [CommandHipc(4)] // 1.0.0-6.2.0
        [CommandHipc(5)]
        // GetAddOnContentBaseId(pid) -> u64
        public ResultCode GetAddonContentBaseId(ServiceCtx context)
        {
            // NOTE: Call 4 uses the TitleId instead of the Pid.
            long pid = context.Process.Pid;

            ResultCode resultCode = GetAddOnContentBaseIdImpl(context, pid);

            context.ResponseData.Write(_addOnContentBaseId);

            return resultCode;
        }

        [CommandHipc(6)] // 1.0.0-6.2.0
        [CommandHipc(7)]
        // PrepareAddOnContent(u32 index, pid)
        public ResultCode PrepareAddOnContent(ServiceCtx context)
        {
            // NOTE: Call 6 use the TitleId instead of the Pid.
            long pid   = context.Process.Pid;
            uint index = context.RequestData.ReadUInt32();

            ResultCode resultCode = GetAddOnContentBaseIdImpl(context, pid);

            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            // TODO: Service calls ns:am RegisterContentsExternalKey?, GetOwnedApplicationContentMetaStatus? etc...
            //       Ideally, this should probably initialize the AocData values for the specified index

            Logger.Stub?.PrintStub(LogClass.ServiceNs, new { index });

            return ResultCode.Success;
        }

        [CommandHipc(8)] // 4.0.0+
        // GetAddOnContentListChangedEvent() -> handle<copy>
        public ResultCode GetAddOnContentListChangedEvent(ServiceCtx context)
        {
            return GetAddOnContentListChangedEventImpl(context);
        }

        [CommandHipc(9)] // 10.0.0+
        // GetAddOnContentLostErrorCode() -> u64
        public ResultCode GetAddOnContentLostErrorCode(ServiceCtx context)
        {
            // NOTE: 0x7D0A4 -> 2164-1000
            ulong lostErrorCode = (0x7D0A4 & 0x1FF | (((0x7D0A4 >> 9) & 0x1FFF) << 32)) + 2000;

            context.ResponseData.Write(lostErrorCode);

            return ResultCode.Success;
        }

        [CommandHipc(10)] // 11.0.0+
        // GetAddOnContentListChangedEventWithProcessId(pid) -> handle<copy>
        public ResultCode GetAddOnContentListChangedEventWithProcessId(ServiceCtx context)
        {
            long pid = context.Process.Pid;

            // TODO: Found where stored value is used.
            ResultCode resultCode = GetAddOnContentBaseIdImpl(context, pid);
            
            if (resultCode != ResultCode.Success)
            {
                return resultCode;
            }

            return GetAddOnContentListChangedEventImpl(context);
        }

        [CommandHipc(100)] // 7.0.0+
        // CreateEcPurchasedEventManager() -> object<nn::ec::IPurchaseEventManager>
        public ResultCode CreateEcPurchasedEventManager(ServiceCtx context)
        {
            MakeObject(context, new IPurchaseEventManager(context.Device.System));

            return ResultCode.Success;
        }

        [CommandHipc(101)] // 9.0.0+
        // CreatePermanentEcPurchasedEventManager() -> object<nn::ec::IPurchaseEventManager>
        public ResultCode CreatePermanentEcPurchasedEventManager(ServiceCtx context)
        {
            // NOTE: Service call arp:r to get the TitleId, do some extra checks and pass it to returned interface.

            MakeObject(context, new IPurchaseEventManager(context.Device.System));

            return ResultCode.Success;
        }

        [CommandHipc(110)] // 12.0.0+
        // CreateContentsServiceManager() -> object<nn::ec::IContentsServiceManager>
        public ResultCode CreateContentsServiceManager(ServiceCtx context)
        {
            MakeObject(context, new IContentsServiceManager());

            return ResultCode.Success;
        }

        private ResultCode GetAddOnContentBaseIdImpl(ServiceCtx context, long pid)
        {
            // NOTE: Service calls arp:r GetApplicationControlProperty to get AddOnContentBaseId using pid,
            //       If the call fails, calls arp:r GetApplicationLaunchProperty to get App TitleId using pid,
            //       If the call fails, it returns ResultCode.InvalidPid.

            _addOnContentBaseId = context.Device.Application.ControlData.Value.AddOnContentBaseId;

            if (_addOnContentBaseId == 0)
            {
                _addOnContentBaseId = context.Device.Application.TitleId + 0x1000;
            }

            return ResultCode.Success;
        }

        private ResultCode GetAddOnContentListChangedEventImpl(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(_addOnContentListChangedEvent.ReadableEvent, out int addOnContentListChangedEventHandle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(addOnContentListChangedEventHandle);

            return ResultCode.Success;
        }
    }
}