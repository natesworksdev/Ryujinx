using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.Translation.Transforms
{
    interface ITransformPass
    {
        abstract static bool IsEnabled(IGpuAccessor gpuAccessor, ShaderStage stage, TargetLanguage targetLanguage, FeatureFlags usedFeatures);
        abstract static LinkedListNode<INode> RunPass(
            HelperFunctionManager hfm,
            LinkedListNode<INode> node,
            ResourceManager resourceManager,
            IGpuAccessor gpuAccessor,
            ShaderStage stage,
            ref FeatureFlags usedFeatures);
    }
}