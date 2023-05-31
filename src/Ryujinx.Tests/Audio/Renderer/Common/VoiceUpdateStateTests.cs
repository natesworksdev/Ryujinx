using NUnit.Framework;
using Ryujinx.Audio.Renderer.Common;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Common
{
    sealed class VoiceUpdateStateTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.LessOrEqual(Unsafe.SizeOf<VoiceUpdateState>(), 0x100);
        }
    }
}
