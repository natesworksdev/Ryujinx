using NUnit.Framework;
using Ryujinx.Audio.Renderer.Server.Voice;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Server
{
    class VoiceChannelResourceTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0xD0, Is.EqualTo(Unsafe.SizeOf<VoiceChannelResource>()));
        }
    }
}
