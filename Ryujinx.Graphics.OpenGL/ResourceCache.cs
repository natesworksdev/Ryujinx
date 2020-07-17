using Ryujinx.Graphics.GAL;
using Ryujinx.Graphics.OpenGL.Image;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Graphics.OpenGL
{
    class DisposedTexture
    {
        public TextureCreateInfo Info;
        public TextureView View;
        public float ScaleFactor;
        public int RemainingFrames;
    }

    class ResourceCache
    {
        private const int DisposedCacheFrames = 2;

        private object _lock = new object();
        private Dictionary<uint, List<DisposedTexture>> _textures = new Dictionary<uint, List<DisposedTexture>>();

        private uint GetTextureKey(TextureCreateInfo info)
        {
            return ((uint)info.Width) | ((uint)info.Height << 16);
        }

        public void AddTexture(TextureView view)
        {
            uint key = GetTextureKey(view.Info);

            List<DisposedTexture> list;
            if (!_textures.TryGetValue(key, out list))
            {
                list = new List<DisposedTexture>();
                _textures.Add(key, list);
            }

            list.Add(new DisposedTexture()
            {
                Info = view.Info,
                View = view,
                ScaleFactor = view.ScaleFactor,
                RemainingFrames = DisposedCacheFrames
            });
        }

        public TextureView TryGetTexture(TextureCreateInfo info, float scaleFactor)
        {
            uint key = GetTextureKey(info);

            List<DisposedTexture> list;
            if (!_textures.TryGetValue(key, out list))
            {
                return null;
            }

            foreach (DisposedTexture texture in list)
            {
                if (texture.View.Info.Equals(info) && scaleFactor == texture.ScaleFactor)
                {
                    // TODO: prepare?
                    list.Remove(texture);
                    return texture.View;
                }
            }

            return null;
        }

        public void Tick()
        {
            foreach (List<DisposedTexture> list in _textures.Values)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    DisposedTexture tex = list[i];

                    if (--tex.RemainingFrames < 0)
                    {
                        tex.View.TrueDispose();
                        list.RemoveAt(i--);
                    }
                }
            }
        }
    }
}
