using System;

namespace Ryujinx.Graphics.Gpu.Shader
{
    ref struct ShaderPack
    {
        private ReadOnlySpan<byte> _code0;
        private ReadOnlySpan<byte> _code1;
        private ReadOnlySpan<byte> _code2;
        private ReadOnlySpan<byte> _code3;
        private ReadOnlySpan<byte> _code4;

        public int Count { get; private set; }

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
    }
}
