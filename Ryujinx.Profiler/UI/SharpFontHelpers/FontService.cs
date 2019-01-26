using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SharpFont;

namespace Ryujinx.Profiler.UI.SharpFontHelpers
{
    public class FontService
    {
        private struct CharacterInfo
        {
            public float left, right, bottom, top;
            public float height, aspectRatio, xBearing, yBearing, advance;
            public int width;
        }

        private const int SheetWidth = 256;
        private const int SheetHeight = 256;
        private int characterTextureSheet;
        private CharacterInfo[] characters;

        public Color fontColor = Color.Black;

        public void InitalizeTextures()
        {
            // Create and init some vars
            uint[] rawCharacterSheet = new uint[SheetWidth * SheetHeight];
            int x, y, lineOffset, maxHeight;
            x = y = lineOffset = maxHeight = 0;
            characters = new CharacterInfo[94];

            // Get font
            var font = new FontFace(File.OpenRead(Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData), @"RyuFs\system\fonts\FontStandard.ttf")));

            // Update raw data for each character
            for (int i = 0; i < 94; i++)
            {
                float xBearing, yBearing, advance;
                var surface = RenderSurface((char)(i + 33), font, out xBearing, out yBearing, out advance);

                characters[i] = UpdateTexture(surface, ref rawCharacterSheet, ref x, ref y, ref lineOffset);
                characters[i].xBearing = xBearing;
                characters[i].yBearing = yBearing;
                characters[i].advance = advance;

                if (maxHeight < characters[i].height)
                    maxHeight = (int)characters[i].height;
            }

            // Fix height for characters shorter than line height
            for (int i = 0; i < 94; i++)
            {
                characters[i].xBearing /= characters[i].width;
                characters[i].yBearing /= maxHeight;
                characters[i].advance /= characters[i].width;
                characters[i].height /= maxHeight;
                characters[i].aspectRatio = (float)characters[i].width / maxHeight;
            }

            // Convert raw data into texture
            characterTextureSheet = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, characterTextureSheet);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
            
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, SheetWidth, SheetHeight, 0, PixelFormat.Rgba, PixelType.UnsignedInt8888, rawCharacterSheet);

            GL.BindTexture(TextureTarget.Texture2D, 0);
        }

        public float DrawText(string text, float x, float y, float height)
        {
            float originalX = x;

            // Use font map texture
            GL.BindTexture(TextureTarget.Texture2D, characterTextureSheet);

            // Enable blending and textures
            GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            
            // Draw all characters
            GL.Begin(PrimitiveType.Triangles);
            GL.Color4(fontColor);

            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == ' ')
                {
                    x += height / 4;
                    continue;
                }

                CharacterInfo charInfo = characters[text[i] - 33];
                float width = (charInfo.aspectRatio * height);
                x += (charInfo.xBearing * charInfo.aspectRatio) * width;
                float right = x + width;
                DrawChar(charInfo, x, right, y + height * (charInfo.height - charInfo.yBearing), y - height * charInfo.yBearing);
                x = right + charInfo.advance * charInfo.aspectRatio;
            }

            GL.End();

            // Cleanup for caller
            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Disable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Blend);

            // Return width of rendered text
            return x - originalX;
        }

        private void DrawChar(CharacterInfo charInfo, float left, float right, float top, float bottom)
        {
            GL.TexCoord2(charInfo.left, charInfo.bottom);  GL.Vertex2(left, bottom);
            GL.TexCoord2(charInfo.left, charInfo.top);     GL.Vertex2(left, top);
            GL.TexCoord2(charInfo.right, charInfo.top);    GL.Vertex2(right, top);

            GL.TexCoord2(charInfo.right, charInfo.top);    GL.Vertex2(right, top);
            GL.TexCoord2(charInfo.right, charInfo.bottom); GL.Vertex2(right, bottom);
            GL.TexCoord2(charInfo.left, charInfo.bottom);  GL.Vertex2(left, bottom);
        }

        public unsafe Surface RenderSurface(char c, FontFace font, out float xBearing, out float yBearing, out float advance)
        {
            var glyph = font.GetGlyph(c, 32);
            xBearing = glyph.HorizontalMetrics.Bearing.X;
            yBearing = glyph.RenderHeight - glyph.HorizontalMetrics.Bearing.Y;
            advance = glyph.HorizontalMetrics.Advance;

            var surface = new Surface
            {
                Bits = Marshal.AllocHGlobal(glyph.RenderWidth * glyph.RenderHeight),
                Width = glyph.RenderWidth,
                Height = glyph.RenderHeight,
                Pitch = glyph.RenderWidth
            };

            var stuff = (byte*)surface.Bits;
            for (int i = 0; i < surface.Width * surface.Height; i++)
                *stuff++ = 0;

            glyph.RenderTo(surface);

            return surface;
        }

        private CharacterInfo UpdateTexture(Surface surface, ref uint[] rawCharMap, ref int posX, ref int posY, ref int lineOffset)
        {
            int width = surface.Width;
            int height = surface.Height;
            int len = width * height;
            byte[] data = new byte[len];

            // Get character bitmap
            Marshal.Copy(surface.Bits, data, 0, len);

            // Find a slot
            if (posX + width > SheetWidth)
            {
                posX = 0;
                posY += lineOffset;
                lineOffset = 0;
            }

            // Update lineoffset
            if (lineOffset < height)
            {
                lineOffset = height + 1;
            }

            // Copy char to sheet
            for (int y = 0; y < height; y++)
            {
                int destOffset = (y + posY) * SheetWidth + posX;
                int sourceOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    rawCharMap[destOffset + x] = (uint)((0xFFFFFF << 8) | data[sourceOffset + x]);
                }
            }

            // Generate character info
            CharacterInfo charInfo = new CharacterInfo()
            {
                left = (float)posX / SheetWidth,
                right = (float)(posX + width) / SheetWidth,
                top = (float)(posY - 1) / SheetHeight,
                bottom = (float)(posY + height) / SheetHeight,
                width = width,
                height = height,
            };

            // Update x
            posX += width + 1;

            // Give the memory back
            Marshal.FreeHGlobal(surface.Bits);
            return charInfo;
        }
    }
}