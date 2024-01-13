using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter.Sink;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Parameter.Sink
{
    class CircularBufferParameterTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x24, Is.EqualTo(Unsafe.SizeOf<CircularBufferParameter>()));
        }
    }
}
