using System;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// Values that should override <see cref="TextureInfo"/> parameters.
    /// </summary>
    readonly struct TextureInfoOverride
    {
        /// <summary>
        /// Texture width override.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Texture height override.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Texture depth (for 3D textures), or layers count override.
        /// </summary>
        public int DepthOrLayers { get; }

        /// <summary>
        /// Mipmap levels override.
        /// </summary>
        public int Levels { get; }

        /// <summary>
        /// Texture format override.
        /// </summary>
        public FormatInfo FormatInfo { get; }

        /// <summary>
        /// Constructs the texture override structure.
        /// </summary>
        /// <param name="width">Texture width override</param>
        /// <param name="height">Texture height override</param>
        /// <param name="depthOrLayers">Texture depth (for 3D textures), or layers count override</param>
        /// <param name="levels">Mipmap levels override</param>
        /// <param name="formatInfo">Texture format override</param>
        public TextureInfoOverride(int width, int height, int depthOrLayers, int levels, FormatInfo formatInfo)
        {
            Width = width;
            Height = height;
            DepthOrLayers = depthOrLayers;
            Levels = levels;
            FormatInfo = formatInfo;
        }

        public override bool Equals(object obj)
        {
            return obj is TextureInfoOverride other &&
                other.Width == Width &&
                other.Height == Height &&
                other.DepthOrLayers == DepthOrLayers &&
                other.Levels == Levels &&
                other.FormatInfo.Format == FormatInfo.Format;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Width, Height, DepthOrLayers, Levels, FormatInfo.Format);
        }
    }
}
