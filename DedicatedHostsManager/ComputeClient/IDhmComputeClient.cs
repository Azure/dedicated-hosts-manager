using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using System.Threading.Tasks;

namespace DedicatedHostsManager.ComputeClient
{
    /// <summary>
    /// Compute client to use with Dedicated Host calls.
    /// </summary>
    public interface IDhmComputeClient
    {
        Task<IComputeManagementClient> GetComputeManagementClient(
            string subscriptionId,
            AzureCredentials azureCredentials,
            AzureEnvironment azureEnvironment);
    }
}