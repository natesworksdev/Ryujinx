using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;
using Ryujinx.Graphics.Shader;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetImageArrayCommand : IGALCommand, IGALCommand<SetImageArrayCommand>
    {
        public readonly CommandType CommandType => CommandType.SetImageArray;
        private ShaderStage _stage;
        private bool _hasSetIndex;
        private int _setIndex;
        private int _binding;
        private TableRef<IImageArray> _array;

        public void Set(ShaderStage stage, int binding, TableRef<IImageArray> array)
        {
            _stage = stage;
            _hasSetIndex = false;
            _setIndex = 0;
            _binding = binding;
            _array = array;
        }

        public void Set(ShaderStage stage, int setIndex, int binding, TableRef<IImageArray> array)
        {
            _stage = stage;
            _hasSetIndex = true;
            _setIndex = setIndex;
            _binding = binding;
            _array = array;
        }

        public static void Run(ref SetImageArrayCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            if (command._hasSetIndex)
            {
                renderer.Pipeline.SetImageArray(command._stage, command._setIndex, command._binding, command._array.GetAs<ThreadedImageArray>(threaded)?.Base);
            }
            else
            {
                renderer.Pipeline.SetImageArray(command._stage, command._binding, command._array.GetAs<ThreadedImageArray>(threaded)?.Base);
            }
        }
    }
}
