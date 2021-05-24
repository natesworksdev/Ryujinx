using Ryujinx.Graphics.GAL.Multithreading.Model;
using Ryujinx.Graphics.GAL.Multithreading.Resources;
using System;

namespace Ryujinx.Graphics.GAL.Multithreading.Commands.Window
{
    struct WindowPresentCommand : IGALCommand
    {
        public CommandType CommandType => CommandType.WindowPresent;
        private TableRef<ThreadedTexture> _texture;
        private ImageCrop _crop;
        private TableRef<Action> _swapBuffersCallback;

        public void Set(TableRef<ThreadedTexture> texture, ImageCrop crop, TableRef<Action> swapBuffersCallback)
        {
            _texture = texture;
            _crop = crop;
            _swapBuffersCallback = swapBuffersCallback;
        }

        public void Run(ThreadedRenderer threaded, IRenderer renderer)
        {
            threaded.SignalFrame();
            renderer.Window.Present(_texture.Get(threaded)?.Base, _crop, _swapBuffersCallback.Get(threaded));
        }
    }
}
