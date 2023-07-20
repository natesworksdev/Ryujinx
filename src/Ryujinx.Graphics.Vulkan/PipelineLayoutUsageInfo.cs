using System;

namespace Ryujinx.Graphics.Vulkan
{
    struct PipelineLayoutUsageInfo : IEquatable<PipelineLayoutUsageInfo>
    {
        public readonly uint BindlessTexturesCount;
        public readonly uint BindlessSamplersCount;
        public readonly bool UsePushDescriptors;

        public PipelineLayoutUsageInfo(uint bindlessTexturesCount, uint bindlessSamplersCount, bool usePushDescriptors)
        {
            BindlessTexturesCount = bindlessTexturesCount;
            BindlessSamplersCount = bindlessSamplersCount;
            UsePushDescriptors = usePushDescriptors;
        }

        public override bool Equals(object obj)
        {
            return obj is PipelineLayoutUsageInfo other && Equals(other);
        }

        public bool Equals(PipelineLayoutUsageInfo other)
        {
            return BindlessTexturesCount == other.BindlessTexturesCount &&
                   BindlessSamplersCount == other.BindlessSamplersCount &&
                   UsePushDescriptors == other.UsePushDescriptors;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(BindlessTexturesCount, BindlessSamplersCount, UsePushDescriptors);
        }
    }
}