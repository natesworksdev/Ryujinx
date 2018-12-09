using System;

namespace Ryujinx.Graphics.Gal
{
    public struct GalVertexAttribArray : IEquatable<GalVertexAttribArray>
    {
        public bool Enabled { get; private set; }
        public long VboKey  { get; private set; }
        public int  Stride  { get; private set; }
        public int  Divisor { get; private set; }

        public GalVertexAttribArray(long vboKey, int stride, int divisor)
        {
            Enabled = true;
            VboKey  = vboKey;
            Stride  = stride;
            Divisor = divisor;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is GalVertexAttribArray array))
            {
                return false;
            }

            return Equals(array);
        }

        public bool Equals(GalVertexAttribArray array)
        {
            return Enabled == array.Enabled &&
                   VboKey  == array.VboKey  &&
                   Stride  == array.Stride  &&
                   Divisor == array.Divisor;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Enabled, VboKey, Stride, Divisor);
        }
    }
}