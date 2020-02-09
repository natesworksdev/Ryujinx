using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Shader;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Shader meta data.
    /// </summary>
    class ShaderMeta
    {
        private const int ShaderStages = 6;

        private const int UbStageShift = 5;
        private const int SbStageShift = 4;
        private const int TexStageShift = 5;
        private const int ImgStageShift = 3;

        private const int UbsPerStage = 1 << UbStageShift;
        private const int SbsPerStage = 1 << SbStageShift;
        private const int TexsPerStage = 1 << TexStageShift;
        private const int ImgsPerStage = 1 << ImgStageShift;

        private readonly int[] _ubBindingPoints;
        private readonly int[] _sbBindingPoints;
        private readonly int[] _textureUnits;
        private readonly int[] _imageUnits;

        /// <summary>
        /// Shader program information.
        /// </summary>
        public ShaderProgramInfo[] Info { get; }

        /// <summary>
        /// Creates a new instace of the shader meta data.
        /// </summary>
        /// <param name="hostProgram">Host shader program</param>
        /// <param name="infos">Shader program information, per stage</param>
        public ShaderMeta(IProgram hostProgram, params ShaderProgramInfo[] infos)
        {
            _ubBindingPoints = new int[UbsPerStage * ShaderStages];
            _sbBindingPoints = new int[SbsPerStage * ShaderStages];
            _textureUnits = new int[TexsPerStage * ShaderStages];
            _imageUnits = new int[ImgsPerStage * ShaderStages];

            for (int index = 0; index < _ubBindingPoints.Length; index++)
            {
                _ubBindingPoints[index] = -1;
            }

            for (int index = 0; index < _sbBindingPoints.Length; index++)
            {
                _sbBindingPoints[index] = -1;
            }

            for (int index = 0; index < _textureUnits.Length; index++)
            {
                _textureUnits[index] = -1;
            }

            for (int index = 0; index < _imageUnits.Length; index++)
            {
                _imageUnits[index] = -1;
            }

            int ubBindingPoint = 0;
            int sbBindingPoint = 0;
            int textureUnit = 0;
            int imageUnit = 0;

            Info = new ShaderProgramInfo[infos.Length];

            for (int index = 0; index < infos.Length; index++)
            {
                ShaderProgramInfo info = infos[index];

                if (info == null)
                {
                    continue;
                }

                foreach (BufferDescriptor descriptor in info.CBuffers)
                {
                    hostProgram.SetUniformBufferBindingPoint(descriptor.Name, ubBindingPoint);

                    int bpIndex = (int)info.Stage << UbStageShift | descriptor.Slot;

                    _ubBindingPoints[bpIndex] = ubBindingPoint;

                    ubBindingPoint++;
                }

                foreach (BufferDescriptor descriptor in info.SBuffers)
                {
                    hostProgram.SetStorageBufferBindingPoint(descriptor.Name, sbBindingPoint);

                    int bpIndex = (int)info.Stage << SbStageShift | descriptor.Slot;

                    _sbBindingPoints[bpIndex] = sbBindingPoint;

                    sbBindingPoint++;
                }

                int samplerIndex = 0;

                foreach (TextureDescriptor descriptor in info.Textures)
                {
                    hostProgram.SetImageUnit(descriptor.Name, textureUnit);

                    int uIndex = (int)info.Stage << TexStageShift | samplerIndex++;

                    _textureUnits[uIndex] = textureUnit;

                    textureUnit++;
                }

                int imageIndex = 0;

                foreach (TextureDescriptor descriptor in info.Images)
                {
                    hostProgram.SetImageUnit(descriptor.Name, imageUnit);

                    int uIndex = (int)info.Stage << ImgStageShift | imageIndex++;

                    _imageUnits[uIndex] = imageUnit;

                    imageUnit++;
                }

                Info[index] = info;
            }
        }

        /// <summary>
        /// Gets the uniform buffer binding point for a given shader stage and resource index.
        /// </summary>
        /// <param name="stage">Shader stage</param>
        /// <param name="index">Resource index</param>
        /// <returns>Host binding point</returns>
        public int GetUniformBufferBindingPoint(ShaderStage stage, int index)
        {
            return _ubBindingPoints[(int)stage << UbStageShift | index];
        }

        /// <summary>
        /// Gets the storage buffer binding point for a given shader stage and resource index.
        /// </summary>
        /// <param name="stage">Shader stage</param>
        /// <param name="index">Resource index</param>
        /// <returns>Host binding point</returns>
        public int GetStorageBufferBindingPoint(ShaderStage stage, int index)
        {
            return _sbBindingPoints[(int)stage << SbStageShift | index];
        }

        /// <summary>
        /// Gets the texture unit for a given shader stage and texture index.
        /// </summary>
        /// <param name="stage">Shader stage</param>
        /// <param name="index">Texture index</param>
        /// <returns>Host unit</returns>
        public int GetTextureUnit(ShaderStage stage, int index)
        {
            return _textureUnits[(int)stage << TexStageShift | index];
        }

        /// <summary>
        /// Gets the image unit for a given shader stage and image index.
        /// </summary>
        /// <param name="stage">Shader stage</param>
        /// <param name="index">Image index</param>
        /// <returns>Host unit</returns>
        public int GetImageUnit(ShaderStage stage, int index)
        {
            return _imageUnits[(int)stage << ImgStageShift | index];
        }
    }
}
