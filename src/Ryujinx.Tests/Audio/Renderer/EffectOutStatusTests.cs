using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer
{
    class EffectOutStatusTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x10, Is.EqualTo(Unsafe.SizeOf<EffectOutStatusVersion1>()));
            Assert.That(0x90, Is.EqualTo(Unsafe.SizeOf<EffectOutStatusVersion2>()));
        }
    }
}
