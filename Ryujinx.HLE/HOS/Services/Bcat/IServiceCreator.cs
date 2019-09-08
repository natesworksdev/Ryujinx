using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS.Services.Arp;
using Ryujinx.HLE.Utilities;
using System;

namespace Ryujinx.HLE.HOS.Services.Bcat
{
    [Service("bcat:a")]
    [Service("bcat:m")]
    [Service("bcat:u")]
    [Service("bcat:s")]
    class IServiceCreator : IpcService
    {
        public IServiceCreator(ServiceCtx context) { }

        [Command(0)]
        // CreateBcatService(u64, pid) -> object<nn::bcat::detail::ipc::IBcatService>
        public ResultCode CreateBcatService(ServiceCtx context)
        {
            // TODO: Call arp:r GetApplicationLaunchProperty with the pid to get the TitleId.
            //       Add an instance of nn::bcat::detail::service::core::PassphraseManager.
            //       Add an instance of nn::bcat::detail::service::ServiceMemoryManager.
            //       Add an instance of nn::bcat::detail::service::core::TaskManager who load "bcat-sys:/" system save data and open "dc/task.bin". 
            //       If the file don't exist, create a new one (size of 0x800) and write 2 empty struct with a size of 0x400.

            ApplicationLaunchProperty applicationLaunchProperty = new ApplicationLaunchProperty
            {
                TitleId             = BitConverter.ToInt64(StringUtils.HexToBytes(context.Device.System.TitleID), 0),
                Version             = 0x00,
                BaseGameStorageId   = (byte)StorageId.NandSystem,
                UpdateGameStorageId = (byte)StorageId.None
            };

            MakeObject(context, new IBcatService(applicationLaunchProperty));

            // NOTE: If the IBcatService is null this error is returned, Doesn't occur in our case. 
            //       return ResultCode.NullObject;

            return ResultCode.Success;
        }

        [Command(1)]
        // CreateDeliveryCacheStorageService(u64, pid) -> object<nn::bcat::detail::ipc::IDeliveryCacheStorageService>
        public ResultCode CreateDeliveryCacheStorageService(ServiceCtx context)
        {
            // TODO: Call arp:r GetApplicationLaunchProperty with the pid to get the TitleId.
            //       Add an instance of nn::bcat::detail::service::core::ApplicationStorageManager who load "bcat-dc-X:/" system save data,
            //       return ResultCode.NullSaveData if failed.
            //       Where X depend of the ApplicationLaunchProperty stored in an array (range 0-3).
            //       Add an instance of nn::bcat::detail::service::ServiceMemoryManager.

            ApplicationLaunchProperty applicationLaunchProperty = new ApplicationLaunchProperty
            {
                TitleId             = BitConverter.ToInt64(StringUtils.HexToBytes(context.Device.System.TitleID), 0),
                Version             = 0x00,
                BaseGameStorageId   = (byte)StorageId.NandSystem,
                UpdateGameStorageId = (byte)StorageId.None
            };

            MakeObject(context, new IDeliveryCacheStorageService(context, applicationLaunchProperty));

            // NOTE: If the IDeliveryCacheStorageService is null this error is returned, Doesn't occur in our case. 
            //       return ResultCode.NullObject;

            return ResultCode.Success;
        }
    }
}