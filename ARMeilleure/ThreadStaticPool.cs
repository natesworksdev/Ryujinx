using System;
using System.Threading;

namespace ARMeilleure
{
    internal class ThreadStaticPool<T> where T : class, new()
    {
        [ThreadStatic]
        private static ThreadStaticPool<T> _instance;
        public static ThreadStaticPool<T> Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ThreadStaticPool<T>(_poolSizeIncrement * 2);
                }
                return _instance;
            }
        }

        private T[] _pool;
        private int _poolUsed = -1;
        private int _poolSize;
        private static readonly int _poolSizeIncrement = 200;

        public ThreadStaticPool(int initialSize)
        {
            _pool = new T[initialSize];

            for (int i = 0; i < initialSize; i++)
            {
                _pool[i] = new T();
            }

            _poolSize = initialSize;
        }

        public T Allocate()
        {
            int index = Interlocked.Increment(ref _poolUsed);
            if (index >= _poolSize)
            {
                IncreaseSize();
            }
            return _pool[index];
        }

        private void IncreaseSize()
        {
            _poolSize += _poolSizeIncrement;

            T[] newArray = new T[_poolSize];
            Array.Copy(_pool, 0, newArray, 0, _pool.Length);

            for (int i = _pool.Length; i < _poolSize; i++)
            {
                newArray[i] = new T();
            }

            Interlocked.Exchange(ref _pool, newArray);
        }

        public void Clear()
        {
            _poolUsed = -1;
        }
    }
}
