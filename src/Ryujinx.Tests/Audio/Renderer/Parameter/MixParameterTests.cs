using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Parameter
{
    class MixParameterTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x930, Is.EqualTo(Unsafe.SizeOf<MixParameter>()));
        }
    }
}
