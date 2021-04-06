using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetProgramCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.SetProgram;
        private TableRef<ThreadedProgram> _program;

        public void Set(TableRef<ThreadedProgram> program)
        {
            _program = program;
        }

        public void Run(ThreadedRenderer threaded, IRenderer renderer)
        {
            ThreadedProgram program = _program.Get(threaded);

            threaded.Programs.WaitForProgram(program);

            renderer.Pipeline.SetProgram(program.Base);
        }
    }
}
