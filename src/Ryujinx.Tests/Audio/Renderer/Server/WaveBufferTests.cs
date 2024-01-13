using NUnit.Framework;
using Ryujinx.Audio.Renderer.Server.Voice;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Server
{
    class WaveBufferTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x58, Is.EqualTo(Unsafe.SizeOf<WaveBuffer>()));
        }
    }
}
