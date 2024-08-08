using NUnit.Framework;
using Ryujinx.Audio.Renderer.Common;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Common
{
    class WaveBufferTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x30, Is.EqualTo(Unsafe.SizeOf<WaveBuffer>()));
        }
    }
}
