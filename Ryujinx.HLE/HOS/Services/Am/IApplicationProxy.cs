using Ryujinx.HLE.HOS.Ipc;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS.Services.Am
{
    class ApplicationProxy : IpcService
    {
        private Dictionary<int, ServiceProcessRequest> _mCommands;

        public override IReadOnlyDictionary<int, ServiceProcessRequest> Commands => _mCommands;

        public ApplicationProxy()
        {
            _mCommands = new Dictionary<int, ServiceProcessRequest>()
            {
                { 0,    GetCommonStateGetter    },
                { 1,    GetSelfController       },
                { 2,    GetWindowController     },
                { 3,    GetAudioController      },
                { 4,    GetDisplayController    },
                { 11,   GetLibraryAppletCreator },
                { 20,   GetApplicationFunctions },
                { 1000, GetDebugFunctions       }
            };
        }

        public long GetCommonStateGetter(ServiceCtx context)
        {
            MakeObject(context, new CommonStateGetter(context.Device.System));

            return 0;
        }

        public long GetSelfController(ServiceCtx context)
        {
            MakeObject(context, new SelfController(context.Device.System));

            return 0;
        }

        public long GetWindowController(ServiceCtx context)
        {
            MakeObject(context, new WindowController());

            return 0;
        }

        public long GetAudioController(ServiceCtx context)
        {
            MakeObject(context, new AudioController());

            return 0;
        }

        public long GetDisplayController(ServiceCtx context)
        {
            MakeObject(context, new DisplayController());

            return 0;
        }

        public long GetLibraryAppletCreator(ServiceCtx context)
        {
            MakeObject(context, new LibraryAppletCreator());

            return 0;
        }

        public long GetApplicationFunctions(ServiceCtx context)
        {
            MakeObject(context, new ApplicationFunctions());

            return 0;
        }

        public long GetDebugFunctions(ServiceCtx context)
        {
            MakeObject(context, new DebugFunctions());

            return 0;
        }
    }
}