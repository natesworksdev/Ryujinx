using NUnit.Framework;
using Ryujinx.Audio.Renderer.Server.MemoryPool;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Server
{
    class MemoryPoolStateTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(Unsafe.SizeOf<MemoryPoolState>(), Is.EqualTo(0x20));
        }

        [Test]
        public void TestContains()
        {
            MemoryPoolState memoryPool = MemoryPoolState.Create(MemoryPoolState.LocationType.Cpu);

            memoryPool.SetCpuAddress(0x1000000, 0x1000);

            memoryPool.DspAddress = 0x2000000;

            Assert.That(memoryPool.Contains(0x1000000, 0x10), Is.True);
            Assert.That(memoryPool.Contains(0x1000FE0, 0x10), Is.True);
            Assert.That(memoryPool.Contains(0x1000FFF, 0x1), Is.True);
            Assert.That(memoryPool.Contains(0x1000FFF, 0x2), Is.False);
            Assert.That(memoryPool.Contains(0x1001000, 0x10), Is.False);
            Assert.That(memoryPool.Contains(0x2000000, 0x10), Is.False);
        }

        [Test]
        public void TestTranslate()
        {
            MemoryPoolState memoryPool = MemoryPoolState.Create(MemoryPoolState.LocationType.Cpu);

            memoryPool.SetCpuAddress(0x1000000, 0x1000);

            memoryPool.DspAddress = 0x2000000;

            Assert.That(0x2000FE0, Is.EqualTo(memoryPool.Translate(0x1000FE0, 0x10)));
            Assert.That(0x2000FFF, Is.EqualTo(memoryPool.Translate(0x1000FFF, 0x1)));
            Assert.That(0x0, Is.EqualTo(memoryPool.Translate(0x1000FFF, 0x2)));
            Assert.That(0x0, Is.EqualTo(memoryPool.Translate(0x1001000, 0x10)));
            Assert.That(0x0, Is.EqualTo(memoryPool.Translate(0x2000000, 0x10)));
        }

        [Test]
        public void TestIsMapped()
        {
            MemoryPoolState memoryPool = MemoryPoolState.Create(MemoryPoolState.LocationType.Cpu);

            memoryPool.SetCpuAddress(0x1000000, 0x1000);

            Assert.That(memoryPool.IsMapped(), Is.False);

            memoryPool.DspAddress = 0x2000000;

            Assert.That(memoryPool.IsMapped(), Is.True);
        }
    }
}
