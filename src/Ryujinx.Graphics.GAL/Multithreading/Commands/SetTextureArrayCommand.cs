using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;
using Ryujinx.Graphics.Shader;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands
{
    struct SetTextureArrayCommand : IGALCommand, IGALCommand<SetTextureArrayCommand>
    {
        public readonly CommandType CommandType => CommandType.SetTextureArray;
        private ShaderStage _stage;
        private bool _hasSetIndex;
        private int _setIndex;
        private int _binding;
        private TableRef<ITextureArray> _array;

        public void Set(ShaderStage stage, int binding, TableRef<ITextureArray> array)
        {
            _stage = stage;
            _hasSetIndex = false;
            _setIndex = 0;
            _binding = binding;
            _array = array;
        }

        public void Set(ShaderStage stage, int setIndex, int binding, TableRef<ITextureArray> array)
        {
            _stage = stage;
            _hasSetIndex = true;
            _setIndex = setIndex;
            _binding = binding;
            _array = array;
        }

        public static void Run(ref SetTextureArrayCommand command, ThreadedRenderer threaded, IRenderer renderer)
        {
            if (command._hasSetIndex)
            {
                renderer.Pipeline.SetTextureArray(command._stage, command._setIndex, command._binding, command._array.GetAs<ThreadedTextureArray>(threaded)?.Base);
            }
            else
            {
                renderer.Pipeline.SetTextureArray(command._stage, command._binding, command._array.GetAs<ThreadedTextureArray>(threaded)?.Base);
            }
        }
    }
}
