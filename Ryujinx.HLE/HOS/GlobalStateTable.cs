using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Ryujinx.HLE.HOS
{
    internal class GlobalStateTable
    {
        private ConcurrentDictionary<Process, IdDictionary> _dictByProcess;

        public GlobalStateTable()
        {
            _dictByProcess = new ConcurrentDictionary<Process, IdDictionary>();
        }

        public bool Add(Process process, int id, object data)
        {
            IdDictionary dict = _dictByProcess.GetOrAdd(process, (key) => new IdDictionary());

            return dict.Add(id, data);
        }

        public int Add(Process process, object data)
        {
            IdDictionary dict = _dictByProcess.GetOrAdd(process, (key) => new IdDictionary());

            return dict.Add(data);
        }

        public object GetData(Process process, int id)
        {
            if (_dictByProcess.TryGetValue(process, out IdDictionary dict))
            {
                return dict.GetData(id);
            }

            return null;
        }

        public T GetData<T>(Process process, int id)
        {
            if (_dictByProcess.TryGetValue(process, out IdDictionary dict))
            {
                return dict.GetData<T>(id);
            }

            return default(T);
        }

        public object Delete(Process process, int id)
        {
            if (_dictByProcess.TryGetValue(process, out IdDictionary dict))
            {
                return dict.Delete(id);
            }

            return null;
        }

        public ICollection<object> DeleteProcess(Process process)
        {
            if (_dictByProcess.TryRemove(process, out IdDictionary dict))
            {
                return dict.Clear();
            }

            return null;
        }
    }
}