﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Ryujinx.Common
{
    public class ObjectPool<T>
        where T : class
    {
        private T _firstItem;
        private readonly T[] _items;

        private readonly Func<T> _factory;

        public ObjectPool(Func<T> factory, int size)
        {
            _items   = new T[size - 1];
            _factory = factory;
        }

        public T Allocate()
        {
            var instance = _firstItem;

            if (instance == null || instance != Interlocked.CompareExchange(ref _firstItem, null, instance))
            {
                instance = AllocateInternal();
            }

            return instance;
        }

        private T AllocateInternal()
        {
            var items = _items;

            for (int i = 0; i < items.Length; i++)
            {
                var instance = items[i];

                if (instance != null && instance == Interlocked.CompareExchange(ref items[i], null, instance))
                {
                    return instance;
                }
            }

            return _factory();
        }

        public void Release(T obj)
        {
            if (_firstItem == null)
            {
                _firstItem = obj;
            }
            else
            {
                ReleaseInternal(obj);
            }
        }

        private void ReleaseInternal(T obj)
        {
            var items = _items;

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    items[i] = obj;
                    break;
                }
            }
        }
    }
}
