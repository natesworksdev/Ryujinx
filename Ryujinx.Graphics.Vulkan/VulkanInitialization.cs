using Ryujinx.Common.Configuration;
using Ryujinx.Common.Logging;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Ryujinx.Graphics.Vulkan
{
    unsafe static class VulkanInitialization
    {
        private const uint InvalidIndex = uint.MaxValue;
        private const string AppName = "Ryujinx.Graphics.Vulkan";
        private const int QueuesCount = 2;

        private static readonly string[] RequiredExtensions = new string[]
        {
            KhrSwapchain.ExtensionName,
            "VK_EXT_shader_subgroup_vote",
            ExtTransformFeedback.ExtensionName
        };

        private static readonly string[] DesirableExtensions = new string[]
        {
            ExtConditionalRendering.ExtensionName,
            ExtExtendedDynamicState.ExtensionName,
            KhrDrawIndirectCount.ExtensionName,
            "VK_EXT_index_type_uint8",
            "VK_EXT_custom_border_color",
            "VK_EXT_robustness2"
        };

        public static Instance CreateInstance(Vk api, GraphicsDebugLevel logLevel, string[] requiredExtensions, out ExtDebugReport debugReport, out DebugReportCallbackEXT debugReportCallback)
        {
            var enabledLayers = new List<string>();

            void AddAvailableLayer(string layerName)
            {
                uint layerPropertiesCount;

                api.EnumerateInstanceLayerProperties(&layerPropertiesCount, null).ThrowOnError();

                LayerProperties[] layerProperties = new LayerProperties[layerPropertiesCount];

                fixed (LayerProperties* pLayerProperties = layerProperties)
                {
                    api.EnumerateInstanceLayerProperties(&layerPropertiesCount, layerProperties).ThrowOnError();

                    for (int i = 0; i < layerPropertiesCount; i++)
                    {
                        string currentLayerName = Marshal.PtrToStringAnsi((IntPtr)pLayerProperties[i].LayerName);

                        if (currentLayerName == layerName)
                        {
                            enabledLayers.Add(layerName);
                            return;
                        }
                    }
                }

                Logger.Warning?.Print(LogClass.Gpu, $"Missing layer {layerName}");
            }

            if (logLevel == GraphicsDebugLevel.Slowdowns || logLevel == GraphicsDebugLevel.All)
            {
                AddAvailableLayer("VK_LAYER_KHRONOS_validation");
            }

            var enabledExtensions = requiredExtensions.Append(ExtDebugReport.ExtensionName).ToArray();

            var appName = Marshal.StringToHGlobalAnsi(AppName);

            var applicationInfo = new ApplicationInfo
            {
                PApplicationName = (byte*)appName,
                ApplicationVersion = 1,
                PEngineName = (byte*)appName,
                EngineVersion = 1,
                ApiVersion = Vk.Version12.Value
            };

            IntPtr* ppEnabledExtensions = stackalloc IntPtr[enabledExtensions.Length];
            IntPtr* ppEnabledLayers = stackalloc IntPtr[enabledLayers.Count];

            for (int i = 0; i < enabledExtensions.Length; i++)
            {
                ppEnabledExtensions[i] = Marshal.StringToHGlobalAnsi(enabledExtensions[i]);
            }

            for (int i = 0; i < enabledLayers.Count; i++)
            {
                ppEnabledLayers[i] = Marshal.StringToHGlobalAnsi(enabledLayers[i]);
            }

            var instanceCreateInfo = new InstanceCreateInfo
            {
                SType = StructureType.InstanceCreateInfo,
                PApplicationInfo = &applicationInfo,
                PpEnabledExtensionNames = (byte**)ppEnabledExtensions,
                PpEnabledLayerNames = (byte**)ppEnabledLayers,
                EnabledExtensionCount = (uint)enabledExtensions.Length,
                EnabledLayerCount = (uint)enabledLayers.Count
            };

            api.CreateInstance(in instanceCreateInfo, null, out var instance).ThrowOnError();

            Marshal.FreeHGlobal(appName);

            for (int i = 0; i < enabledExtensions.Length; i++)
            {
                Marshal.FreeHGlobal(ppEnabledExtensions[i]);
            }

            for (int i = 0; i < enabledLayers.Count; i++)
            {
                Marshal.FreeHGlobal(ppEnabledLayers[i]);
            }

            if (!api.TryGetInstanceExtension(instance, out debugReport))
            {
                throw new Exception();
                // TODO: Exception.
            }

            if (logLevel != GraphicsDebugLevel.None)
            {
                var flags = logLevel switch
                {
                    GraphicsDebugLevel.Error => DebugReportFlagsEXT.DebugReportErrorBitExt,
                    GraphicsDebugLevel.Slowdowns => DebugReportFlagsEXT.DebugReportErrorBitExt | DebugReportFlagsEXT.DebugReportPerformanceWarningBitExt,
                    GraphicsDebugLevel.All => DebugReportFlagsEXT.DebugReportInformationBitExt        |
                                              DebugReportFlagsEXT.DebugReportWarningBitExt            |
                                              DebugReportFlagsEXT.DebugReportPerformanceWarningBitExt |
                                              DebugReportFlagsEXT.DebugReportErrorBitExt              |
                                              DebugReportFlagsEXT.DebugReportDebugBitExt,
                    _ => throw new NotSupportedException()
                };
                var debugReportCallbackCreateInfo = new DebugReportCallbackCreateInfoEXT()
                {
                    SType = StructureType.DebugReportCallbackCreateInfoExt,
                    Flags = flags,
                    PfnCallback = new PfnDebugReportCallbackEXT(DebugReport)
                };

                debugReport.CreateDebugReportCallback(instance, in debugReportCallbackCreateInfo, null, out debugReportCallback).ThrowOnError();
            }
            else
            {
                debugReportCallback = default;
            }

            return instance;
        }

        private static string[] ExcludedMessages = new string[]
        {
            // NOTE: Done on purpuse right now.
            "UNASSIGNED-CoreValidation-Shader-OutputNotConsumed",
            // TODO: Figure out if fixable
            "VUID-vkCmdDrawIndexed-None-04584",
            // TODO: might be worth looking into making this happy to possibly optimize copies.
            "UNASSIGNED-CoreValidation-DrawState-InvalidImageLayout"
        };

        private unsafe static uint DebugReport(
            uint flags,
            DebugReportObjectTypeEXT objectType,
            ulong @object,
            nuint location,
            int messageCode,
            byte* layerPrefix,
            byte* message,
            void* userData)
        {
            var msg = Marshal.PtrToStringAnsi((IntPtr)message);

            foreach (string excludedMessagePart in ExcludedMessages)
            {
                if (msg.Contains(excludedMessagePart))
                {
                    return 0;
                }
            }

            DebugReportFlagsEXT debugFlags = (DebugReportFlagsEXT)flags;

            if (debugFlags.HasFlag(DebugReportFlagsEXT.DebugReportErrorBitExt))
            {
                Logger.Error?.Print(LogClass.Gpu, msg);
                //throw new Exception(msg);
            }
            else if (debugFlags.HasFlag(DebugReportFlagsEXT.DebugReportWarningBitExt))
            {
                Logger.Warning?.Print(LogClass.Gpu, msg);
            }
            else if (debugFlags.HasFlag(DebugReportFlagsEXT.DebugReportInformationBitExt))
            {
                Logger.Info?.Print(LogClass.Gpu, msg);
            }
            else if (debugFlags.HasFlag(DebugReportFlagsEXT.DebugReportPerformanceWarningBitExt))
            {
                Logger.Warning?.Print(LogClass.Gpu, msg);
            }
            else
            {
                Logger.Debug?.Print(LogClass.Gpu, msg);
            }

            return 0;
        }

        public static PhysicalDevice FindSuitablePhysicalDevice(Vk api, Instance instance, SurfaceKHR surface)
        {
            uint physicalDeviceCount;

            api.EnumeratePhysicalDevices(instance, &physicalDeviceCount, null).ThrowOnError();

            PhysicalDevice[] physicalDevices = new PhysicalDevice[physicalDeviceCount];

            fixed (PhysicalDevice* pPhysicalDevices = physicalDevices)
            {
                api.EnumeratePhysicalDevices(instance, &physicalDeviceCount, pPhysicalDevices).ThrowOnError();
            }

            if (physicalDevices.Length > 1)
            {
                return physicalDevices[0];
            }

            for (int i = 0; i < physicalDevices.Length; i++)
            {
                if (IsSuitableDevice(api, physicalDevices[i], surface))
                {
                    return physicalDevices[i];
                }
            }

            throw new VulkanException("Initialization failed, none of the available GPUs meets the minimum requirements.");
        }

        private static bool IsSuitableDevice(Vk api, PhysicalDevice physicalDevice, SurfaceKHR surface)
        {
            int extensionMatches = 0;
            uint propertiesCount;

            api.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &propertiesCount, null).ThrowOnError();

            ExtensionProperties[] extensionProperties = new ExtensionProperties[propertiesCount];

            fixed (ExtensionProperties* pExtensionProperties = extensionProperties)
            {
                api.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &propertiesCount, pExtensionProperties).ThrowOnError();

                for (int i = 0; i < propertiesCount; i++)
                {
                    string extensionName = Marshal.PtrToStringAnsi((IntPtr)pExtensionProperties[i].ExtensionName);

                    if (RequiredExtensions.Contains(extensionName))
                    {
                        extensionMatches++;
                    }
                }
            }

            return extensionMatches == RequiredExtensions.Length && FindSuitableQueueFamily(api, physicalDevice, surface, out _) != InvalidIndex;
        }

        public static uint FindSuitableQueueFamily(Vk api, PhysicalDevice physicalDevice, SurfaceKHR surface, out uint queueCount)
        {
            const QueueFlags RequiredFlags = QueueFlags.QueueGraphicsBit | QueueFlags.QueueComputeBit;

            var khrSurface = new KhrSurface(api.Context);

            uint propertiesCount;

            api.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &propertiesCount, null);

            QueueFamilyProperties[] properties = new QueueFamilyProperties[propertiesCount];

            fixed (QueueFamilyProperties* pProperties = properties)
            {
                api.GetPhysicalDeviceQueueFamilyProperties(physicalDevice, &propertiesCount, pProperties);
            }

            for (uint index = 0; index < propertiesCount; index++)
            {
                var queueFlags = properties[index].QueueFlags;

                khrSurface.GetPhysicalDeviceSurfaceSupport(physicalDevice, index, surface, out var surfaceSupported).ThrowOnError();

                if (queueFlags.HasFlag(RequiredFlags) && surfaceSupported)
                {
                    queueCount = properties[index].QueueCount;
                    return index;
                }
            }

            queueCount = 0;
            return InvalidIndex;
        }

        public static Device CreateDevice(Vk api, PhysicalDevice physicalDevice, uint queueFamilyIndex, string[] supportedExtensions, uint queueCount)
        {
            if (queueCount > QueuesCount)
            {
                queueCount = QueuesCount;
            }

            float* queuePriorities = stackalloc float[(int)queueCount];

            for (int i = 0; i < queueCount; i++)
            {
                queuePriorities[i] = 1f;
            }

            var queueCreateInfo = new DeviceQueueCreateInfo()
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = queueFamilyIndex,
                QueueCount = queueCount,
                PQueuePriorities = queuePriorities
            };

            var features = new PhysicalDeviceFeatures()
            {
                DepthBiasClamp = true,
                DepthClamp = true,
                DualSrcBlend = true,
                FragmentStoresAndAtomics = true,
                GeometryShader = true,
                ImageCubeArray = true,
                IndependentBlend = true,
                LogicOp = true,
                MultiViewport = true,
                PipelineStatisticsQuery = true,
                SamplerAnisotropy = true,
                ShaderClipDistance = true,
                ShaderImageGatherExtended = true,
                // ShaderStorageImageReadWithoutFormat = true,
                // ShaderStorageImageWriteWithoutFormat = true,
                VertexPipelineStoresAndAtomics = true
            };

            var featuresIndexU8 = new PhysicalDeviceIndexTypeUint8FeaturesEXT()
            {
                SType = StructureType.PhysicalDeviceIndexTypeUint8FeaturesExt,
                IndexTypeUint8 = true
            };

            var featuresTransformFeedback = new PhysicalDeviceTransformFeedbackFeaturesEXT()
            {
                SType = StructureType.PhysicalDeviceTransformFeedbackFeaturesExt,
                PNext = supportedExtensions.Contains("VK_EXT_index_type_uint8") ? &featuresIndexU8 : null,
                TransformFeedback = true
            };

            var featuresRobustness2 = new PhysicalDeviceRobustness2FeaturesEXT()
            {
                SType = StructureType.PhysicalDeviceRobustness2FeaturesExt,
                PNext = &featuresTransformFeedback,
                NullDescriptor = true
            };

            var featuresVk12 = new PhysicalDeviceVulkan12Features()
            {
                SType = StructureType.PhysicalDeviceVulkan12Features,
                PNext = &featuresRobustness2,
                DrawIndirectCount = supportedExtensions.Contains(KhrDrawIndirectCount.ExtensionName)
            };

            var enabledExtensions = RequiredExtensions.Union(DesirableExtensions.Intersect(supportedExtensions)).ToArray();

            IntPtr* ppEnabledExtensions = stackalloc IntPtr[enabledExtensions.Length];

            for (int i = 0; i < enabledExtensions.Length; i++)
            {
                ppEnabledExtensions[i] = Marshal.StringToHGlobalAnsi(enabledExtensions[i]);
            }

            var deviceCreateInfo = new DeviceCreateInfo()
            {
                SType = StructureType.DeviceCreateInfo,
                PNext = &featuresVk12,
                QueueCreateInfoCount = 1,
                PQueueCreateInfos = &queueCreateInfo,
                PpEnabledExtensionNames = (byte**)ppEnabledExtensions,
                EnabledExtensionCount = (uint)enabledExtensions.Length,
                PEnabledFeatures = &features
            };

            api.CreateDevice(physicalDevice, in deviceCreateInfo, null, out var device).ThrowOnError();

            for (int i = 0; i < enabledExtensions.Length; i++)
            {
                Marshal.FreeHGlobal(ppEnabledExtensions[i]);
            }

            return device;
        }

        public static string[] GetSupportedExtensions(Vk api, PhysicalDevice physicalDevice)
        {
            uint propertiesCount;

            api.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &propertiesCount, null).ThrowOnError();

            ExtensionProperties[] extensionProperties = new ExtensionProperties[propertiesCount];

            fixed (ExtensionProperties* pExtensionProperties = extensionProperties)
            {
                api.EnumerateDeviceExtensionProperties(physicalDevice, (byte*)null, &propertiesCount, pExtensionProperties).ThrowOnError();
            }

            return extensionProperties.Select(x => Marshal.PtrToStringAnsi((IntPtr)x.ExtensionName)).ToArray();
        }

        public static CommandBufferPool CreateCommandBufferPool(Vk api, Device device, Queue queue, uint queueFamilyIndex)
        {
            return new CommandBufferPool(api, device, queue, queueFamilyIndex);
        }
    }
}
