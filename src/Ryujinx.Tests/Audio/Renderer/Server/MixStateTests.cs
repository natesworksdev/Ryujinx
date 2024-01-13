using NUnit.Framework;
using Ryujinx.Audio.Renderer.Server.Mix;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Server
{
    class MixStateTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x940, Is.EqualTo(Unsafe.SizeOf<MixState>()));
        }
    }
}
