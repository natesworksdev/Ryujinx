using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer
{
    class MemoryPoolParameterTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x20, Is.EqualTo(Unsafe.SizeOf<MemoryPoolInParameter>()));
            Assert.That(0x10, Is.EqualTo(Unsafe.SizeOf<MemoryPoolOutStatus>()));
        }
    }
}
