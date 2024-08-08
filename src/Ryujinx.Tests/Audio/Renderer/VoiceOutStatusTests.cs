using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer
{
    class VoiceOutStatusTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x10, Is.EqualTo(Unsafe.SizeOf<VoiceOutStatus>()));
        }
    }
}
