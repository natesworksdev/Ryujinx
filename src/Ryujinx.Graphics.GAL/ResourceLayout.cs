using System;
using System.Collections.ObjectModel;

namespace Ryujinx.Graphics.GAL
{
    public enum ResourceType : byte
    {
        UniformBuffer,
        StorageBuffer,
        Texture,
        Sampler,
        TextureAndSampler,
        Image,
        BufferTexture,
        BufferImage
    }

    public enum ResourceAccess : byte
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = Read | Write
    }

    [Flags]
    public enum ResourceStages : byte
    {
        None = 0,
        Compute = 1 << 0,
        Vertex = 1 << 1,
        TessellationControl = 1 << 2,
        TessellationEvaluation = 1 << 3,
        Geometry = 1 << 4,
        Fragment = 1 << 5
    }

    public readonly struct ResourceDescriptor
    {
        public int Binding { get; }
        public int Count { get; }
        public ResourceType Type { get; }
        public ResourceStages Stages { get; }

        public ResourceDescriptor(int binding, int count, ResourceType type, ResourceStages stages)
        {
            Binding = binding;
            Count = count;
            Type = type;
            Stages = stages;
        }
    }

    public readonly struct ResourceUsage
    {
        public int Binding { get; }
        public ResourceStages Stages { get; }
        public ResourceAccess Access { get; }

        public ResourceUsage(int binding, ResourceStages stages, ResourceAccess access)
        {
            Binding = binding;
            Stages = stages;
            Access = access;
        }
    }

    public readonly struct ResourceDescriptorCollection
    {
        public ReadOnlyCollection<ResourceDescriptor> Descriptors { get; }

        public ResourceDescriptorCollection(ReadOnlyCollection<ResourceDescriptor> descriptors)
        {
            Descriptors = descriptors;
        }
    }

    public readonly struct ResourceUsageCollection
    {
        public ReadOnlyCollection<ResourceUsage> Usages { get; }

        public ResourceUsageCollection(ReadOnlyCollection<ResourceUsage> usages)
        {
            Usages = usages;
        }
    }

    public readonly struct ResourceLayout
    {
        public ReadOnlyCollection<ResourceDescriptorCollection> Sets { get; }
        public ReadOnlyCollection<ResourceUsageCollection> SetUsages { get; }

        public ResourceLayout(
            ReadOnlyCollection<ResourceDescriptorCollection> sets,
            ReadOnlyCollection<ResourceUsageCollection> setUsages)
        {
            Sets = sets;
            SetUsages = setUsages;
        }
    }
}
