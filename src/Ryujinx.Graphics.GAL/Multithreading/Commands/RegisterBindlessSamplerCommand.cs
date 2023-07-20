using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct RegisterBindlessSamplerCommand : IGALCommand, IGALCommand<RegisterBindlessSamplerCommand>
    {
        public CommandType CommandType => CommandType.RegisterBindlessSampler;
        private int _samplerId;
        private TableRef<ISampler> _sampler;

        public void Set(int samplerId, TableRef<ISampler> sampler)
        {
            _samplerId = samplerId;
            _sampler = sampler;
        }

        public static void Run(ref RegisterBindlessSamplerCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            renderer.Pipeline.RegisterBindlessSampler(command._samplerId, command._sampler.GetAs<ThreadedSampler>(threaded)?.Base);
        }
    }
}
