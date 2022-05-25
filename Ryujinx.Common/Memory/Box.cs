namespace Ryujinx.Common.Memory
{
    public class Box<T> where T : unmanaged
    {
        private T _data;

        public ref T Data => ref _data;

        public Box()
        {
            _data = new T();
        }
    }
}
