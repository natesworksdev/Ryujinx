using System;

namespace Ryujinx.Common.Cache
{
    public interface IDataAccessor
    {
        ReadOnlySpan<byte> GetSpan(int offset, int length);
    }
}
