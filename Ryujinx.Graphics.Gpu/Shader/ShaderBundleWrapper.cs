using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Graphics.Gpu.Shader
{


    class ShaderBundleWrapper
    {
        public ShaderBundle shaderBundle { get; set; }
        public ulong shaderAddress { get; set; }
        public ShaderAddresses shaderAddresses { get; set; }

        public ShaderBundleWrapper(ShaderBundle bundle, ulong address)
        {
            this.shaderBundle = bundle;
            this.shaderAddress = address;
        }

        public ShaderBundleWrapper(ShaderBundle bundle, ShaderAddresses addresses)
        {
            this.shaderBundle = bundle;
            this.shaderAddresses = addresses;
        }
    }
}
