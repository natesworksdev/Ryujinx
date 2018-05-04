using Ryujinx.Core.OsHle.Ipc;
using System.Collections.Generic;
using Ryujinx.Core.Logging;

namespace Ryujinx.Core.OsHle.Services.Vi
{
    class ISystemDisplayService : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> m_Commands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => m_Commands;

        public ISystemDisplayService()
        {
            m_Commands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 2205, SetLayerZ },
                { 2207, SetLayerVisibility },
                { 3200, GetDisplayMode }
            };
        }

        public static long SetLayerZ(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceVi, "Stubbed.");
            return 0;
        }

        public static long SetLayerVisibility(ServiceCtx Context)
        {
            Context.Ns.Log.PrintStub(LogClass.ServiceVi, "Stubbed.");
            return 0;
        }

        public static long GetDisplayMode(ServiceCtx Context)
        {
            Context.ResponseData.Write(1280);
            Context.ResponseData.Write(720);
            Context.ResponseData.Write(16.0 / 9.0);
            Context.ResponseData.Write(0L);
            return 0;
        }
    }
}