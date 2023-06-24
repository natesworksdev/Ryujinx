using Ryujinx.Graphics.Shader.IntermediateRepresentation;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Shader.Translation.Transforms
{
    static class TransformPasses
    {
        private ref struct Context
        {
            public HelperFunctionManager Hfm;
            public BasicBlock[] Blocks;
            public ResourceManager ResourceManager;
            public IGpuAccessor GpuAccessor;
            public TargetLanguage TargetLanguage;
            public ShaderStage Stage;
            public ref FeatureFlags UsedFeatures;
        }

        public static void RunPass(
            HelperFunctionManager hfm,
            BasicBlock[] blocks,
            ResourceManager resourceManager,
            IGpuAccessor gpuAccessor,
            TargetLanguage targetLanguage,
            ShaderStage stage,
            ref FeatureFlags usedFeatures)
        {
            Context context = new Context()
            {
                Hfm = hfm,
                Blocks = blocks,
                ResourceManager = resourceManager,
                GpuAccessor = gpuAccessor,
                TargetLanguage = targetLanguage,
                Stage = stage,
                UsedFeatures = ref usedFeatures
            };

            RunPass<DrawParametersReplace>(context);
            RunPass<ForcePreciseEnable>(context);
            RunPass<VectorComponentSelect>(context);
            RunPass<TexturePass>(context);
            RunPass<SharedStoreSmallIntCas>(context);
            RunPass<SharedAtomicSignedCas>(context);
        }

        private static void RunPass<T>(Context context) where T : ITransformPass
        {
            if (!T.IsEnabled(context.GpuAccessor, context.Stage, context.TargetLanguage, context.UsedFeatures))
            {
                return;
            }

            HelperFunctionManager hfm = context.Hfm;
            ResourceManager resourceManager = context.ResourceManager;
            IGpuAccessor gpuAccessor = context.GpuAccessor;
            ShaderStage stage = context.Stage;
            ref FeatureFlags usedFeatures = ref context.UsedFeatures;

            for (int blkIndex = 0; blkIndex < context.Blocks.Length; blkIndex++)
            {
                BasicBlock block = context.Blocks[blkIndex];

                for (LinkedListNode<INode> node = block.Operations.First; node != null; node = node.Next)
                {
                    if (node.Value is not Operation)
                    {
                        continue;
                    }

                    node = T.RunPass(hfm, node, resourceManager, gpuAccessor, stage, ref usedFeatures);
                }
            }
        }
    }
}