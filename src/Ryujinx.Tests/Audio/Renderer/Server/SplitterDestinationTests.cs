using NUnit.Framework;
using Ryujinx.Audio.Renderer.Server.Splitter;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Server
{
    class SplitterDestinationTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0xE0, Is.EqualTo(Unsafe.SizeOf<SplitterDestinationVersion1>()));
            Assert.That(0x110, Is.EqualTo(Unsafe.SizeOf<SplitterDestinationVersion2>()));
        }
    }
}
