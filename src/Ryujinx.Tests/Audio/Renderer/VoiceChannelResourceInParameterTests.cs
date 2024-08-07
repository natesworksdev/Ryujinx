using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer
{
    class VoiceChannelResourceInParameterTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x70, Is.EqualTo(Unsafe.SizeOf<VoiceChannelResourceInParameter>()));
        }
    }
}
