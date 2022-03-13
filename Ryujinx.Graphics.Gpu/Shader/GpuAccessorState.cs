namespace Ryujinx.Graphics.Gpu.Shader
{
    /// <summary>
    /// State used by the <see cref="GpuAccessor"/>.
    /// </summary>
    class GpuAccessorState
    {
        public readonly GpuChannelPoolState PoolState;

        public readonly GpuChannelComputeState ComputeState;

        public readonly GpuChannelGraphicsState GraphicsState;

        public readonly ShaderSpecializationState SpecializationState;

        /// <summary>
        /// Transform feedback information, if the shader uses transform feedback. Otherwise, should be null.
        /// </summary>
        public readonly TransformFeedbackDescriptor[] TransformFeedbackDescriptors;

        public readonly ResourceCounts ResourceCounts;

        public GpuAccessorState(
            GpuChannelPoolState poolState,
            GpuChannelComputeState computeState,
            GpuChannelGraphicsState graphicsState,
            ShaderSpecializationState specializationState,
            TransformFeedbackDescriptor[] transformFeedbackDescriptors = null)
        {
            PoolState = poolState;
            GraphicsState = graphicsState;
            ComputeState = computeState;
            SpecializationState = specializationState;
            TransformFeedbackDescriptors = transformFeedbackDescriptors;
            ResourceCounts = new ResourceCounts();
        }
    }
}