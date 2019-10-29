using System.Threading.Tasks;

namespace DedicatedHostsManager.Sync
{
    public interface ISyncProvider
    {
        Task StartSerialRequests(string blobName);

        Task EndSerialRequests(string blobName);
    }
}