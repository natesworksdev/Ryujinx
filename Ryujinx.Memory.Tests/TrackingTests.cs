using NUnit.Framework;
using Ryujinx.Memory.Tracking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Memory.Tests
{
    public class TrackingTests
    {
        private const int RndCnt = 3;

        // Test summary:
        // Multithreading
        // Multi Region Handle Dirty Flags
        // Read Action triggers once
        // Unmapping removes tracking regions
        // Disposing tracking regions removes handles (?)

        private const ulong MemorySize = 0x8000;
        private const int PageSize = 4096;

        private MemoryBlock _memoryBlock;
        private MemoryTracking _tracking;

        [SetUp]
        public void Setup()
        {
            _memoryBlock = new MemoryBlock(MemorySize);
            _tracking = new MemoryTracking(new MockVirtualMemoryManager(MemorySize, PageSize), _memoryBlock, PageSize);
        }

        [TearDown]
        public void Teardown()
        {
            _memoryBlock.Dispose();
        }

        private bool TestSingleWrite(RegionHandle handle, ulong address, ulong size)
        {
            handle.Reprotect();
            _tracking.VirtualMemoryEvent(address, size, true);
            return handle.Dirty;
        }

        [Test]
        public void SingleRegion()
        {
            RegionHandle handle = _tracking.BeginTracking(0, PageSize);
            (ulong address, ulong size)? readTrackingTriggered = null;
            handle.RegisterAction((address, size) =>
            {
                readTrackingTriggered = (address, size);
            });

            bool dirtyInitial = handle.Dirty;
            Assert.True(dirtyInitial); // Handle starts dirty.

            handle.Reprotect();

            bool dirtyAfterReprotect = handle.Dirty;
            Assert.False(dirtyAfterReprotect); // Handle is no longer dirty.

            _tracking.VirtualMemoryEvent(PageSize * 2, 4, true);
            _tracking.VirtualMemoryEvent(PageSize * 2, 4, false);

            bool dirtyAfterUnrelatedReadWrite = handle.Dirty;
            Assert.False(dirtyAfterUnrelatedReadWrite); // Not dirtied, as the write was to an unrelated address.

            Assert.IsNull(readTrackingTriggered); // Hasn't been triggered yet

            _tracking.VirtualMemoryEvent(0, 4, false);

            bool dirtyAfterRelatedRead = handle.Dirty;
            Assert.False(dirtyAfterRelatedRead); // Only triggers on write.
            Assert.AreEqual(readTrackingTriggered, (0UL, 4UL)); // Read action was triggered.

            readTrackingTriggered = null;
            _tracking.VirtualMemoryEvent(0, 4, true);

            bool dirtyAfterRelatedWrite = handle.Dirty;
            Assert.True(dirtyAfterRelatedWrite); // Dirty flag should now be set.

            _tracking.VirtualMemoryEvent(4, 4, true);
            bool dirtyAfterRelatedWrite2 = handle.Dirty;
            Assert.True(dirtyAfterRelatedWrite2); // Dirty flag should still be set.

            handle.Reprotect();

            bool dirtyAfterReprotect2 = handle.Dirty;
            Assert.False(dirtyAfterReprotect2); // Handle is no longer dirty.
        }

        [Test]
        public void OverlappingRegions()
        {
            RegionHandle allHandle = _tracking.BeginTracking(0, PageSize * 16);
            allHandle.Reprotect();

            (ulong address, ulong size)? readTrackingTriggeredAll = null;
            Action registerReadAction = () =>
            {
                readTrackingTriggeredAll = null;
                allHandle.RegisterAction((address, size) =>
                {
                    readTrackingTriggeredAll = (address, size);
                });
            };
            registerReadAction();

            // Create 16 page sized handles contained within the allHandle.
            RegionHandle[] containedHandles = new RegionHandle[16];

            for (int i = 0; i < 16; i++)
            {
                containedHandles[i] = _tracking.BeginTracking((ulong)i * PageSize, PageSize);
                containedHandles[i].Reprotect();
            }

            for (int i = 0; i < 16; i++)
            {
                // No handles are dirty.
                Assert.False(allHandle.Dirty);
                Assert.IsNull(readTrackingTriggeredAll);
                for (int j = 0; j < 16; j++)
                {
                    Assert.False(containedHandles[j].Dirty);
                }

                _tracking.VirtualMemoryEvent((ulong)i * PageSize, 1, true);

                // Only the handle covering the entire range and the relevant contained handle are dirty.
                Assert.True(allHandle.Dirty);
                Assert.AreEqual(readTrackingTriggeredAll, ((ulong)i * PageSize, 1UL)); // Triggered read tracking
                for (int j = 0; j < 16; j++)
                {
                    if (j == i)
                    {
                        Assert.True(containedHandles[j].Dirty);
                    }
                    else
                    {
                        Assert.False(containedHandles[j].Dirty);
                    }
                }

                // Clear flags and reset read action.
                registerReadAction();
                allHandle.Reprotect();
                containedHandles[i].Reprotect();
            }
        }

        [Test]
        public void PageAlignment(
            [Values(1ul, 512ul, 2048ul, 4096ul, 65536ul)] [Random(1ul, 65536ul, RndCnt)] ulong address,
            [Values(1ul, 4ul, 1024ul, 4096ul, 65536ul)] [Random(1ul, 65536ul, RndCnt)] ulong size)
        {
            ulong alignedStart = (address / PageSize) * PageSize;
            ulong alignedEnd = ((address + size + PageSize - 1) / PageSize) * PageSize;
            ulong alignedSize = alignedEnd - alignedStart;

            RegionHandle handle = _tracking.BeginTracking(address, size);

            // Anywhere inside the pages the region is contained on should trigger.

            bool originalRangeTriggers = TestSingleWrite(handle, address, size);
            Assert.True(originalRangeTriggers);

            bool alignedRangeTriggers = TestSingleWrite(handle, alignedStart, alignedSize);
            Assert.True(alignedRangeTriggers);

            bool alignedStartTriggers = TestSingleWrite(handle, alignedStart, 1);
            Assert.True(alignedStartTriggers);

            bool alignedEndTriggers = TestSingleWrite(handle, alignedEnd - 1, 1);
            Assert.True(alignedEndTriggers);

            // Outside the tracked range should not trigger.

            bool alignedBeforeTriggers = TestSingleWrite(handle, alignedStart - 1, 1);
            Assert.False(alignedBeforeTriggers);

            bool alignedAfterTriggers = TestSingleWrite(handle, alignedEnd, 1);
            Assert.False(alignedAfterTriggers);
        }
    }
}
