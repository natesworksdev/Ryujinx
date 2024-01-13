using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer
{
    class EffectInfoParameterTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0xC0, Is.EqualTo(Unsafe.SizeOf<EffectInParameterVersion1>()));
            Assert.That(0xC0, Is.EqualTo(Unsafe.SizeOf<EffectInParameterVersion2>()));
        }
    }
}
