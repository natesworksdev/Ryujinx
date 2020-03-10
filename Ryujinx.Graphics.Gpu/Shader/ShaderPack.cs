using System;

namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// Spans of shader code for each active shader stage.
    /// </summary>
    ref struct ShaderPack
    {
        private ReadOnlySpan<byte> _code0;
        private ReadOnlySpan<byte> _code1;
        private ReadOnlySpan<byte> _code2;
        private ReadOnlySpan<byte> _code3;
        private ReadOnlySpan<byte> _code4;

        /// <summary>
        /// Number of active shader stages.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        /// Gets code for a given shader stage.
        /// </summary>
        /// <param name="index">Index of the shader code</param>
        /// <returns>Guest shader code</returns>
        public ReadOnlySpan<byte> this[int index]
        {
            get
            {
                if ((uint)index > 4)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                if (index == 0)
                {
                    return _code0;
                }
                else if (index == 1)
                {
                    return _code1;
                }
                else if (index == 2)
                {
                    return _code2;
                }
                else if (index == 3)
                {
                    return _code3;
                }
                else /* if (index == 4) */
                {
                    return _code4;
                }
            }
        }

        /// <summary>
        /// Adds shader code to the pack.
        /// This can be used to add code for a shader stage to the pack.
        /// </summary>
        /// <param name="code">Code to be added</param>
        public void Add(ReadOnlySpan<byte> code)
        {
            if (Count >= 5)
            {
                throw new InvalidOperationException("Already full.");
            }

            int index = Count++;

            if (index == 0)
            {
                _code0 = code;
            }
            else if (index == 1)
            {
                _code1 = code;
            }
            else if (index == 2)
            {
                _code2 = code;
            }
            else if (index == 3)
            {
                _code3 = code;
            }
            else /* if (index == 4) */
            {
                _code4 = code;
            }
        }

        /// <summary>
        /// Copies the internal code spans to a new array.
        /// </summary>
        /// <returns>Arrays of code for all the stages</returns>
        public byte[][] ToArray()
        {
            byte[][] output = new byte[Count][];

            int index = 0;

            if (Count >= 1)
            {
                output[index++] = _code0.ToArray();
            }

            if (Count >= 2)
            {
                output[index++] = _code1.ToArray();
            }

            if (Count >= 3)
            {
                output[index++] = _code2.ToArray();
            }

            if (Count >= 4)
            {
                output[index++] = _code3.ToArray();
            }

            if (Count >= 5)
            {
                output[index++] = _code4.ToArray();
            }

            return output;
        }
    }
}
