using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest.Azure;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace DedicatedHostsManager.DedicatedHostEngine
{
    /// <summary>
    /// Dedicated Host Engine.
    /// </summary>
    public interface IDedicatedHostEngine
    {
        /// <summary>
        /// Creates a Dedicated Host Group.
        /// </summary>
        /// <param name="token">Auth token.</param>
        /// <param name="azureEnvironment">Azure cloud.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group.</param>
        /// <param name="dhgName">Dedicated Host group name.</param>
        /// <param name="azName">Availability zone.</param>
        /// <param name="platformFaultDomainCount">Fault domain count.</param>
        /// <param name="location">Location/region.</param>
        Task<AzureOperationResponse<DedicatedHostGroup>> CreateDedicatedHostGroup(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string dhgName,
            string azName,
            int platformFaultDomainCount,
            string location);

        /// <summary>
        /// Creates a Dedicated Host.
        /// </summary>
        /// <param name="token">Auth token.</param>
        /// <param name="azureEnvironment">Azure cloud.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group.</param>
        /// <param name="dhgName">Dedicated Host group name.</param>
        /// <param name="dhName">Dedicated Host name.</param>
        /// <param name="dhSku">Dedicated Host SKU</param>
        /// <param name="location">Azure region.</param>
        Task<AzureOperationResponse<DedicatedHost>> CreateDedicatedHost(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string dhgName,
            string dhName,
            string dhSku,
            string location);

        /// <summary>
        /// Creates a VM on a Dedicated Host
        /// </summary>
        /// <param name="token">Auth token.</param>
        /// <param name="azureEnvironment">Azure cloud.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group.</param>
        /// <param name="dhgName">Dedicated Host group name.</param>
        /// <param name="vmSku">VM SKU.</param>
        /// <param name="vmName">VM name.</param>
        /// <param name="region">Azure region for VM.</param>
        /// <param name="virtualMachine">VirtualMachine object (serialized).</param>
        Task<VirtualMachine> CreateVmOnDedicatedHost(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string dhgName,
            string vmSku,
            string vmName,
            Region region,
            VirtualMachine virtualMachine);

        /// <summary>
        /// Finds a Dedicated Host to host a VM.
        /// </summary>
        /// <param name="token">Auth token.</param>
        /// <param name="azureEnvironment">Azure cloud.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group.</param>
        /// <param name="hostGroupName">Dedicated Host group name.</param>
        /// /// <param name="requiredVmSize">VM SKU.</param>
        /// <param name="vmName">VM name.</param>
        /// <param name="location">VM region.</param>
        Task<string> GetDedicatedHostForVmPlacement(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName,
            string requiredVmSize,
            string vmName,
            string location);

        /// <summary>
        /// List Dedicated Host groups.
        /// </summary>
        /// <param name="token">Auth token.</param>
        /// <param name="azureEnvironment">Azure cloud.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        Task<IList<DedicatedHostGroup>> ListDedicatedHostGroups(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId);

        /// <summary>
        /// Deletes a VM running on a Dedicated Host, and the Host too if it does not have
        /// any more VMs running.
        /// </summary>
        /// <param name="token">Auth token.</param>
        /// <param name="azureEnvironment">Azure cloud.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group.</param>
        /// <param name="dedicatedHostGroup">Dedicated Host group name.</param>
        /// <param name="vmName">VM name.</param>
        /// <returns></returns>
        Task DeleteVmOnDedicatedHost(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string dedicatedHostGroup,
            string vmName);
    }
}