using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer
{
    class AudioRendererConfigurationTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x34, Is.EqualTo(Unsafe.SizeOf<AudioRendererConfiguration>()));
        }
    }
}
