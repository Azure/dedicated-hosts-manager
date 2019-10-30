using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace DedicatedHostsManager.DedicatedHostEngine
{
    public interface IDedicatedHostSelector
    {
        Task<string> SelectDedicatedHost(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName,
            string requiredVmSize);

        Task<IList<DedicatedHost>> ListDedicatedHosts(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName);
    }
}