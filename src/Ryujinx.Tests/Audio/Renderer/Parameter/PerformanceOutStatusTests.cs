using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter.Performance;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Parameter
{
    class PerformanceOutStatusTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x10, Is.EqualTo(Unsafe.SizeOf<PerformanceOutStatus>()));
        }
    }
}
