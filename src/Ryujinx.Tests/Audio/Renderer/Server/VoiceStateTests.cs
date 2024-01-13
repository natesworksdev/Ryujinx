using NUnit.Framework;
using Ryujinx.Audio.Renderer.Server.Voice;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Server
{
    class VoiceStateTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(Unsafe.SizeOf<VoiceState>(), Is.LessThanOrEqualTo(0x220));
        }
    }
}
