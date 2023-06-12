using Microsoft.VisualBasic;
using NUnit.Framework;
using Ryujinx.Graphics.Gpu;
using Ryujinx.Graphics.Texture.Astc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ryujinx.Tests.Graphics
{
    public class AstcDecoderTests
    {
        private string _workingDir;
        private string _testDataDir;

        [SetUp]
        public void SetupFixture()
        {
            _workingDir = TestContext.CurrentContext.TestDirectory;
            _testDataDir = Path.Join(_workingDir, "Graphics", "TestData");
        } 

        [Test]
        public void _Test()
        {
            GraphicsConfig.EnableTextureRecompression = false;

            var (original, encodedRef, decodedRef) = _getTestDataTupleFromShortname("kodim01");
            int blockWidth = 4;
            int blockHeight = 4;

            int texWidth = 768;
            int texHeight = 512;
            byte[] outputBuffer = Array.Empty<byte>();
            int depth = 1;
            int levels = 1;
            int layers = 1;

            _ = AstcDecoder.TryDecodeToRgba8P(original, blockWidth, blockHeight, texWidth, texHeight, depth, levels, layers, out outputBuffer);
            var outputPath = Path.Join(_testDataDir, "kodim01.l4x4.output.png");

            // Make sure we're clobbering the test output.
            if (File.Exists(outputPath)) 
                File.Delete(outputPath);
            File.WriteAllBytes(outputPath, outputBuffer);
            
            Assert.AreEqual(decodedRef, outputBuffer);

        }

        private (ReadOnlyMemory<byte>, ReadOnlyMemory<byte>, ReadOnlyMemory<byte>) _getTestDataTupleFromShortname(string shortName)
        {
            var original = _getFileDataFromPath($"{shortName}.png");
            // TODO: add brains for block sizes/etc
            var encodedRef = _getFileDataFromPath($"{shortName}.l4x4.astc");
            var decodedRef = _getFileDataFromPath($"{shortName}.l4x4.astc.png");

            return (original, encodedRef, decodedRef);
        }

        private ReadOnlyMemory<byte> _getFileDataFromPath(string relativeFilePath)
        {
            var fullPath = Path.Join(_testDataDir, relativeFilePath);
            return File.ReadAllBytes(fullPath);
        }


    }
}
