using NUnit.Framework;
using Ryujinx.Audio.Renderer.Common;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer
{
    class BehaviourParameterTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x10, Is.EqualTo(Unsafe.SizeOf<BehaviourParameter>()));
            Assert.That(0x10, Is.EqualTo(Unsafe.SizeOf<BehaviourParameter.ErrorInfo>()));
        }
    }
}
