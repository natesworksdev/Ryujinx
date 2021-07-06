using System;
using System.Diagnostics;

namespace Ryujinx.Graphics.Vulkan
{
    interface IAuto
    {
        void IncrementReferenceCount();
        void DecrementReferenceCount(int cbIndex);
        void DecrementReferenceCount();
    }

    interface IAutoPrivate : IAuto
    {
        void AddCommandBufferDependencies(CommandBufferScoped cbs);
    }

    class Auto<T> : IAutoPrivate, IDisposable where T : IDisposable
    {
        private int _referenceCount;
        private T _value;

        private readonly BitMap _cbOwnership;
        private readonly MultiFenceHolder _waitable;
        private readonly IAutoPrivate[] _referencedObjs;

        private bool _disposed;
        private bool _destroyed;

        public Auto(T value)
        {
            _referenceCount = 1;
            _value = value;
            _cbOwnership = new BitMap(CommandBufferPool.MaxCommandBuffers);
        }

        public Auto(T value, MultiFenceHolder waitable, params IAutoPrivate[] referencedObjs) : this(value)
        {
            _waitable = waitable;
            _referencedObjs = referencedObjs;

            for (int i = 0; i < referencedObjs.Length; i++)
            {
                referencedObjs[i].IncrementReferenceCount();
            }
        }

        public T Get(CommandBufferScoped cbs, int offset, int size)
        {
            _waitable?.AddBufferUse(cbs.CommandBufferIndex, offset, size);
            return Get(cbs);
        }

        public T GetUnsafe()
        {
            return _value;
        }

        public T Get(CommandBufferScoped cbs)
        {
            if (!_destroyed)
            {
                AddCommandBufferDependencies(cbs);
            }

            return _value;
        }

        public void AddCommandBufferDependencies(CommandBufferScoped cbs)
        {
            // We don't want to add a reference to this object to the command buffer
            // more than once, so if we detect that the command buffer already has ownership
            // of this object, then we can just return without doing anything else.
            if (_cbOwnership.Set(cbs.CommandBufferIndex))
            {
                if (_waitable != null)
                {
                    cbs.AddWaitable(_waitable);
                }

                cbs.AddDependant(this);

                // We need to add a dependency on the command buffer to all objects this object
                // references aswell.
                if (_referencedObjs != null)
                {
                    for (int i = 0; i < _referencedObjs.Length; i++)
                    {
                        _referencedObjs[i].AddCommandBufferDependencies(cbs);
                    }
                }
            }
        }

        public void IncrementReferenceCount()
        {
            if (_referenceCount == 0)
            {
                throw new Exception("Attempted to inc ref of dead object.");
            }
            _referenceCount++;
        }

        public void DecrementReferenceCount(int cbIndex)
        {
            _cbOwnership.Clear(cbIndex);
            DecrementReferenceCount();
        }

        public void DecrementReferenceCount()
        {
            if (--_referenceCount == 0)
            {
                _value.Dispose();
                _value = default;
                _destroyed = true;

                // Value is no longer in use by the GPU, dispose all other
                // resources that it references.
                if (_referencedObjs != null)
                {
                    for (int i = 0; i < _referencedObjs.Length; i++)
                    {
                        _referencedObjs[i].DecrementReferenceCount();
                    }
                }
            }

            Debug.Assert(_referenceCount >= 0);
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                DecrementReferenceCount();
                _disposed = true;
            }
        }
    }
}
