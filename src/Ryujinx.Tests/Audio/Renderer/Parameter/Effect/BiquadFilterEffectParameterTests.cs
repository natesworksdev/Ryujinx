using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter.Effect;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Parameter.Effect
{
    class BiquadFilterEffectParameterTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x18, Is.EqualTo(Unsafe.SizeOf<BiquadFilterEffectParameter>()));
        }
    }
}
