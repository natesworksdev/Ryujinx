using NUnit.Framework;
using Ryujinx.Audio.Renderer.Server.MemoryPool;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Tests.Audio.Renderer.Server
{
    class AddressInfoTests
    {
        [Test]
        public void EnsureTypeSize()
        {
            Assert.That(0x20, Is.EqualTo(Unsafe.SizeOf<AddressInfo>()));
        }

        [Test]
        public void TestGetReference()
        {
            MemoryPoolState[] memoryPoolState = new MemoryPoolState[1];
            memoryPoolState[0] = MemoryPoolState.Create(MemoryPoolState.LocationType.Cpu);
            memoryPoolState[0].SetCpuAddress(0x1000000, 0x10000);
            memoryPoolState[0].DspAddress = 0x4000000;

            AddressInfo addressInfo = AddressInfo.Create(0x1000000, 0x1000);

            addressInfo.ForceMappedDspAddress = 0x2000000;

            Assert.That(0x2000000, Is.EqualTo(addressInfo.GetReference(true)));

            addressInfo.SetupMemoryPool(memoryPoolState.AsSpan());

            Assert.That(0x4000000, Is.EqualTo(addressInfo.GetReference(true)));
        }
    }
}
