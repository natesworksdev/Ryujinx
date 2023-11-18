using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;
using System.Collections.Generic;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureBase
    {
        public int Handle { get; protected set; }

        public TextureCreateInfo Info { get; }

        public int Width => Info.Width;
        public int Height => Info.Height;

        public Target Target => Info.Target;
        public Format Format => Info.Format;

        private Dictionary<int, (BindlessManager, long, int)> _bindlessHandles;

        public TextureBase(TextureCreateInfo info)
        {
            Info = info;

            Handle = GL.GenTexture();
        }

        public void Bind(int unit)
        {
            Bind(Target.Convert(), unit);
        }

        protected void Bind(TextureTarget target, int unit)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + unit);
            GL.BindTexture(target, Handle);
        }

        public static void ClearBinding(int unit)
        {
            GL.BindTextureUnit(unit, 0);
        }

        public bool AddBindlessHandle(int textureId, int samplerId, BindlessManager owner, long bindlessHandle)
        {
            var bindlessHandles = _bindlessHandles ??= new();
            return bindlessHandles.TryAdd(samplerId, (owner, bindlessHandle, textureId));
        }

        public void RevokeBindlessAccess()
        {
            if (_bindlessHandles == null)
            {
                return;
            }

            foreach (var kv in _bindlessHandles)
            {
                int samplerId = kv.Key;
                (BindlessManager owner, long bindlessHandle, int textureId) = kv.Value;

                owner.Unregister(textureId, samplerId, bindlessHandle);
            }

            _bindlessHandles.Clear();
            _bindlessHandles = null;
        }
    }
}
