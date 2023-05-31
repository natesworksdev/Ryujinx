using NUnit.Framework;
using Ryujinx.Audio.Renderer.Common;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Common
{
    sealed class UpdateDataHeaderTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.AreEqual(0x40, Unsafe.SizeOf<UpdateDataHeader>());
        }
    }
}
