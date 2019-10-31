using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DedicatedHostsManager.DedicatedHostEngine
{
    public interface IDedicatedHostSelector
    {
        /// <summary>
        /// Selects a Dedicated Host from a pool of available hosts.
        /// </summary>
        /// <param name="token">Auth token.</param>
        /// <param name="azureEnvironment">Azure cloud.</param>
        /// <param name="tenantId">Azure tenant ID.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group.</param>
        /// <param name="hostGroupName">Dedicated Host group name.</param>
        /// <param name="requiredVmSize">Needed VM size/SKU.</param>
        Task<string> SelectDedicatedHost(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName,
            string requiredVmSize);

        /// <summary>
        /// List Dedicated Hosts in a host group.
        /// </summary>
        /// <param name="token">Auth token.</param>
        /// <param name="azureEnvironment">Azure cloud.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group.</param>
        /// <param name="hostGroupName">Dedicated Host group name.</param>
        Task<IList<DedicatedHost>> ListDedicatedHosts(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName);
    }
}