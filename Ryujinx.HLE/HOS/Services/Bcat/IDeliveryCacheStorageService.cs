using Ryujinx.HLE.HOS.Services.Arp;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Bcat
{
    class IDeliveryCacheStorageService : IpcService
    {
        private const int DeliveryCacheDirectoriesLimit    = 100;
        private const int DeliveryCacheDirectoryNameLength = 32;

        private List<string> _deliveryCacheDirectories = new List<string>();

        public IDeliveryCacheStorageService(ServiceCtx context, ApplicationLaunchProperty applicationLaunchProperty)
        {
            // TODO: Read directories.meta file from the save data (loaded in IServiceCreator) in _deliveryCacheDirectories.
        }

        [Command(10)]
        // EnumerateDeliveryCacheDirectory() -> (u32, buffer<nn::bcat::DirectoryName, 6>)
        public ResultCode EnumerateDeliveryCacheDirectory(ServiceCtx context)
        {
            long outputPosition = context.Request.ReceiveBuff[0].Position;
            long outputSize     = context.Request.ReceiveBuff[0].Size;

            for (int index = 0; index < _deliveryCacheDirectories.Count; index++)
            {
                if (index == DeliveryCacheDirectoriesLimit - 1)
                {
                    break;
                }

                byte[] directoryNameBuffer = Encoding.ASCII.GetBytes(_deliveryCacheDirectories[index]);

                Array.Resize(ref directoryNameBuffer, DeliveryCacheDirectoryNameLength);

                directoryNameBuffer[DeliveryCacheDirectoryNameLength - 1] = 0x00;
                
                context.Memory.WriteBytes(outputPosition + index * DeliveryCacheDirectoryNameLength, directoryNameBuffer);
            }

            context.ResponseData.Write(_deliveryCacheDirectories.Count);

            return ResultCode.Success;
        }
    }
}