using NUnit.Framework;
using Ryujinx.Audio.Renderer.Common;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Common
{
    class UpdateDataHeaderTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x40, Is.EqualTo(Unsafe.SizeOf<UpdateDataHeader>()));
        }
    }
}
