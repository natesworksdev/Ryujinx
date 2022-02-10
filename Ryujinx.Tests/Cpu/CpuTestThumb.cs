using NUnit.Framework;
using System;

namespace Ryujinx.Tests.Cpu
{
    [Category("Thumb")]
    public sealed class CpuTestThumb : CpuTest32
    {
        private const int RndCnt = 2;
    }
}
