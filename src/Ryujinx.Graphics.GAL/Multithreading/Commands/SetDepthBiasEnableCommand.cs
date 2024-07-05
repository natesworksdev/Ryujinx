namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetDepthBiasEnableCommand : IGALCommand, IGALCommand<SetDepthBiasEnableCommand>
    {
        public readonly CommandType CommandType => CommandType.SetDepthBias;
        private PolygonModeMask _enables;


        public void Set(PolygonModeMask enables)
        {
            _enables = enables;
        }

        public static void Run(ref SetDepthBiasEnableCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.SetDepthBiasEnable(command._enables);
        }
    }
}
