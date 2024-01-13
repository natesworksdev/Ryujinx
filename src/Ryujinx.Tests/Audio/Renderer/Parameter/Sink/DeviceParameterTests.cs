using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter.Sink;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Parameter.Sink
{
    class DeviceParameterTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x11C, Is.EqualTo(Unsafe.SizeOf<DeviceParameter>()));
        }
    }
}
