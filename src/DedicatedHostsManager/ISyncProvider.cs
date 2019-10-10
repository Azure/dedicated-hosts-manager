using System.Threading.Tasks;

namespace DedicatedHostsManager
{
    public interface ISyncProvider
    {
        Task StartSerialRequests(string blobName);

        Task EndSerialRequests(string blobName);
    }
}