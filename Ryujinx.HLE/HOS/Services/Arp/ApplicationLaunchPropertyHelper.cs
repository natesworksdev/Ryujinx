using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.Utilities;
using System;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Arp
{
    static class ApplicationLaunchPropertyHelper
    {
        public static ApplicationLaunchProperty GetDefault()
        {
            return new ApplicationLaunchProperty
            {
                TitleId             = 0x00,
                Version             = 0x00,
                BaseGameStorageId   = (byte)StorageId.NandSystem,
                UpdateGameStorageId = (byte)StorageId.None
            };
        }

        public static ApplicationLaunchProperty GetByPid(ServiceCtx context)
        {
            // TODO: Handle ApplicationLaunchProperty as array when pid will be supported and return the right item.
            //       For now we can hardcode values, and fix it after GetApplicationLaunchProperty is implemented.

            return new ApplicationLaunchProperty
            {
                TitleId             = BitConverter.ToInt64(StringUtils.HexToBytes(context.Device.System.TitleID), 0),
                Version             = 0x00,
                BaseGameStorageId   = (byte)StorageId.NandSystem,
                UpdateGameStorageId = (byte)StorageId.None
            };
        }
    }
}