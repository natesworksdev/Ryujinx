using OpenTK.Graphics.OpenGL;
using Ryujinx.Graphics.GAL;

namespace Ryujinx.Graphics.OpenGL.Image
{
    class TextureBase : ITextureInfo
    {
        private BindlessTexture _bindlessTexture;

        private readonly Renderer _renderer;

        public TextureCreateInfo Info { get; }

        public int Width => Info.Width;
        public int Height => Info.Height;
        public float ScaleFactor { get; }

        public int Handle { get; protected set; }

        public bool Bindless { get; private set; }

        public Target Target => Info.Target;
        public Format Format => Info.Format;

        public TextureBase(Renderer renderer, TextureCreateInfo info, float scaleFactor = 1f)
        {
            _renderer = renderer;
            Info = info;
            ScaleFactor = scaleFactor;

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

        public long GetBindlessHandle(ISampler sampler)
        {
            if (_bindlessTexture == null)
            {
                _bindlessTexture = new BindlessTexture(this);
                Bindless = true;
            }

            return _bindlessTexture.GetHandle(sampler);
        }

        public virtual void IncrementReferenceCount()
        {
        }

        public virtual void DecrementReferenceCount()
        {
        }

        public virtual void Release()
        {
            if (_bindlessTexture != null)
            {
                _renderer.DisposalQueue.Add(_bindlessTexture);
                _bindlessTexture = null;
            }
        }
    }
}
