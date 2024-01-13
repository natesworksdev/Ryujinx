using NUnit.Framework;
using Ryujinx.Audio.Renderer.Parameter.Effect;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Parameter.Effect
{
    class LimiterStatisticsTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x30, Is.EqualTo(Unsafe.SizeOf<LimiterStatistics>()));
        }
    }
}
