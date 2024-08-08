using NUnit.Framework;
using Ryujinx.Audio.Renderer.Server.Splitter;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Server
{
    class SplitterStateTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x20, Is.EqualTo(Unsafe.SizeOf<SplitterState>()));
        }
    }
}
