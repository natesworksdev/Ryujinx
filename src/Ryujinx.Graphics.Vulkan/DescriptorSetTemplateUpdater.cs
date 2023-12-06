using Ryujinx.Common;
using Silk.NET.Vulkan;
using System;
using System.Runtime.CompilerServices;

namespace Ryujinx.Graphics.Vulkan
{
    unsafe class DescriptorSetTemplateUpdater : IDisposable
    {
        private const int SizeGranularity = 512;

        private DescriptorSetTemplate _activeTemplate;
        private NativeArray<byte> _data;
        private byte* _dataPtr;

        private void EnsureSize(int size)
        {
            if (_data == null || _data.Length < size)
            {
                _data?.Dispose();

                int dataSize = BitUtils.AlignUp(size, SizeGranularity);
                _data = new NativeArray<byte>(dataSize);
            }
        }

        public void Begin(DescriptorSetTemplate template)
        {
            _activeTemplate = template;

            if (template != null)
            {
                EnsureSize(template.Size);

                _dataPtr = _data.Pointer;
            }
        }

        public void Push<T>(ReadOnlySpan<T> values) where T : unmanaged
        {
            for (int i = 0; i < values.Length; i++)
            {
                *((T*)_dataPtr) = values[i];
                _dataPtr += Unsafe.SizeOf<T>();
            }
        }

        public void Commit(VulkanRenderer gd, Device device, DescriptorSet set)
        {
            gd.Api.UpdateDescriptorSetWithTemplate(device, set, _activeTemplate.Template, _data.Pointer);
        }

        public void Dispose()
        {
            _data?.Dispose();
        }
    }
}
