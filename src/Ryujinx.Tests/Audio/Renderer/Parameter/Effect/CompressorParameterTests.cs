using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter.Effect;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Parameter.Effect
{
    class CompressorParameterTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x38, Is.EqualTo(Unsafe.SizeOf<CompressorParameter>()));
        }
    }
}
