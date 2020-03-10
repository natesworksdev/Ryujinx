using Ryujinx.Common.Logging;
using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.Gpu;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.HLE.HOS.Services.Nv.NvDrvServices.NvMap;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    class SurfaceFlinger : IConsumerListener, IDisposable
    {
        private Switch _device;

        private Dictionary<long, Layer> _layers;

        private bool _isRunning;

        private Thread _composerThread;

        private int _swapInterval;

        private readonly object Lock = new object();

        public long LastId { get; private set; }

        private class Layer
        {
            public IGraphicBufferProducer Producer;
            public BufferItemConsumer     Consumer;
            public KProcess               Owner;
        }

        private class TextureCallbackInformation
        {
            public Layer      Layer;
            public BufferItem Item;
        }

        public SurfaceFlinger(Switch device)
        {
            _device          = device;
            _layers          = new Dictionary<long, Layer>();
            LastId           = 0;

            _composerThread = new Thread(HandleComposition)
            {
                Name = "SurfaceFlinger.Composer"
            };

            _composerThread.Start();

            _swapInterval = 1;
        }

        public IGraphicBufferProducer OpenLayer(KProcess process, long layerId)
        {
            bool needCreate;

            lock (Lock)
            {
                needCreate = GetLayerByIdLocked(layerId) == null;
            }

            if (needCreate)
            {
                CreateLayerFromId(process, layerId);
            }

            return GetProducerByLayerId(layerId);
        }

        public IGraphicBufferProducer CreateLayer(KProcess process, out long layerId)
        {
            layerId = 1;

            lock (Lock)
            {
                foreach (KeyValuePair<long, Layer> pair in _layers)
                {
                    if (pair.Key >= layerId)
                    {
                        layerId = pair.Key + 1;
                    }
                }
            }

            CreateLayerFromId(process, layerId);

            return GetProducerByLayerId(layerId);
        }

        private void CreateLayerFromId(KProcess process, long layerId)
        {
            lock (Lock)
            {
                Logger.PrintInfo(LogClass.SurfaceFlinger, $"Creating layer {layerId}");

                BufferQueue.CreateBufferQueue(_device, process, out BufferQueueProducer producer, out BufferQueueConsumer consumer);

                _layers.Add(layerId, new Layer
                {
                    Producer = producer,
                    Consumer = new BufferItemConsumer(_device, consumer, 0, -1, false, this),
                    Owner    = process
                });

                LastId = layerId;
            }
        }

        public bool CloseLayer(long layerId)
        {
            lock (Lock)
            {
                return _layers.Remove(layerId);
            }
        }

        private Layer GetLayerByIdLocked(long layerId)
        {
            foreach (KeyValuePair<long, Layer> pair in _layers)
            {
                if (pair.Key == layerId)
                {
                    return pair.Value;
                }
            }

            return null;
        }

        public IGraphicBufferProducer GetProducerByLayerId(long layerId)
        {
            lock (Lock)
            {
                Layer layer = GetLayerByIdLocked(layerId);

                if (layer != null)
                {
                    return layer.Producer;
                }
            }

            return null;
        }

        private void HandleComposition()
        {
            _isRunning = true;

            while (_isRunning)
            {
                Compose();

                _device.System.SignalVsync();
                Thread.Sleep(8 * _swapInterval);
            }
        }

        public void Compose()
        {
            lock (Lock)
            {
                // TODO: support multilayers (& multidisplay ?)
                if (_layers.Count == 0)
                {
                    return;
                }

                Layer layer = GetLayerByIdLocked(LastId);

                Status acquireStatus = layer.Consumer.AcquireBuffer(out BufferItem item, 0, false);

                if (acquireStatus == Status.Success)
                {
                    _swapInterval = item.SwapInterval;

                    PostFrameBuffer(layer, item);
                }
                else if (acquireStatus != Status.NoBufferAvailaible)
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private void PostFrameBuffer(Layer layer, BufferItem item)
        { 
            int frameBufferWidth  = item.GraphicBuffer.Object.Width;
            int frameBufferHeight = item.GraphicBuffer.Object.Height;

            int nvMapHandle = item.GraphicBuffer.Object.Buffer.Surfaces[0].NvMapHandle;

            if (nvMapHandle == 0)
            {
                nvMapHandle = item.GraphicBuffer.Object.Buffer.NvMapId;
            }

            int bufferOffset = item.GraphicBuffer.Object.Buffer.Surfaces[0].Offset;

            NvMapHandle map = NvMapDeviceFile.GetMapFromHandle(layer.Owner, nvMapHandle);

            ulong frameBufferAddress = (ulong)(map.Address + bufferOffset);

            Format format = ConvertColorFormat(item.GraphicBuffer.Object.Buffer.Surfaces[0].ColorFormat);

            int bytesPerPixel =
                format == Format.B5G6R5Unorm ||
                format == Format.R4G4B4A4Unorm ? 2 : 4;

            int gobBlocksInY = 1 << item.GraphicBuffer.Object.Buffer.Surfaces[0].BlockHeightLog2;

            // Note: Rotation is being ignored.
            Rect cropRect = item.Crop;

            bool flipX = item.Transform.HasFlag(NativeWindowTransform.FlipX);
            bool flipY = item.Transform.HasFlag(NativeWindowTransform.FlipY);

            ImageCrop crop = new ImageCrop(
                cropRect.Left,
                cropRect.Right,
                cropRect.Top,
                cropRect.Bottom,
                flipX,
                flipY);

            TextureCallbackInformation textureCallbackInformation = new TextureCallbackInformation
            {
                Layer = layer,
                Item  = item
            };

            _device.Gpu.Window.EnqueueFrameThreadSafe(
                frameBufferAddress,
                frameBufferWidth,
                frameBufferHeight,
                0,
                false,
                gobBlocksInY,
                format,
                bytesPerPixel,
                crop,
                AcquireBuffer,
                ReleaseBuffer,
                textureCallbackInformation);
        }

        private void ReleaseBuffer(object obj)
        {
            ReleaseBuffer((TextureCallbackInformation)obj);
        }

        private void ReleaseBuffer(TextureCallbackInformation information)
        {
            information.Layer.Consumer.ReleaseBuffer(information.Item);
        }

        private void AcquireBuffer(GpuContext ignored, object obj)
        {
            AcquireBuffer((TextureCallbackInformation)obj);
        }

        private void AcquireBuffer(TextureCallbackInformation information)
        {
            information.Item.Fence.WaitForever(_device.Gpu);
        }

        public static Format ConvertColorFormat(ColorFormat colorFormat)
        {
            switch (colorFormat)
            {
                case ColorFormat.A8B8G8R8:
                    return Format.R8G8B8A8Unorm;
                case ColorFormat.X8B8G8R8:
                    return Format.R8G8B8A8Unorm;
                case ColorFormat.R5G6B5:
                    return Format.B5G6R5Unorm;
                case ColorFormat.A8R8G8B8:
                    return Format.B8G8R8A8Unorm;
                case ColorFormat.A4B4G4R4:
                    return Format.R4G4B4A4Unorm;
                default:
                    throw new NotImplementedException($"Color Format \"{colorFormat}\" not implemented!");
            }
        }

        public void Dispose()
        {
            _isRunning = false;
        }

        public void OnFrameAvailable(ref BufferItem item)
        {
            _device.Statistics.RecordGameFrameTime();
        }

        public void OnFrameReplaced(ref BufferItem item)
        {
            _device.Statistics.RecordGameFrameTime();
        }

        public void onBuffersReleased()
        {

        }
    }
}
