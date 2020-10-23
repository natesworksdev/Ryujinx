using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Image
{
    partial class Texture
    {
        /// <summary>
        /// Bindless texture state.
        /// </summary>
        private class BindlessState
        {
            /// <summary>
            /// Sampler pool currently in use by the bindless texture.
            /// </summary>
            public SamplerPool SamplerPool { get; set; }

            /// <summary>
            /// Handle manager of the respective texture pool.
            /// </summary>
            public BindlessHandleManager HandleManager { get; }

            /// <summary>
            /// Texture IDs on the pool where the texture is registered.
            /// </summary>
            public HashSet<int> TextureIds { get; }

            /// <summary>
            /// Sampler IDs currently in use by the texture, with host handles created.
            /// </summary>
            public Dictionary<Sampler, int> SamplerIds { get; }

            /// <summary>
            /// Creates a new instance of the bindless texture state.
            /// </summary>
            /// <param name="samplerPool">Sampler pool currently in use</param>
            /// <param name="handleManager">Handle manager of the texture pool where the texture is registered</param>
            public BindlessState(SamplerPool samplerPool, BindlessHandleManager handleManager)
            {
                SamplerPool = samplerPool;
                HandleManager = handleManager;
                TextureIds = new HashSet<int>();
                SamplerIds = new Dictionary<Sampler, int>();
            }
        }

        private readonly Dictionary<TexturePool, BindlessState> _bindlessState = new Dictionary<TexturePool, BindlessState>();

        private bool _isBindless;

        /// <summary>
        /// Forces the re-creation of the host bindless handles for the texture.
        /// </summary>
        public void BindlessReplace()
        {
            foreach (var kv in _bindlessState)
            {
                var texturePool = kv.Key;
                var state = kv.Value;

                foreach (int textureId in state.TextureIds)
                {
                    BindlessRegister(texturePool, state.SamplerPool, state.HandleManager, textureId);
                }
            }
        }

        /// <summary>
        /// Registers the texture for use as bindless texture, while also creating the host bindless handles
        /// and making them accessible to the shader as needed.
        /// </summary>
        /// <param name="texturePool">Texture pool where the texture is currently registered</param>
        /// <param name="samplerPool">Sampler pool currently in use</param>
        /// <param name="handleManager">Handle manager of the texture pool</param>
        /// <param name="textureId">ID of the texture on the texture pool</param>
        public void BindlessRegister(TexturePool texturePool, SamplerPool samplerPool, BindlessHandleManager handleManager, int textureId)
        {
            if (!_isBindless)
            {
                _isBindless = true;
                BindlessTrack();
            }

            if (!_bindlessState.TryGetValue(texturePool, out BindlessState state))
            {
                _bindlessState.Add(texturePool, state = new BindlessState(samplerPool, handleManager));
            }
            else
            {
                state.SamplerPool = samplerPool;
            }

            bool isNewTextureId = state.TextureIds.Add(textureId);
            samplerPool.AddDependant(this);

            for (int index = 0; index < samplerPool.SamplerIds.Count; index++)
            {
                int samplerId = samplerPool.SamplerIds[index];
                Sampler sampler = samplerPool.Get(samplerId);

                AddBindlessHandle(state, textureId, samplerId, sampler, isNewTextureId);
            }
        }

        /// <summary>
        /// Unregisters the bindless texture, making it inaccessible from shaders.
        /// </summary>
        /// <remarks>
        /// Only the specified ID will be unregistered, if you which to unregister all of them, use
        /// <see cref="BindlessUnregister()"/> instead.
        /// </remarks>
        /// <param name="texturePool">Texture pool where the texture is currently registered</param>
        /// <param name="textureId">ID of the texture on the texture pool</param>
        public void BindlessUnregister(TexturePool texturePool, int textureId)
        {
            if (_bindlessState.TryGetValue(texturePool, out BindlessState state))
            {
                state.TextureIds.Remove(textureId);

                foreach (var kv in state.SamplerIds)
                {
                    var sampler = kv.Key;
                    var samplerId = kv.Value;

                    state.HandleManager.RemoveBindlessHandle(textureId, samplerId);
                    if (state.TextureIds.Count == 0)
                    {
                        sampler.RemoveDependant(this);
                    }
                }

                if (state.TextureIds.Count == 0)
                {
                    _bindlessState.Remove(texturePool);
                }
            }
        }

        /// <summary>
        /// Unregisters the bindless texture, making it inaccessible from shaders.
        /// </summary>
        public void BindlessUnregister()
        {
            foreach (BindlessState state in _bindlessState.Values)
            {
                foreach (int textureId in state.TextureIds)
                {
                    foreach (var kv in state.SamplerIds)
                    {
                        var sampler = kv.Key;
                        var samplerId = kv.Value;

                        state.HandleManager.RemoveBindlessHandle(textureId, samplerId);
                        sampler.RemoveDependant(this);
                    }
                }

                state.SamplerPool.RemoveDependant(this);
            }

            _bindlessState.Clear();
        }

        /// <summary>
        /// Starts tracking reads and writes to the texture from the CPU.
        /// </summary>
        public void BindlessTrack()
        {
            _memoryTracking?.RegisterAction(ExternalFlush);
        }

        /// <summary>
        /// Notifies the texture that a new sampler was created.
        /// </summary>
        /// <param name="samplerId">ID of the sampler on the pool</param>
        /// <param name="sampler">New sampler</param>
        public void NotifySamplerCreation(int samplerId, Sampler sampler)
        {
            foreach (BindlessState state in _bindlessState.Values)
            {
                foreach (int textureId in state.TextureIds)
                {
                    AddBindlessHandle(state, textureId, samplerId, sampler, false);
                }
            }
        }

        /// <summary>
        /// Adds the host bindless handle to the handle table that can be accessed from shaders.
        /// </summary>
        /// <param name="state">Bindless state</param>
        /// <param name="textureId">ID of the texture on the pool</param>
        /// <param name="samplerId">ID of the sampler on the pool</param>
        /// <param name="sampler">Sampler</param>
        /// <param name="isNewTextureId">True if the texture ID was not registered before, false otherwise</param>
        private void AddBindlessHandle(BindlessState state, int textureId, int samplerId, Sampler sampler, bool isNewTextureId)
        {
            if (TextureValidation.IsSamplerCompatible(Info, sampler.Descriptor))
            {
                bool isNewSamplerId = state.SamplerIds.TryAdd(sampler, samplerId);

                if (isNewTextureId || isNewSamplerId)
                {
                    sampler.AddDependant(this);

                    state.HandleManager.AddBindlessHandle(
                        textureId,
                        samplerId,
                        HostTexture.GetBindlessHandle(sampler.HostSampler),
                        ScaleFactor);
                }
            }
        }

        /// <summary>
        /// Notifies the texture that a sampler it uses was disposed as is no longer valid.
        /// </summary>
        /// <param name="sampler">Disposed sampler</param>
        public void NotifySamplerDisposal(Sampler sampler)
        {
            foreach (BindlessState state in _bindlessState.Values)
            {
                foreach (int textureId in state.TextureIds)
                {
                    state.HandleManager.RemoveBindlessHandle(textureId, state.SamplerIds[sampler]);
                }

                state.SamplerIds.Remove(sampler);
            }
        }
    }
}
