using Ryujinx.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Sampler pool.
    /// </summary>
    class SamplerPool : Pool<Sampler, SamplerDescriptor>
    {
        private readonly BitMap _modifiedEntries;
        private readonly HashSet<Texture> _dependants;

        public List<int> SamplerIds { get; }

        /// <summary>
        /// Constructs a new instance of the sampler pool.
        /// </summary>
        /// <param name="context">GPU context that the sampler pool belongs to</param>
        /// <param name="address">Address of the sampler pool in guest memory</param>
        /// <param name="maximumId">Maximum sampler ID of the sampler pool (equal to maximum samplers minus one)</param>
        public SamplerPool(GpuContext context, ulong address, int maximumId) : base(context, address, maximumId)
        {
            int entries = BitUtils.DivRoundUp(maximumId + 1, DescriptorSize);
            _modifiedEntries = new BitMap(entries);
            _dependants = new HashSet<Texture>();
            SamplerIds = new List<int>();
        }

        /// <summary>
        /// Gets the sampler with the given ID.
        /// </summary>
        /// <param name="id">ID of the sampler. This is effectively a zero-based index</param>
        /// <returns>The sampler with the given ID</returns>
        public override Sampler Get(int id)
        {
            if ((uint)id >= Items.Length)
            {
                return null;
            }

            Sampler sampler = Items[id];

            if (sampler == null)
            {
                Items[id] = sampler = GetSampler(id);
            }

            return sampler;
        }

        /// <summary>
        /// Gets the sampler at the given <paramref name="id"/> from the cache,
        /// or creates a new one if not found.
        /// </summary>
        /// <param name="id">Index of the sampler on the pool</param>
        /// <returns>Sampler for the given pool index</returns>
        private Sampler GetSampler(int id)
        {
            ulong address = Address + (ulong)(uint)id * DescriptorSize;

            ReadOnlySpan<byte> data = Context.PhysicalMemory.GetSpan(address, DescriptorSize);

            SamplerDescriptor descriptor = MemoryMarshal.Cast<byte, SamplerDescriptor>(data)[0];

            DescriptorCache[id] = descriptor;

            if (descriptor.UnpackFontFilterWidth() != 1 || descriptor.UnpackFontFilterHeight() != 1 || (descriptor.Word0 >> 23) != 0)
            {
                return null;
            }

            return new Sampler(Context, descriptor);
        }

        /// <summary>
        /// Checks if the sampler at the given pool <paramref name="id"/>
        /// has the same parameters as <paramref name="current"/>.
        /// </summary>
        /// <param name="current">Sampler to compare with</param>
        /// <param name="id">Index on the pool to compare with</param>
        /// <returns>True if the parameters are equal, false otherwise</returns>
        private bool IsPerfectMatch(Sampler current, int id)
        {
            ulong address = Address + (ulong)(uint)id * DescriptorSize;

            ReadOnlySpan<byte> data = Context.PhysicalMemory.GetSpan(address, DescriptorSize);

            SamplerDescriptor descriptor = MemoryMarshal.Cast<byte, SamplerDescriptor>(data)[0];

            return current.Descriptor.Equals(descriptor);
        }

        /// <summary>
        /// Implementation of the sampler pool range invalidation.
        /// </summary>
        /// <param name="address">Start address of the range of the sampler pool</param>
        /// <param name="size">Size of the range being invalidated</param>
        protected override void InvalidateRangeImpl(ulong address, ulong size)
        {
            ulong endAddress = address + size;

            for (; address < endAddress; address += DescriptorSize)
            {
                int id = (int)((address - Address) / DescriptorSize);

                Sampler sampler = Items[id];

                if (sampler != null)
                {
                    SamplerDescriptor descriptor = GetDescriptor(id);

                    // If the descriptors are the same, the sampler is still valid.
                    if (descriptor.Equals(ref DescriptorCache[id]))
                    {
                        continue;
                    }

                    sampler.Dispose();
                    SamplerIds.Remove(id);

                    Items[id] = null;
                }

                _modifiedEntries.Set(id);
            }
        }

        /// <summary>
        /// Loads all the samplers currently registered by the guest application on the pool.
        /// This is required for bindless access, as it's not possible to predict which samplers will be used.
        /// </summary>
        public void LoadAll()
        {
            _modifiedEntries.BeginIterating();

            int id;

            while ((id = _modifiedEntries.GetNextAndClear()) >= 0)
            {
                Sampler sampler = Items[id] ?? GetSampler(id);

                if (sampler != null)
                {
                    SamplerIds.Add(id);

                    foreach (Texture texture in _dependants)
                    {
                        texture.NotifySamplerCreation(id, sampler);
                    }
                }

                Items[id] = sampler;
            }
        }

        /// <summary>
        /// Adds a texture that should be notified of all modifications to this sampler pool.
        /// </summary>
        /// <param name="texture">Texture to notify</param>
        public void AddDependant(Texture texture)
        {
            _dependants.Add(texture);
        }

        /// <summary>
        /// Removes a texture from the sampler pool dependency list, this stops the texture from being notified.
        /// </summary>
        /// <param name="texture">Texture to remove</param>
        public void RemoveDependant(Texture texture)
        {
            _dependants.Remove(texture);
        }

        /// <summary>
        /// Deletes a given sampler pool entry.
        /// The host memory used by the sampler is released by the driver.
        /// </summary>
        /// <param name="item">The entry to be deleted</param>
        protected override void Delete(Sampler item)
        {
            item?.Dispose();
        }
    }
}