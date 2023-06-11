using NUnit.Framework;
using Ryujinx.Graphics.Gpu;
using Ryujinx.Graphics.Texture.Astc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Tests.Graphics
{
    public class AstcDecoderTests
    {
        [SetUp]
        public void SetupFixture()
        {

        } 

        [Test]
        public void _Test()
        {
            var inputData = GetTestData();
            var outputBuffer = new Memory<byte>();
            GraphicsConfig.EnableTextureRecompression = false;


            AstcDecoder.TryDecodeToRgba8P()

            AstcDecoder target = new AstcDecoder();
        }

        private ReadOnlyMemory<byte> GetTestData();
    }
}
