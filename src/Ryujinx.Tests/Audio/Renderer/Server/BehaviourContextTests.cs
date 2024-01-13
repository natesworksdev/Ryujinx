using NUnit.Framework;
using Ryujinx.Audio.Renderer.Server;

namespace Ryujinx.Tests.Audio.Renderer.Server
{
    public class BehaviourContextTests
    {
        [Test]
        public void TestCheckFeature()
        {
            int latestRevision = BehaviourContext.BaseRevisionMagic + BehaviourContext.LastRevision;
            int previousRevision = BehaviourContext.BaseRevisionMagic + (BehaviourContext.LastRevision - 1);
            int invalidRevision = BehaviourContext.BaseRevisionMagic + (BehaviourContext.LastRevision + 1);

            Assert.That(BehaviourContext.CheckFeatureSupported(latestRevision, latestRevision), Is.True);
            Assert.That(BehaviourContext.CheckFeatureSupported(previousRevision, latestRevision), Is.False);
            Assert.That(BehaviourContext.CheckFeatureSupported(latestRevision, previousRevision), Is.True);
            // In case we get an invalid revision, this is supposed to auto default to REV1 internally.. idk what the hell Nintendo was thinking here..
            Assert.That(BehaviourContext.CheckFeatureSupported(invalidRevision, latestRevision), Is.True);
        }

        [Test]
        public void TestsMemoryPoolForceMappingEnabled()
        {
            BehaviourContext behaviourContext = new();

            behaviourContext.SetUserRevision(BehaviourContext.BaseRevisionMagic + BehaviourContext.Revision1);

            Assert.That(behaviourContext.IsMemoryPoolForceMappingEnabled(), Is.False);

            behaviourContext.UpdateFlags(0x1);

            Assert.That(behaviourContext.IsMemoryPoolForceMappingEnabled(), Is.True);
        }

        [Test]
        public void TestRevision1()
        {
            BehaviourContext behaviourContext = new();

            behaviourContext.SetUserRevision(BehaviourContext.BaseRevisionMagic + BehaviourContext.Revision1);

            Assert.That(behaviourContext.IsAdpcmLoopContextBugFixed(), Is.False);
            Assert.That(behaviourContext.IsSplitterSupported(), Is.False);
            Assert.That(behaviourContext.IsLongSizePreDelaySupported(), Is.False);
            Assert.That(behaviourContext.IsAudioUsbDeviceOutputSupported(), Is.False);
            Assert.That(behaviourContext.IsFlushVoiceWaveBuffersSupported(), Is.False);
            Assert.That(behaviourContext.IsSplitterBugFixed(), Is.False);
            Assert.That(behaviourContext.IsElapsedFrameCountSupported(), Is.False);
            Assert.That(behaviourContext.IsDecodingBehaviourFlagSupported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterEffectStateClearBugFixed(), Is.False);
            Assert.That(behaviourContext.IsMixInParameterDirtyOnlyUpdateSupported(), Is.False);
            Assert.That(behaviourContext.IsWaveBufferVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsEffectInfoVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterGroupedOptimizationSupported(), Is.False);

            Assert.That(0.70f, Is.EqualTo(behaviourContext.GetAudioRendererProcessingTimeLimit()));
            Assert.That(1, Is.EqualTo(behaviourContext.GetCommandProcessingTimeEstimatorVersion()));
            Assert.That(1, Is.EqualTo(behaviourContext.GetPerformanceMetricsDataFormat()));
        }

        [Test]
        public void TestRevision2()
        {
            BehaviourContext behaviourContext = new();

            behaviourContext.SetUserRevision(BehaviourContext.BaseRevisionMagic + BehaviourContext.Revision2);

            Assert.That(behaviourContext.IsAdpcmLoopContextBugFixed(), Is.True);
            Assert.That(behaviourContext.IsSplitterSupported(), Is.True);
            Assert.That(behaviourContext.IsLongSizePreDelaySupported(), Is.False);
            Assert.That(behaviourContext.IsAudioUsbDeviceOutputSupported(), Is.False);
            Assert.That(behaviourContext.IsFlushVoiceWaveBuffersSupported(), Is.False);
            Assert.That(behaviourContext.IsSplitterBugFixed(), Is.False);
            Assert.That(behaviourContext.IsElapsedFrameCountSupported(), Is.False);
            Assert.That(behaviourContext.IsDecodingBehaviourFlagSupported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterEffectStateClearBugFixed(), Is.False);
            Assert.That(behaviourContext.IsMixInParameterDirtyOnlyUpdateSupported(), Is.False);
            Assert.That(behaviourContext.IsWaveBufferVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsEffectInfoVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterGroupedOptimizationSupported(), Is.False);

            Assert.That(0.70f, Is.EqualTo(behaviourContext.GetAudioRendererProcessingTimeLimit()));
            Assert.That(1, Is.EqualTo(behaviourContext.GetCommandProcessingTimeEstimatorVersion()));
            Assert.That(1, Is.EqualTo(behaviourContext.GetPerformanceMetricsDataFormat()));
        }

        [Test]
        public void TestRevision3()
        {
            BehaviourContext behaviourContext = new();

            behaviourContext.SetUserRevision(BehaviourContext.BaseRevisionMagic + BehaviourContext.Revision3);

            Assert.That(behaviourContext.IsAdpcmLoopContextBugFixed(), Is.True);
            Assert.That(behaviourContext.IsSplitterSupported(), Is.True);
            Assert.That(behaviourContext.IsLongSizePreDelaySupported(), Is.True);
            Assert.That(behaviourContext.IsAudioUsbDeviceOutputSupported(), Is.False);
            Assert.That(behaviourContext.IsFlushVoiceWaveBuffersSupported(), Is.False);
            Assert.That(behaviourContext.IsSplitterBugFixed(), Is.False);
            Assert.That(behaviourContext.IsElapsedFrameCountSupported(), Is.False);
            Assert.That(behaviourContext.IsDecodingBehaviourFlagSupported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterEffectStateClearBugFixed(), Is.False);
            Assert.That(behaviourContext.IsMixInParameterDirtyOnlyUpdateSupported(), Is.False);
            Assert.That(behaviourContext.IsWaveBufferVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsEffectInfoVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterGroupedOptimizationSupported(), Is.False);

            Assert.That(0.70f, Is.EqualTo(behaviourContext.GetAudioRendererProcessingTimeLimit()));
            Assert.That(1, Is.EqualTo(behaviourContext.GetCommandProcessingTimeEstimatorVersion()));
            Assert.That(1, Is.EqualTo(behaviourContext.GetPerformanceMetricsDataFormat()));
        }

        [Test]
        public void TestRevision4()
        {
            BehaviourContext behaviourContext = new();

            behaviourContext.SetUserRevision(BehaviourContext.BaseRevisionMagic + BehaviourContext.Revision4);

            Assert.That(behaviourContext.IsAdpcmLoopContextBugFixed(), Is.True);
            Assert.That(behaviourContext.IsSplitterSupported(), Is.True);
            Assert.That(behaviourContext.IsLongSizePreDelaySupported(), Is.True);
            Assert.That(behaviourContext.IsAudioUsbDeviceOutputSupported(), Is.True);
            Assert.That(behaviourContext.IsFlushVoiceWaveBuffersSupported(), Is.False);
            Assert.That(behaviourContext.IsSplitterBugFixed(), Is.False);
            Assert.That(behaviourContext.IsElapsedFrameCountSupported(), Is.False);
            Assert.That(behaviourContext.IsDecodingBehaviourFlagSupported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterEffectStateClearBugFixed(), Is.False);
            Assert.That(behaviourContext.IsMixInParameterDirtyOnlyUpdateSupported(), Is.False);
            Assert.That(behaviourContext.IsWaveBufferVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsEffectInfoVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterGroupedOptimizationSupported(), Is.False);

            Assert.That(0.75f, Is.EqualTo(behaviourContext.GetAudioRendererProcessingTimeLimit()));
            Assert.That(1, Is.EqualTo(behaviourContext.GetCommandProcessingTimeEstimatorVersion()));
            Assert.That(1, Is.EqualTo(behaviourContext.GetPerformanceMetricsDataFormat()));
        }

        [Test]
        public void TestRevision5()
        {
            BehaviourContext behaviourContext = new();

            behaviourContext.SetUserRevision(BehaviourContext.BaseRevisionMagic + BehaviourContext.Revision5);

            Assert.That(behaviourContext.IsAdpcmLoopContextBugFixed(), Is.True);
            Assert.That(behaviourContext.IsSplitterSupported(), Is.True);
            Assert.That(behaviourContext.IsLongSizePreDelaySupported(), Is.True);
            Assert.That(behaviourContext.IsAudioUsbDeviceOutputSupported(), Is.True);
            Assert.That(behaviourContext.IsFlushVoiceWaveBuffersSupported(), Is.True);
            Assert.That(behaviourContext.IsSplitterBugFixed(), Is.True);
            Assert.That(behaviourContext.IsElapsedFrameCountSupported(), Is.True);
            Assert.That(behaviourContext.IsDecodingBehaviourFlagSupported(), Is.True);
            Assert.That(behaviourContext.IsBiquadFilterEffectStateClearBugFixed(), Is.False);
            Assert.That(behaviourContext.IsMixInParameterDirtyOnlyUpdateSupported(), Is.False);
            Assert.That(behaviourContext.IsWaveBufferVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsEffectInfoVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterGroupedOptimizationSupported(), Is.False);

            Assert.That(0.80f, Is.EqualTo(behaviourContext.GetAudioRendererProcessingTimeLimit()));
            Assert.That(2, Is.EqualTo(behaviourContext.GetCommandProcessingTimeEstimatorVersion()));
            Assert.That(2, Is.EqualTo(behaviourContext.GetPerformanceMetricsDataFormat()));
        }

        [Test]
        public void TestRevision6()
        {
            BehaviourContext behaviourContext = new();

            behaviourContext.SetUserRevision(BehaviourContext.BaseRevisionMagic + BehaviourContext.Revision6);

            Assert.That(behaviourContext.IsAdpcmLoopContextBugFixed(), Is.True);
            Assert.That(behaviourContext.IsSplitterSupported(), Is.True);
            Assert.That(behaviourContext.IsLongSizePreDelaySupported(), Is.True);
            Assert.That(behaviourContext.IsAudioUsbDeviceOutputSupported(), Is.True);
            Assert.That(behaviourContext.IsFlushVoiceWaveBuffersSupported(), Is.True);
            Assert.That(behaviourContext.IsSplitterBugFixed(), Is.True);
            Assert.That(behaviourContext.IsElapsedFrameCountSupported(), Is.True);
            Assert.That(behaviourContext.IsDecodingBehaviourFlagSupported(), Is.True);
            Assert.That(behaviourContext.IsBiquadFilterEffectStateClearBugFixed(), Is.True);
            Assert.That(behaviourContext.IsMixInParameterDirtyOnlyUpdateSupported(), Is.False);
            Assert.That(behaviourContext.IsWaveBufferVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsEffectInfoVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterGroupedOptimizationSupported(), Is.False);

            Assert.That(0.80f, Is.EqualTo(behaviourContext.GetAudioRendererProcessingTimeLimit()));
            Assert.That(2, Is.EqualTo(behaviourContext.GetCommandProcessingTimeEstimatorVersion()));
            Assert.That(2, Is.EqualTo(behaviourContext.GetPerformanceMetricsDataFormat()));
        }

        [Test]
        public void TestRevision7()
        {
            BehaviourContext behaviourContext = new();

            behaviourContext.SetUserRevision(BehaviourContext.BaseRevisionMagic + BehaviourContext.Revision7);

            Assert.That(behaviourContext.IsAdpcmLoopContextBugFixed(), Is.True);
            Assert.That(behaviourContext.IsSplitterSupported(), Is.True);
            Assert.That(behaviourContext.IsLongSizePreDelaySupported(), Is.True);
            Assert.That(behaviourContext.IsAudioUsbDeviceOutputSupported(), Is.True);
            Assert.That(behaviourContext.IsFlushVoiceWaveBuffersSupported(), Is.True);
            Assert.That(behaviourContext.IsSplitterBugFixed(), Is.True);
            Assert.That(behaviourContext.IsElapsedFrameCountSupported(), Is.True);
            Assert.That(behaviourContext.IsDecodingBehaviourFlagSupported(), Is.True);
            Assert.That(behaviourContext.IsBiquadFilterEffectStateClearBugFixed(), Is.True);
            Assert.That(behaviourContext.IsMixInParameterDirtyOnlyUpdateSupported(), Is.True);
            Assert.That(behaviourContext.IsWaveBufferVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsEffectInfoVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterGroupedOptimizationSupported(), Is.False);

            Assert.That(0.80f, Is.EqualTo(behaviourContext.GetAudioRendererProcessingTimeLimit()));
            Assert.That(2, Is.EqualTo(behaviourContext.GetCommandProcessingTimeEstimatorVersion()));
            Assert.That(2, Is.EqualTo(behaviourContext.GetPerformanceMetricsDataFormat()));
        }

        [Test]
        public void TestRevision8()
        {
            BehaviourContext behaviourContext = new();

            behaviourContext.SetUserRevision(BehaviourContext.BaseRevisionMagic + BehaviourContext.Revision8);

            Assert.That(behaviourContext.IsAdpcmLoopContextBugFixed(), Is.True);
            Assert.That(behaviourContext.IsSplitterSupported(), Is.True);
            Assert.That(behaviourContext.IsLongSizePreDelaySupported(), Is.True);
            Assert.That(behaviourContext.IsAudioUsbDeviceOutputSupported(), Is.True);
            Assert.That(behaviourContext.IsFlushVoiceWaveBuffersSupported(), Is.True);
            Assert.That(behaviourContext.IsSplitterBugFixed(), Is.True);
            Assert.That(behaviourContext.IsElapsedFrameCountSupported(), Is.True);
            Assert.That(behaviourContext.IsDecodingBehaviourFlagSupported(), Is.True);
            Assert.That(behaviourContext.IsBiquadFilterEffectStateClearBugFixed(), Is.True);
            Assert.That(behaviourContext.IsMixInParameterDirtyOnlyUpdateSupported(), Is.True);
            Assert.That(behaviourContext.IsWaveBufferVersion2Supported(), Is.True);
            Assert.That(behaviourContext.IsEffectInfoVersion2Supported(), Is.False);
            Assert.That(behaviourContext.IsBiquadFilterGroupedOptimizationSupported(), Is.False);

            Assert.That(0.80f, Is.EqualTo(behaviourContext.GetAudioRendererProcessingTimeLimit()));
            Assert.That(3, Is.EqualTo(behaviourContext.GetCommandProcessingTimeEstimatorVersion()));
            Assert.That(2, Is.EqualTo(behaviourContext.GetPerformanceMetricsDataFormat()));
        }

        [Test]
        public void TestRevision9()
        {
            BehaviourContext behaviourContext = new();

            behaviourContext.SetUserRevision(BehaviourContext.BaseRevisionMagic + BehaviourContext.Revision9);

            Assert.That(behaviourContext.IsAdpcmLoopContextBugFixed(), Is.True);
            Assert.That(behaviourContext.IsSplitterSupported(), Is.True);
            Assert.That(behaviourContext.IsLongSizePreDelaySupported(), Is.True);
            Assert.That(behaviourContext.IsAudioUsbDeviceOutputSupported(), Is.True);
            Assert.That(behaviourContext.IsFlushVoiceWaveBuffersSupported(), Is.True);
            Assert.That(behaviourContext.IsSplitterBugFixed(), Is.True);
            Assert.That(behaviourContext.IsElapsedFrameCountSupported(), Is.True);
            Assert.That(behaviourContext.IsDecodingBehaviourFlagSupported(), Is.True);
            Assert.That(behaviourContext.IsBiquadFilterEffectStateClearBugFixed(), Is.True);
            Assert.That(behaviourContext.IsMixInParameterDirtyOnlyUpdateSupported(), Is.True);
            Assert.That(behaviourContext.IsWaveBufferVersion2Supported(), Is.True);
            Assert.That(behaviourContext.IsEffectInfoVersion2Supported(), Is.True);
            Assert.That(behaviourContext.IsBiquadFilterGroupedOptimizationSupported(), Is.False);

            Assert.That(0.80f, Is.EqualTo(behaviourContext.GetAudioRendererProcessingTimeLimit()));
            Assert.That(3, Is.EqualTo(behaviourContext.GetCommandProcessingTimeEstimatorVersion()));
            Assert.That(2, Is.EqualTo(behaviourContext.GetPerformanceMetricsDataFormat()));
        }

        [Test]
        public void TestRevision10()
        {
            BehaviourContext behaviourContext = new();

            behaviourContext.SetUserRevision(BehaviourContext.BaseRevisionMagic + BehaviourContext.Revision10);

            Assert.That(behaviourContext.IsAdpcmLoopContextBugFixed(), Is.True);
            Assert.That(behaviourContext.IsSplitterSupported(), Is.True);
            Assert.That(behaviourContext.IsLongSizePreDelaySupported(), Is.True);
            Assert.That(behaviourContext.IsAudioUsbDeviceOutputSupported(), Is.True);
            Assert.That(behaviourContext.IsFlushVoiceWaveBuffersSupported(), Is.True);
            Assert.That(behaviourContext.IsSplitterBugFixed(), Is.True);
            Assert.That(behaviourContext.IsElapsedFrameCountSupported(), Is.True);
            Assert.That(behaviourContext.IsDecodingBehaviourFlagSupported(), Is.True);
            Assert.That(behaviourContext.IsBiquadFilterEffectStateClearBugFixed(), Is.True);
            Assert.That(behaviourContext.IsMixInParameterDirtyOnlyUpdateSupported(), Is.True);
            Assert.That(behaviourContext.IsWaveBufferVersion2Supported(), Is.True);
            Assert.That(behaviourContext.IsEffectInfoVersion2Supported(), Is.True);
            Assert.That(behaviourContext.IsBiquadFilterGroupedOptimizationSupported(), Is.True);

            Assert.That(0.80f, Is.EqualTo(behaviourContext.GetAudioRendererProcessingTimeLimit()));
            Assert.That(4, Is.EqualTo(behaviourContext.GetCommandProcessingTimeEstimatorVersion()));
            Assert.That(2, Is.EqualTo(behaviourContext.GetPerformanceMetricsDataFormat()));
        }
    }
}
