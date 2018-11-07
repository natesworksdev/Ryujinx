using Ryujinx.Common;

namespace Ryujinx.HLE.HOS.Kernel
{
    class KMemoryRegionManager
    {
        private static readonly int[] BlockOrders = new int[] { 12, 16, 21, 22, 25, 29, 30 };

        private long Address;
        private long EndAddr;
        private long Size;
        private int  BlockOrdersCount;

        private KMemoryRegionBlock[] Blocks;

        public KMemoryRegionManager(long Address, long Size, long EndAddr)
        {
            Blocks = new KMemoryRegionBlock[BlockOrders.Length];

            this.Address = Address;
            this.Size    = Size;
            this.EndAddr = EndAddr;

            BlockOrdersCount = BlockOrders.Length;

            for (int BlockIndex = 0; BlockIndex < BlockOrdersCount; BlockIndex++)
            {
                Blocks[BlockIndex] = new KMemoryRegionBlock();

                Blocks[BlockIndex].Order = BlockOrders[BlockIndex];

                int NextOrder = BlockIndex == BlockOrdersCount - 1 ? 0 : BlockOrders[BlockIndex + 1];

                Blocks[BlockIndex].NextOrder = NextOrder;

                int CurrBlockSize = 1 << BlockOrders[BlockIndex];
                int NextBlockSize = CurrBlockSize;

                if (NextOrder != 0)
                {
                    NextBlockSize = 1 << NextOrder;
                }

                long StartAligned   = BitUtils.AlignDown(Address, NextBlockSize);
                long EndAddrAligned = BitUtils.AlignDown(EndAddr, CurrBlockSize);

                ulong SizeInBlocksTruncated = (ulong)(EndAddrAligned - StartAligned) >> BlockOrders[BlockIndex];

                long EndAddrRounded = BitUtils.AlignUp(Address + Size, NextBlockSize);

                ulong SizeInBlocksRounded = (ulong)(EndAddrRounded - StartAligned) >> BlockOrders[BlockIndex];

                Blocks[BlockIndex].StartAligned          = StartAligned;
                Blocks[BlockIndex].SizeInBlocksTruncated = (long)SizeInBlocksTruncated;
                Blocks[BlockIndex].SizeInBlocksRounded   = (long)SizeInBlocksRounded;

                ulong CurrSizeInBlocks = SizeInBlocksRounded;

                int MaxLevel = 0;

                do
                {
                    MaxLevel++;
                }
                while ((CurrSizeInBlocks /= 64) != 0);

                Blocks[BlockIndex].MaxLevel = MaxLevel;

                Blocks[BlockIndex].Masks = new long[MaxLevel][];

                CurrSizeInBlocks = SizeInBlocksRounded;

                for (int Level = MaxLevel - 1; Level >= 0; Level--)
                {
                    CurrSizeInBlocks = (CurrSizeInBlocks + 63) / 64;

                    Blocks[BlockIndex].Masks[Level] = new long[CurrSizeInBlocks];
                }
            }

            if (Size != 0)
            {
                FreePages(Address, Size / KMemoryManager.PageSize);
            }
        }

        public KernelResult AllocatePages(long PagesCount, bool Backwards, out KPageList PageList)
        {
            lock (Blocks)
            {
                return AllocatePagesImpl(PagesCount, Backwards, out PageList);
            }
        }

        private KernelResult AllocatePagesImpl(long PagesCount, bool Backwards, out KPageList PageList)
        {
            Backwards = false;

            PageList = new KPageList();

            if (BlockOrdersCount > 0)
            {
                ulong AvailablePages = 0;

                for (int BlockIndex = 0; BlockIndex < BlockOrdersCount; BlockIndex++)
                {
                    KMemoryRegionBlock Block = Blocks[BlockIndex];

                    ulong BlockPagesCount = (1UL << Block.Order) / KMemoryManager.PageSize;

                    AvailablePages += BlockPagesCount * (ulong)Block.FreeCount;
                }

                if (AvailablePages < (ulong)PagesCount)
                {
                    return KernelResult.OutOfMemory;
                }
            }
            else if (PagesCount != 0)
            {
                return KernelResult.OutOfMemory;
            }

            for (int BlockIndex = BlockOrdersCount - 1; BlockIndex >= 0; BlockIndex--)
            {
                KMemoryRegionBlock Block = Blocks[BlockIndex];

                int BestFitBlockSize = 1 << Block.Order;

                int BlockPagesCount = BestFitBlockSize / KMemoryManager.PageSize;

                //Check if this is the best fit for this page size.
                //If so, try allocating as much requested pages as possible.
                while ((ulong)BlockPagesCount <= (ulong)PagesCount)
                {
                    long Address = 0;

                    for (int CurrBlockIndex = BlockIndex;
                             CurrBlockIndex < BlockOrdersCount && Address == 0;
                             CurrBlockIndex++)
                    {
                        Block = Blocks[CurrBlockIndex];

                        int Index = 0;

                        bool ZeroMask = false;

                        for (int Level = 0; Level < Block.MaxLevel; Level++)
                        {
                            long Mask = Block.Masks[Level][Index];

                            if (Mask == 0)
                            {
                                ZeroMask = true;

                                break;
                            }

                            if (Backwards)
                            {
                                Index = (Index * 64 + 63) - BitUtils.CountLeadingZeros64(Mask);
                            }
                            else
                            {
                                Index = Index * 64 + BitUtils.CountLeadingZeros64(BitUtils.ReverseBits64(Mask));
                            }
                        }

                        if ((ulong)Block.SizeInBlocksTruncated <= (ulong)Index || ZeroMask)
                        {
                            continue;
                        }

                        Block.FreeCount--;

                        int TempIdx = Index;

                        for (int Level = Block.MaxLevel - 1; Level >= 0; Level--, TempIdx /= 64)
                        {
                            Block.Masks[Level][TempIdx / 64] &= ~(1L << (TempIdx & 63));

                            if (Block.Masks[Level][TempIdx / 64] != 0)
                            {
                                break;
                            }
                        }

                        Address = Block.StartAligned + ((long)Index << Block.Order);
                    }

                    for (int CurrBlockIndex = BlockIndex;
                             CurrBlockIndex < BlockOrdersCount && Address == 0;
                             CurrBlockIndex++)
                    {
                        Block = Blocks[CurrBlockIndex];

                        int Index = 0;

                        bool ZeroMask = false;

                        for (int Level = 0; Level < Block.MaxLevel; Level++)
                        {
                            long Mask = Block.Masks[Level][Index];

                            if (Mask == 0)
                            {
                                ZeroMask = true;

                                break;
                            }

                            if (Backwards)
                            {
                                Index = Index * 64 + BitUtils.CountLeadingZeros64(BitUtils.ReverseBits64(Mask));
                            }
                            else
                            {
                                Index = (Index * 64 + 63) - BitUtils.CountLeadingZeros64(Mask);
                            }
                        }

                        if ((ulong)Block.SizeInBlocksTruncated <= (ulong)Index || ZeroMask)
                        {
                            continue;
                        }

                        Block.FreeCount--;

                        int TempIdx = Index;

                        for (int Level = Block.MaxLevel - 1; Level >= 0; Level--, TempIdx /= 64)
                        {
                            Block.Masks[Level][TempIdx / 64] &= ~(1L << (TempIdx & 63));

                            if (Block.Masks[Level][TempIdx / 64] != 0)
                            {
                                break;
                            }
                        }

                        Address = Block.StartAligned + ((long)Index << Block.Order);
                    }

                    //The address being zero means that no free space was found on that order,
                    //just give up and try with the next one.
                    if (Address == 0)
                    {
                        break;
                    }

                    //If we are using a larger order than best fit, then we should
                    //split it into smaller blocks.
                    int FirstFreeBlockSize = 1 << Block.Order;

                    if (FirstFreeBlockSize > BestFitBlockSize)
                    {
                        FreePages(Address + BestFitBlockSize, (FirstFreeBlockSize - BestFitBlockSize) / KMemoryManager.PageSize);
                    }

                    //Add new allocated page(s) to the pages list.
                    //If an error occurs, then free all allocated pages and fail.
                    KernelResult Result = PageList.AddRange(Address, BlockPagesCount);

                    if (Result != KernelResult.Success)
                    {
                        FreePages(Address, BlockPagesCount);

                        foreach (KPageNode PageNode in PageList)
                        {
                            FreePages(PageNode.Address, PageNode.PagesCount);
                        }

                        return Result;
                    }

                    PagesCount -= BlockPagesCount;
                }
            }

            //Success case, all requested pages were allocated successfully.
            if (PagesCount == 0)
            {
                return KernelResult.Success;
            }

            //Error case, free allocated pages and return out of memory.
            foreach (KPageNode PageNode in PageList)
            {
                FreePages(PageNode.Address, PageNode.PagesCount);
            }

            return KernelResult.OutOfMemory;
        }

        public void FreePages(long Address, long PagesCount)
        {
            long EndAddr = Address + PagesCount * KMemoryManager.PageSize;

            int BlockIndex = BlockOrdersCount - 1;

            long AddressRounded   = 0;
            long EndAddrTruncated = 0;

            for (; BlockIndex >= 0; BlockIndex--)
            {
                KMemoryRegionBlock AllocInfo = Blocks[BlockIndex];

                long BlockSize = 1L << AllocInfo.Order;

                AddressRounded = (Address + BlockSize - 1) & -BlockSize;

                EndAddrTruncated = EndAddr & -BlockSize;

                if ((ulong)AddressRounded < (ulong)EndAddrTruncated)
                {
                    break;
                }
            }

            void FreeRegion(long CurrAddress)
            {
                for (int CurrBlockIndex = BlockIndex;
                         CurrBlockIndex < BlockOrdersCount && CurrAddress != 0;
                         CurrBlockIndex++)
                {
                    KMemoryRegionBlock Block = Blocks[CurrBlockIndex];

                    Block.FreeCount++;

                    ulong FreedBlocks = (ulong)(CurrAddress - Block.StartAligned) >> Block.Order;

                    int Index = (int)FreedBlocks;

                    for (int Level = Block.MaxLevel - 1; Level >= 0; Level--, Index /= 64)
                    {
                        long Mask = Block.Masks[Level][Index / 64];

                        Block.Masks[Level][Index / 64] = Mask | (1L << (Index & 63));

                        if (Mask != 0)
                        {
                            break;
                        }
                    }

                    int BlockSizeDelta = 1 << (Block.NextOrder - Block.Order);

                    int FreedBlocksTruncated = (int)FreedBlocks & -BlockSizeDelta;

                    if (!Block.TryCoalesce(FreedBlocksTruncated, BlockSizeDelta))
                    {
                        break;
                    }

                    CurrAddress = Block.StartAligned + ((long)FreedBlocksTruncated << Block.Order);
                }
            }

            //Free inside aligned region.
            long BaseAddress = AddressRounded;

            while ((ulong)BaseAddress < (ulong)EndAddrTruncated)
            {
                long BlockSize = 1L << Blocks[BlockIndex].Order;

                FreeRegion(BaseAddress);

                BaseAddress += BlockSize;
            }

            int NextBlockIndex = BlockIndex - 1;

            //Free region between Address and aligned region start.
            BaseAddress = AddressRounded;

            for (BlockIndex = NextBlockIndex; BlockIndex >= 0; BlockIndex--)
            {
                long BlockSize = 1L << Blocks[BlockIndex].Order;

                while ((ulong)(BaseAddress - BlockSize) >= (ulong)Address)
                {
                    BaseAddress -= BlockSize;

                    FreeRegion(BaseAddress);
                }
            }

            //Free region between aligned region end and End Address.
            BaseAddress = EndAddrTruncated;

            for (BlockIndex = NextBlockIndex; BlockIndex >= 0; BlockIndex--)
            {
                long BlockSize = 1L << Blocks[BlockIndex].Order;

                while ((ulong)(BaseAddress + BlockSize) <= (ulong)EndAddr)
                {
                    FreeRegion(BaseAddress);

                    BaseAddress += BlockSize;
                }
            }
        }
    }
}