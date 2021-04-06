using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands.Program
{
    struct ProgramCheckLinkCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.ProgramCheckLink;
        private TableRef<ThreadedProgram> _program;
        private bool _blocking;
        private TableRef<ResultBox<ProgramLinkStatus>> _result;

        public void Set(TableRef<ThreadedProgram> program, bool blocking, TableRef<ResultBox<ProgramLinkStatus>> result)
        {
            _program = program;
            _blocking = blocking;
            _result = result;
        }

        public void Run(ThreadedRenderer threaded, IRenderer renderer)
        {
            ProgramLinkStatus result = _program.Get(threaded).Base.CheckProgramLink(_blocking);

            _result.Get(threaded).Result = result;
        }
    }
}
