using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources.Programs;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands.Renderer
{
    struct CreateProgramCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.CreateProgram;
        private TableRef<IProgramRequest> _request;

        public void Set(TableRef<IProgramRequest> request)
        {
            _request = request;
        }

        public void Run(ThreadedRenderer threaded, IRenderer renderer)
        {
            IProgramRequest request = _request.Get(threaded);

            if (request.Threaded.Base == null)
            {
                request.Threaded.Base = request.Create(renderer);
            }

            threaded.Programs.ProcessQueue();
        }
    }
}
