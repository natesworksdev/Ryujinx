using Ryujinx.Graphics.Gpu.Memory;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Sampler pool.
    /// </summary>
    class SamplerPool : Pool<Sampler, SamplerDescriptor>
    {
        private float _forcedAnisotropy;
        public Dictionary<Sampler, int> Samplers { get; }

        /// <summary>
        /// Constructs a new instance of the sampler pool.
        /// </summary>
        /// <param name="context">GPU context that the sampler pool belongs to</param>
        /// <param name="physicalMemory">Physical memory where the sampler descriptors are mapped</param>
        /// <param name="address">Address of the sampler pool in guest memory</param>
        /// <param name="maximumId">Maximum sampler ID of the sampler pool (equal to maximum samplers minus one)</param>
        public SamplerPool(GpuContext context, PhysicalMemory physicalMemory, ulong address, int maximumId) : base(context, physicalMemory, address, maximumId)
        {
            _forcedAnisotropy = GraphicsConfig.MaxAnisotropy;
            Samplers = new Dictionary<Sampler, int>();
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

            if (SequenceNumber != Context.SequenceNumber)
            {
                if (_forcedAnisotropy != GraphicsConfig.MaxAnisotropy)
                {
                    _forcedAnisotropy = GraphicsConfig.MaxAnisotropy;

                    for (int i = 0; i < Items.Length; i++)
                    {
                        if (Items[i] != null)
                        {
                            Items[i].Dispose();

                            Items[i] = null;
                        }
                    }
                }

                SequenceNumber = Context.SequenceNumber;

                SynchronizeMemory();
            }

            Sampler sampler = Items[id];

            if (sampler == null)
            {
                SamplerDescriptor descriptor = GetDescriptor(id);

                sampler = new Sampler(Context, descriptor);

                Items[id] = sampler;

                DescriptorCache[id] = descriptor;
            }

            return sampler;
        }

        /// <summary>
        /// Loads all the samplers currently registered by the guest application on the pool.
        /// This is required for bindless access, as it's not possible to predict which sampler will be used.
        /// </summary>
        public void LoadAll()
        {
            if (SequenceNumber != Context.SequenceNumber)
            {
                SequenceNumber = Context.SequenceNumber;

                SynchronizeMemory();
            }

            ModifiedEntries.BeginIterating();

            int id;

            while ((id = ModifiedEntries.GetNextAndClear()) >= 0)
            {
                Sampler sampler = Items[id] ?? GetValidated(id);

                if (sampler != null)
                {
                    Samplers.Add(sampler, id);
                }
            }
        }

        /// <summary>
        /// Gets the sampler at the given <paramref name="id"/> from the cache,
        /// or creates a new one if not found.
        /// </summary>
        /// <param name="id">Index of the sampler on the pool</param>
        /// <returns>Sampler for the given pool index</returns>
        private Sampler GetValidated(int id)
        {
            SamplerDescriptor descriptor = GetDescriptor(id);

            if (descriptor.UnpackFontFilterWidth() != 1 || descriptor.UnpackFontFilterHeight() != 1 || (descriptor.Word0 >> 23) != 0)
            {
                return null;
            }

            return Get(id);
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
                    Samplers.Remove(sampler);

                    Items[id] = null;
                }

                ModifiedEntries.Set(id);
            }
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