using Ryujinx.Memory.Tracking;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Memory.Tests
{
    class MockVirtualMemoryManager : IVirtualMemoryManager
    {
        public MockVirtualMemoryManager(ulong size, int pageSize)
        {

        }

        public (ulong address, ulong size)[] GetPhysicalRegions(ulong va, ulong size)
        {
            return new (ulong address, ulong size)[] { (va, size) };
        }

        public void Reprotect(ulong va, ulong size, MemoryPermission protection)
        {
            
        }
    }
}
