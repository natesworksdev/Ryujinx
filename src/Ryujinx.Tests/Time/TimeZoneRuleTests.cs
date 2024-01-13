using NUnit.Framework;
using Ryujinx.HLE.HOS.Services.Time.TimeZone;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Time
{
    internal class TimeZoneRuleTests
    {
        class EffectInfoParameterTests
        {
            [Test]
            public void EnsureTypeSize()
            {
                Assert.That(0x4000, Is.EqualTo(Unsafe.SizeOf<TimeZoneRule>()));
            }
        }
    }
}
