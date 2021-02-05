using Ryujinx.Cpu.Tracking;
using Ryujinx.Graphics.Texture;
using Ryujinx.Memory.Range;
using System;
using System.Collections.Generic;

namespace Ryujinx.Graphics.Gpu.Image
{
    /// <summary>
    /// A tracking handle for a texture group, which represents a range of views in a storage texture.
    /// Retains a list of overlapping texture views, a modified flag, and tracking for each
    /// CPU VA range that the views cover.
    /// </summary>
    class TextureGroupHandle
    {
        /// <summary>
        /// The byte offset from the start of the storage of this handle.
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// The size in bytes covered by this handle.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// The textures which this handle overlaps with.
        /// </summary>
        public List<Texture> Overlaps { get; }

        /// <summary>
        /// The CPU memory tracking handles that cover this handle.
        /// </summary>
        public CpuRegionHandle[] Handles { get; }

        /// <summary>
        /// True if a texture overlapping this handle has been modified. Is set false when the flush action is called.
        /// </summary>
        public bool Modified { get; set; }

        /// <summary>
        /// Create a new texture group handle, representing a range of views in a storage texture.
        /// </summary>
        /// <param name="group">The TextureGroup that the handle belongs to</param>
        /// <param name="offset">The byte offset from the start of the storage of the handle</param>
        /// <param name="size">The size in bytes covered by the handle</param>
        /// <param name="views">All views of the storage texture, used to calculate overlaps</param>
        /// <param name="handles">The memory tracking handles that represent cover the handle</param>
        public TextureGroupHandle(TextureGroup group, int offset, ulong size, List<Texture> views, CpuRegionHandle[] handles)
        {
            Offset = offset;
            Size = (int)size;
            Overlaps = new List<Texture>();

            if (views != null)
            {
                RecalculateOverlaps(group, views);
            }

            Handles = handles;
        }

        /// <summary>
        /// Calculate a list of which views overlap this handle.
        /// </summary>
        /// <param name="group">The parent texture group, used to find a view's base CPU VA offset</param>
        /// <param name="views">The list of views to search for overlaps</param>
        public void RecalculateOverlaps(TextureGroup group, List<Texture> views)
        {
            // Overlaps can be accessed from the memory tracking signal handler, so access must be atomic.
            lock (Overlaps)
            {
                int endOffset = Offset + Size;

                Overlaps.Clear();

                foreach (Texture view in views)
                {
                    int viewOffset = group.FindOffset(view);
                    if (viewOffset < endOffset && Offset < viewOffset + (int)view.Size)
                    {
                        Overlaps.Add(view);
                    }
                }
            }
        }
    }

    /// <summary>
    /// A texture group represents a group of textures that belong to the same storage.
    /// When views are created, this class will track memory accesses for them separately.
    /// The group iteratively adds more granular tracking as views of different kinds are added.
    /// Note that a texture group can be absorbed into another when it becomes a view parent.
    /// </summary>
    class TextureGroup : IDisposable
    {
        /// <summary>
        /// The storage texture associated with this group.
        /// </summary>
        public Texture Storage { get; }

        private GpuContext _context;

        private int[] _allOffsets;
        private bool _is3D;
        private bool _hasMipViews;
        private bool _hasLayerViews;
        private int _layers;
        private int _levels;

        private MultiRange _textureRange => Storage.Range;

        /// <summary>
        /// The views list from the storage texture.
        /// </summary>
        private List<Texture> _views;
        private TextureGroupHandle[] _handles;
        private bool[] _loadNeeded;

        /// <summary>
        /// Create a new texture group.
        /// </summary>
        /// <param name="context">GPU context that the texture group belongs to</param>
        /// <param name="storage">The storage texture for this group</param>
        public TextureGroup(GpuContext context, Texture storage)
        {
            Storage = storage;
            _context = context;

            _is3D = storage.Info.Target == GAL.Target.Texture3D;
            _layers = storage.Info.GetSlices();
            _levels = storage.Info.Levels;
        }

        /// <summary>
        /// Initialize a new texture group's dirty regions and offsets.
        /// </summary>
        /// <param name="hasLayerViews">True if the storage will have layer views</param>
        /// <param name="hasMipViews">True if the storage will have mip views</param>
        public void Initialize(ref SizeInfo size, bool hasLayerViews, bool hasMipViews)
        {
            _allOffsets = size.AllOffsets;
            _hasLayerViews = hasLayerViews;
            _hasMipViews = hasMipViews;

            RecalculateHandleRegions();
        }

        /// <summary>
        /// Consume the dirty flags for a given texture. The state is shared between views of the same layers and levels.
        /// </summary>
        /// <param name="texture">The texture being used</param>
        /// <returns>True if a flag was dirty, false otherwise.</returns>
        public bool ConsumeDirty(Texture texture)
        {
            (int baseHandle, int regionCount) = EvaluateRelevantHandles(texture);

            bool dirty = false;
            for (int i = 0; i < regionCount; i++)
            {
                TextureGroupHandle group = _handles[baseHandle + i];

                foreach (CpuRegionHandle handle in group.Handles)
                {
                    if (handle.Dirty)
                    {
                        handle.Reprotect();
                        dirty |= true;
                    }
                }
            }

            return dirty;
        }

        /// <summary>
        /// Synchronize memory for a given texture. 
        /// If overlapping tracking handles are dirty, fully or partially synchronize the texture data.
        /// </summary>
        /// <param name="texture">The texture being used</param>
        public void SynchronizeMemory(Texture texture)
        {
            (int baseHandle, int regionCount) = EvaluateRelevantHandles(texture);

            // TODO: Partial reload. Right now it will reload the entire texture that was requested.
            // We want to only reload parts of the texture that have not been modified.

            bool dirty = false;
            bool anyModified = false;
            bool notDirty = false;
            for (int i = 0; i < regionCount; i++)
            {
                TextureGroupHandle group = _handles[baseHandle + i];

                bool modified = group.Modified;

                bool handleDirty = false;

                foreach (CpuRegionHandle handle in group.Handles)
                {
                    if (handle.Dirty)
                    {
                        handle.Reprotect();
                        dirty |= true;
                        handleDirty |= true;
                    }
                    else
                    {
                        anyModified |= modified;
                        notDirty |= true;
                    }
                }

                _loadNeeded[baseHandle + i] = handleDirty;
            }

            if (dirty)
            {
                if (_handles.Length > 1 && anyModified)
                {
                    // Partial texture invalidation. Only update the layers/levels with dirty flags of the storage.

                    SynchronizePartial(baseHandle, regionCount);
                }
                else
                {
                    // Full texture invalidation.

                    texture.SynchronizeFull();
                }
            }
        }

        /// <summary>
        /// Synchronize part of the storage texture, represented by a given range of handles.
        /// Only handles marked by the _loadNeeded array will be synchronized.
        /// </summary>
        /// <param name="baseRegion"></param>
        /// <param name="regionCount"></param>
        private void SynchronizePartial(int baseRegion, int regionCount)
        {
            ReadOnlySpan<byte> fullData = _context.PhysicalMemory.GetSpan(Storage.Range);

            for (int i = 0; i < regionCount; i++)
            {
                if (_loadNeeded[baseRegion + i])
                {
                    var info = GetHandleInformation(baseRegion + i);
                    int offsetIndex = info.index;

                    // Only one of these will be greater than 1, as partial sync is only called when there are sub-image views.
                    for (int layer = 0; layer < info.layers; layer++)
                    {
                        for (int level = 0; level < info.levels; level++)
                        {
                            int offset = _allOffsets[offsetIndex];
                            int endOffset = (offsetIndex + 1 == _allOffsets.Length) ? (int)Storage.Size : _allOffsets[offsetIndex + 1];
                            int size = endOffset - offset;

                            ReadOnlySpan<byte> data = fullData.Slice(offset, size);

                            data = Storage.ConvertToHostCompatibleFormat(data, info.baseLevel, true);

                            Storage.SetData(data, info.baseLayer, info.baseLevel);

                            offsetIndex++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Signal that a texture in the group has been modified by the GPU.
        /// </summary>
        /// <param name="texture">The texture that has been modified.</param>
        public void SignalModified(Texture texture)
        {
            (int baseHandle, int regionCount) = EvaluateRelevantHandles(texture);

            for (int i = 0; i < regionCount; i++)
            {
                TextureGroupHandle group = _handles[baseHandle + i];

                group.Modified = true;

                foreach (CpuRegionHandle handle in group.Handles)
                {
                    handle.RegisterAction((address, size) => FlushAction(group, address, size));
                }
            }
        }

        /// <summary>
        /// Evaluate the range of tracking handles which a view texture overlaps with.
        /// </summary>
        /// <param name="texture">The texture to get handles for</param>
        /// <returns></returns>
        private (int baseHandle, int regionCount) EvaluateRelevantHandles(Texture texture)
        {
            if (texture == Storage || !(_hasMipViews || _hasLayerViews))
            {
                return (0, _handles.Length);
            }

            int targetLayerHandles = _hasLayerViews ? texture.Info.GetSlices() : 1;
            int targetLevelHandles = _hasMipViews ? texture.Info.Levels : 1;

            if (_is3D)
            {
                // Mipmaps come after all layers.
                int layerHandles = _hasLayerViews ? _layers : 1;

                return (texture.FirstLayer + (texture.FirstLevel) * layerHandles, targetLayerHandles + (targetLevelHandles - 1) * layerHandles);
            }
            else
            {
                // Layers come after all mipmaps.
                int levelHandles = _hasMipViews ? _levels : 1;

                return (texture.FirstLevel + (texture.FirstLayer) * levelHandles, targetLevelHandles + (targetLayerHandles - 1) * levelHandles);
            }
        }

        /// <summary>
        /// Get view information for a single tracking handle.
        /// </summary>
        /// <param name="handleIndex">The index of the handle</param>
        /// <returns>The layers and levels that the handle covers, and its index in the offsets array</returns>
        private (int baseLayer, int baseLevel, int levels, int layers, int index) GetHandleInformation(int handleIndex)
        {
            int baseLayer;
            int baseLevel;
            int levels = _hasMipViews ? 1 : _levels;
            int layers = _hasLayerViews ? 1 : _layers;
            int index;

            if (_is3D)
            {
                baseLayer = _hasLayerViews ? handleIndex % _layers : 0;
                baseLevel = _hasLayerViews ? handleIndex / _layers : handleIndex;
                index = baseLayer + baseLevel * _layers;
            }
            else
            {
                baseLevel = _hasMipViews ? handleIndex % _levels : 0;
                baseLayer = _hasMipViews ? handleIndex / _levels : handleIndex;
                index = baseLevel + baseLayer * _levels;
            }

            return (baseLayer, baseLevel, levels, layers, index);
        }

        /// <summary>
        /// Find the byte offset of a given texture relative to the storage.
        /// </summary>
        /// <param name="texture">The texture to locate</param>
        /// <returns>The offset of the texture in bytes</returns>
        public int FindOffset(Texture texture)
        {
            if (_is3D)
            {
                return _allOffsets[texture.FirstLevel + texture.FirstLayer * _levels];
            }
            else
            {
                return _allOffsets[texture.FirstLayer + texture.FirstLevel * _layers];
            }
        }

        /// <summary>
        /// The action to perform when a memory tracking handle is flipped to dirty.
        /// This notifies overlapping textures that the memory needs to be synchronized.
        /// </summary>
        /// <param name="groupHandle">The handle that a dirty flag was set on</param>
        private void DirtyAction(TextureGroupHandle groupHandle)
        {
            // Notify all textures that belong to this handle.

            Storage.SignalGroupDirty();

            lock (groupHandle.Overlaps)
            {
                foreach (Texture overlap in groupHandle.Overlaps)
                {
                    overlap.SignalGroupDirty();
                }
            }
        }

        /// <summary>
        /// Generate a CpuRegionHandle for a given address and size range in CPU VA.
        /// </summary>
        /// <param name="address">The start address of the tracked region</param>
        /// <param name="size">The size of the tracked region</param>
        /// <returns></returns>
        private CpuRegionHandle GenerateHandle(ulong address, ulong size)
        {
            return _context.PhysicalMemory.BeginTracking(address, size);
        }

        /// <summary>
        /// Generate a TextureGroupHandle covering a specified range of views.
        /// </summary>
        /// <param name="viewStart">The start view of the handle</param>
        /// <param name="views">The number of views to cover</param>
        /// <returns>A TextureGroupHandle covering the given views</returns>
        private TextureGroupHandle GenerateHandles(int viewStart, int views)
        {
            int offset = _allOffsets[viewStart];
            int endOffset = (viewStart + views == _allOffsets.Length) ? (int)Storage.Size : _allOffsets[viewStart + views];
            int size = endOffset - offset;

            var result = new List<CpuRegionHandle>();

            for (int i = 0; i < _textureRange.Count; i++)
            {
                MemoryRange item = _textureRange.GetSubRange(i);
                int subRangeSize = (int)item.Size;

                int sliceStart = Math.Clamp(offset, 0, subRangeSize);
                int sliceEnd = Math.Clamp(endOffset, 0, subRangeSize);

                if (sliceStart != sliceEnd)
                {
                    result.Add(GenerateHandle(item.Address + (ulong)sliceStart, (ulong)(sliceEnd - sliceStart)));
                }

                offset -= subRangeSize;
                endOffset -= subRangeSize;

                if (endOffset <= 0)
                {
                    break;
                }
            }

            var groupHandle = new TextureGroupHandle(this, _allOffsets[viewStart], (ulong)size, _views, result.ToArray());

            foreach (CpuRegionHandle handle in result)
            {
                handle.RegisterDirtyEvent(() => DirtyAction(groupHandle));
            }

            return groupHandle;
        }

        /// <summary>
        /// Update the views in this texture group, rebuilding the memory tracking if required.
        /// </summary>
        /// <param name="views">The views list of the storage texture</param>
        public void UpdateViews(List<Texture> views)
        {
            // This is saved to calculate overlapping views for each handle.
            _views = views;

            bool layerViews = _hasLayerViews;
            bool mipViews = _hasMipViews;
            bool regionsRebuilt = false;

            if (!(layerViews && mipViews))
            {
                foreach (Texture view in views)
                {
                    if (view.Info.GetSlices() < _layers)
                    {
                        layerViews = true;
                    }

                    if (view.Info.Levels < _levels)
                    {
                        mipViews = true;
                    }
                }

                if (layerViews != _hasLayerViews || mipViews != _hasMipViews)
                {
                    _hasLayerViews = layerViews;
                    _hasMipViews = mipViews;

                    RecalculateHandleRegions();
                    regionsRebuilt = true;
                }
            }

            if (!regionsRebuilt)
            {
                foreach (TextureGroupHandle handle in _handles)
                {
                    handle.RecalculateOverlaps(this, views);
                }
            }

            Storage.SignalGroupDirty();
            foreach (Texture texture in views)
            {
                texture.SignalGroupDirty();
            }
        }

        /// <summary>
        /// Inherit handle state from an old set of handles, such as modified and dirty flags.
        /// </summary>
        /// <param name="oldHandles">The set of handles to inherit state from</param>
        /// <param name="handles">The set of handles inheriting the state</param>
        private void InheritHandles(TextureGroupHandle[] oldHandles, TextureGroupHandle[] handles)
        {
            foreach (var group in handles)
            {
                foreach (var handle in group.Handles)
                {
                    bool dirty = false;

                    foreach (var oldGroup in oldHandles)
                    {
                        foreach (var oldHandle in oldGroup.Handles)
                        {
                            if (handle.OverlapsWith(oldHandle.Address, oldHandle.Size))
                            {
                                dirty |= oldHandle.Dirty;
                                group.Modified |= oldGroup.Modified;
                            }
                        }
                    }

                    if (dirty && !handle.Dirty)
                    { 
                        handle.Reprotect(true);
                    }

                    if (group.Modified)
                    {
                        handle.RegisterAction((address, size) => FlushAction(group, address, size));
                    }
                }
            }
        }

        /// <summary>
        /// Inherit state from another texture group.
        /// </summary>
        /// <param name="other">The texture group to inherit from</param>
        public void Inherit(TextureGroup other)
        {
            InheritHandles(other._handles, _handles);
        }

        /// <summary>
        /// Replace the current handles with the new handles. It is assumed that the new handles start dirty.
        /// The dirty flags from the previous handles will be kept.
        /// </summary>
        /// <param name="handles">The handles to replace the current handles with</param>
        private void ReplaceHandles(TextureGroupHandle[] handles)
        {
            if (_handles != null)
            {
                // When replacing handles, they should start as non-dirty.

                foreach (TextureGroupHandle groupHandle in handles)
                {
                    foreach (CpuRegionHandle handle in groupHandle.Handles)
                    {
                        handle.Reprotect();
                    }
                }

                InheritHandles(_handles, handles);

                foreach (var oldGroup in _handles)
                {
                    foreach (var oldHandle in oldGroup.Handles)
                    {
                        oldHandle.Dispose();
                    }
                }
            }

            _handles = handles;
            _loadNeeded = new bool[_handles.Length];
        }

        /// <summary>
        /// Recalculate handle regions for this texture group, and inherit existing state into the new handles.
        /// </summary>
        private void RecalculateHandleRegions()
        {
            TextureGroupHandle[] handles;

            if (!(_hasMipViews || _hasLayerViews))
            {
                // Single dirty region.
                var cpuRegionHandles = new CpuRegionHandle[_textureRange.Count];

                for (int i = 0; i < _textureRange.Count; i++)
                {
                    var currentRange = _textureRange.GetSubRange(i);
                    cpuRegionHandles[i] = GenerateHandle(currentRange.Address, currentRange.Size);
                }

                var groupHandle = new TextureGroupHandle(this, 0, Storage.Size, _views, cpuRegionHandles);

                foreach (CpuRegionHandle handle in cpuRegionHandles)
                {
                    handle.RegisterDirtyEvent(() => DirtyAction(groupHandle));
                }

                handles = new TextureGroupHandle[] { groupHandle };
            }
            else
            {
                // Get mips for the host texture.

                int layerHandles = _hasLayerViews ? _layers : 1;
                int levelHandles = _hasMipViews ? _levels : 1;

                handles = new TextureGroupHandle[layerHandles * levelHandles];
                int handleIndex = 0;

                if (_is3D)
                {
                    for (int i = 0; i < levelHandles; i++)
                    {
                        for (int j = 0; j < layerHandles; j++)
                        {
                            int viewStart = j + i * _layers;
                            int views = (_hasLayerViews ? 1 : _layers) * (_hasMipViews ? 1 : _levels);

                            handles[handleIndex++] = GenerateHandles(viewStart, views);
                        }
                    }
                } 
                else
                {
                    for (int i = 0; i < layerHandles; i++)
                    {
                        for (int j = 0; j < levelHandles; j++)
                        {
                            int viewStart = j + i * _levels;
                            int views = (_hasLayerViews ? 1 : _layers) * (_hasMipViews ? 1 : _levels);

                            handles[handleIndex++] = GenerateHandles(viewStart, views);
                        }
                    }
                }
            }

            ReplaceHandles(handles);
        }

        /// <summary>
        /// A flush has been requested on a tracked region. Find an appropriate view to flush.
        /// </summary>
        /// <param name="handle">The handle this flush action is for</param>
        /// <param name="address">The address of the flushing memory access</param>
        /// <param name="address">The size of the flushing memory access</param>
        public void FlushAction(TextureGroupHandle handle, ulong address, ulong size)
        {
            Storage.ExternalFlush(address, size);

            lock (handle.Overlaps)
            {
                foreach (Texture overlap in handle.Overlaps)
                {
                    overlap.ExternalFlush(address, size);
                }
            }

            handle.Modified = false;
        }

        /// <summary>
        /// Dispose this texture group, disposing all related memory tracking handles.
        /// </summary>
        public void Dispose()
        {
            foreach (TextureGroupHandle group in _handles)
            {
                foreach (CpuRegionHandle handle in group.Handles)
                {
                    handle.Dispose();
                }
            }
        }
    }
}
