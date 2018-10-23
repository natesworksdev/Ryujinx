using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Vi
{
    class SystemDisplayService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public SystemDisplayService()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 2205, SetLayerZ },
                { 2207, SetLayerVisibility },
                { 3200, GetDisplayMode }
            };
        }

        public static long SetLayerZ(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceVi, "Stubbed.");

            return 0;
        }

        public static long SetLayerVisibility(ServiceCtx context)
        {
            Logger.PrintStub(LogClass.ServiceVi, "Stubbed.");

            return 0;
        }

        public static long GetDisplayMode(ServiceCtx context)
        {
            //TODO: De-hardcode resolution.
            context.ResponseData.Write(1280);
            context.ResponseData.Write(720);
            context.ResponseData.Write(60.0f);
            context.ResponseData.Write(0);

            return 0;
        }
    }
}