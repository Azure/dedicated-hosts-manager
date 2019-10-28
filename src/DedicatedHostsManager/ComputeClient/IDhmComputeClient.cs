using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace DedicatedHostsManager.ComputeClient
{
    public interface IDhmComputeClient
    {
        Task<IComputeManagementClient> GetComputeManagementClient(
            string subscriptionId,
            AzureCredentials azureCredentials,
            AzureEnvironment azureEnvironment);
    }
}