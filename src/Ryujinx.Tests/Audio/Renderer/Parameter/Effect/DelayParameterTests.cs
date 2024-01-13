using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter.Effect;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Parameter.Effect
{
    class DelayParameterTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x35, Is.EqualTo(Unsafe.SizeOf<DelayParameter>()));
        }
    }
}
