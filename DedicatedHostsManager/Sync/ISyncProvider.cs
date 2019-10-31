using System.Threading.Tasks;

namespace DedicatedHostsManager.Sync
{
    public interface ISyncProvider
    {
        /// <summary>
        /// Serializing logic (start) using storage leases.
        /// </summary>
        /// <param name="blobName">Storage blob name.</param>
        Task StartSerialRequests(string blobName);

        /// <summary>
        /// Serializing logic (stop) using storage leases.
        /// </summary>
        /// <param name="blobName">Storage blob name.</param>
        Task EndSerialRequests(string blobName);
    }
}