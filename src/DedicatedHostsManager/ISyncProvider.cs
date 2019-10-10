using System.Threading.Tasks;

namespace DedicatedHosts
{
    public interface ISyncProvider
    {
        Task StartSerialRequests(string blobName);

        Task EndSerialRequests(string blobName);
    }
}