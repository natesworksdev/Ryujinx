using System.Threading.Tasks;

namespace Ryujinx.Horizon
{
    public interface IService
    {
        public Task Initialize();
        public Task ServiceRequests();
        public void Shutdown();
    }
}
