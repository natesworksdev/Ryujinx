using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter.Effect;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Parameter.Effect
{
    class Reverb3dParameterTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x49, Is.EqualTo(Unsafe.SizeOf<Reverb3dParameter>()));
        }
    }
}
