using NUnit.Framework;
using Ryujinx.Memory.Range;
using System.Collections.Generic;

namespace Ryujinx.Tests.Memory.RangeTests
{
    public class PagedMemoryRangeCoalescingEnumeratorTests
    {
        [Test]
        public void PagedMemoryRangeCoalescingEnumerator_For3AlignedPages_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 0;
            const int Size = 12;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeCoalescingEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
            {
                results.Add(memoryRange);
            }

            // Assert
            Assert.AreEqual(1, results.Count);

            Assert.AreEqual(0, results[0].Address);
            Assert.AreEqual(Size, results[0].Size);
        }

        [Test]
        public void PagedMemoryRangeCoalescingEnumerator_For2PagesWithPartialFirstAndLast_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 2;
            const int Size = 6;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeCoalescingEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
            {
                results.Add(memoryRange);
            }

            // Assert
            Assert.AreEqual(1, results.Count);

            Assert.AreEqual(StartAddress, results[0].Address);
            Assert.AreEqual(Size, results[0].Size);
        }

        [Test]
        public void PagedMemoryRangeCoalescingEnumerator_For4PagesWithPartialFirst_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 2;
            const int Size = 14;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeCoalescingEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
            {
                results.Add(memoryRange);
            }

            // Assert
            Assert.AreEqual(1, results.Count);

            Assert.AreEqual(StartAddress, results[0].Address);
            Assert.AreEqual(Size, results[0].Size);
        }

        [Test]
        public void PagedMemoryRangeCoalescingEnumerator_For4PagesWithPartialLast_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 0;
            const int Size = 14;
            const int PageSize = 4;
            var enumerator = new PagedMemoryRangeCoalescingEnumerator(StartAddress, Size, PageSize, x => x);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
            {
                results.Add(memoryRange);
            }

            // Assert
            Assert.AreEqual(1, results.Count);

            Assert.AreEqual(StartAddress, results[0].Address);
            Assert.AreEqual(Size, results[0].Size);
        }

        [Test]
        public void PagedMemoryRangeCoalescingEnumerator_ForPartiallyDiscontiguous4PagesWithPartialLast_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 0;
            const int Size = 14;
            const int PageSize = 4;
            var memoryMap = new Dictionary<ulong, ulong>()
            {
                { 0ul, 0ul }, { 4ul, 4ul }, { 8ul, 20ul }, { 12ul, 24ul },
            };
            ulong MemoryMapLookup(ulong a) => memoryMap.TryGetValue(a, out var value) ? value : a;
            var enumerator = new PagedMemoryRangeCoalescingEnumerator(StartAddress, Size, PageSize, MemoryMapLookup);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
            {
                results.Add(memoryRange);
            }

            // Assert
            Assert.AreEqual(2, results.Count);

            Assert.AreEqual(0, results[0].Address);
            Assert.AreEqual(PageSize * 2, results[0].Size);

            Assert.AreEqual(20, results[1].Address);
            Assert.AreEqual(PageSize + (Size % PageSize), results[1].Size);
        }

        [Test]
        public void PagedMemoryRangeCoalescingEnumerator_ForFullyDiscontiguous4PagesWithPartialLast_HasCorrectResults()
        {
            // Arrange
            const int StartAddress = 0;
            const int Size = 14;
            const int PageSize = 4;
            var memoryMap = new Dictionary<ulong, ulong>()
            {
                { 0ul, 0ul }, { 4ul, 10ul }, { 8ul, 20ul }, { 12ul, 30ul },
            };
            ulong MemoryMapLookup(ulong a) => memoryMap.TryGetValue(a, out var value) ? value : a;
            var enumerator = new PagedMemoryRangeCoalescingEnumerator(StartAddress, Size, PageSize, MemoryMapLookup);

            // Act
            var results = new List<MemoryRange>();
            foreach (var memoryRange in enumerator)
            {
                results.Add(memoryRange);
            }

            // Assert
            Assert.AreEqual(4, results.Count);

            Assert.AreEqual(0, results[0].Address);
            Assert.AreEqual(PageSize, results[0].Size);

            Assert.AreEqual(10, results[1].Address);
            Assert.AreEqual(PageSize, results[1].Size);

            Assert.AreEqual(20, results[2].Address);
            Assert.AreEqual(PageSize, results[2].Size);

            Assert.AreEqual(30, results[3].Address);
            Assert.AreEqual(Size % PageSize, results[3].Size);
        }
    }
}
