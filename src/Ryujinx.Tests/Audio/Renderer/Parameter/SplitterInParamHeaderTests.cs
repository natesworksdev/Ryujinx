using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Parameter
{
    class SplitterInParamHeaderTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x20, Is.EqualTo(Unsafe.SizeOf<SplitterInParameterHeader>()));
        }
    }
}
