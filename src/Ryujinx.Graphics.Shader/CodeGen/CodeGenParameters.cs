using Ryujinx.Graphics.Shader.StructuredIr;
using Ryujinx.Graphics.Shader.Translation;

namespace Ryujinx.Graphics.Shader.CodeGen
{
    struct CodeGenParameters
    {
        public readonly AttributeUsage AttributeUsage;
        public readonly ShaderDefinitions Definitions;
        public readonly ShaderProperties Properties;
        public readonly HostCapabilities HostCapabilities;
        public readonly TargetApi TargetApi;

        public CodeGenParameters(
            AttributeUsage attributeUsage,
            ShaderDefinitions definitions,
            ShaderProperties properties,
            HostCapabilities hostCapabilities,
            TargetApi targetApi)
        {
            AttributeUsage = attributeUsage;
            Definitions = definitions;
            Properties = properties;
            HostCapabilities = hostCapabilities;
            TargetApi = targetApi;
        }
    }
}
