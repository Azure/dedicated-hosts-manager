using DedicatedHostsManager.ComputeClient;
using DedicatedHostsManager.DedicatedHostStateManager;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DedicatedHostsManager.DedicatedHostEngine
{
    /// <summary>
    /// The Dedicated Hosts selector.
    /// </summary>
    public class DedicatedHostSelector : IDedicatedHostSelector
    {
        private readonly ILogger<DedicatedHostSelector> _logger;
        private readonly IDedicatedHostStateManager _dedicatedHostStateManager;
        private readonly IDhmComputeClient _dhmComputeClient;

        /// <summary>
        /// Initializes the Dedicated Host selector.
        /// </summary>
        /// <param name="logger">Logger.</param>
        /// <param name="dedicatedHostStateManager">Dedicated Host state management.</param>
        /// <param name="dhmComputeClient">Dedicated Host compute client.</param>
        public DedicatedHostSelector(
            ILogger<DedicatedHostSelector> logger, 
            IDedicatedHostStateManager dedicatedHostStateManager,
            IDhmComputeClient dhmComputeClient)
        {
            _logger = logger;
            _dedicatedHostStateManager = dedicatedHostStateManager;
            _dhmComputeClient = dhmComputeClient;
        }

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
        public async Task<string> SelectDedicatedHost(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName,
            string requiredVmSize)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (azureEnvironment == null)
            {
                throw new ArgumentNullException(nameof(azureEnvironment));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            if (string.IsNullOrEmpty(subscriptionId))
            {
                throw new ArgumentException(nameof(subscriptionId));
            }

            if (string.IsNullOrEmpty(resourceGroup))
            {
                throw new ArgumentException(nameof(resourceGroup));
            }

            if (string.IsNullOrEmpty(hostGroupName))
            {
                throw new ArgumentException(nameof(hostGroupName));
            }

            var dedicatedHostList = await ListDedicatedHosts(
                token,
                azureEnvironment,
                tenantId,
                subscriptionId,
                resourceGroup,
                hostGroupName);
            var prunedDedicatedHostList = dedicatedHostList
                .Where(h => !_dedicatedHostStateManager.IsHostAtCapacity(h.Id.ToLower()) 
                            && !_dedicatedHostStateManager.IsHostMarkedForDeletion(h.Id.ToLower()))
                .ToList();
            if (!prunedDedicatedHostList.Any())
            {
                return null;
            }

            var hostToAvailableVmMapping = new ConcurrentDictionary<DedicatedHost, List<DedicatedHostAllocatableVM>>();
            var taskList = dedicatedHostList.Select(
                dedicatedHost => GetAllocatableVmsOnHost(
                    token,
                    azureEnvironment,
                    tenantId,
                    subscriptionId,
                    resourceGroup,
                    hostGroupName,
                    dedicatedHost,
                    hostToAvailableVmMapping)).ToList();
            await Task.WhenAll(taskList);
            var matchingHosts = new List<DedicatedHost>();
            foreach (var (dedicatedHost, allocatableVms) in hostToAvailableVmMapping)
            {
                if (allocatableVms.Any(allocatableVm => string.Equals(allocatableVm.VmSize, requiredVmSize, StringComparison.InvariantCultureIgnoreCase) && allocatableVm.Count >= 1.0))
                {
                    matchingHosts.Add(dedicatedHost);
                }
            }

            if (!matchingHosts.Any())
            {
                return null;
            }

            // TODO: Refactor matching host selection logic to allow configurable/custom selection.
            // TODO: Return based on how packed the hosts are, for now return a random host
            var randomHost = new Random().Next(matchingHosts.Count);
            return matchingHosts[randomHost].Id;
        }

        /// <summary>
        /// Retrieves VMs (number and type) that can be allocated on a Dedicated Host.
        /// </summary>
        /// <param name="token">Auth token.</param>
        /// <param name="azureEnvironment">Azure cloud.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group.</param>
        /// <param name="hostGroupName">Dedicated host group name.</param>
        /// <param name="dedicatedHost">Dedicated Host object.</param>
        /// <param name="dictionary">Dictionary object.</param>
        public virtual async Task GetAllocatableVmsOnHost(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName,
            DedicatedHost dedicatedHost,
            IDictionary<DedicatedHost, List<DedicatedHostAllocatableVM>> dictionary)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (azureEnvironment == null)
            {
                throw new ArgumentNullException(nameof(azureEnvironment));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            var azureCredentials = new AzureCredentials(
                new TokenCredentials(token),
                new TokenCredentials(token),
                tenantId,
                azureEnvironment);
            var computeManagementClient = await _dhmComputeClient.GetComputeManagementClient(
                subscriptionId,
                azureCredentials,
                azureEnvironment);
            var dedicatedHostDetails = await computeManagementClient.DedicatedHosts.GetAsync(
                resourceGroup,
                hostGroupName,
                dedicatedHost.Name,
                InstanceViewTypes.InstanceView,
                default(CancellationToken));
            var virtualMachineList = dedicatedHostDetails?.InstanceView?.AvailableCapacity?.AllocatableVMs?.ToList();
            if (virtualMachineList == null)
            {
                _logger.LogError($"Could not get available VM list for {dedicatedHost.Id}");
                return;
            }

            dictionary[dedicatedHost] = virtualMachineList;
        }

        /// <summary>
        /// List Dedicated Hosts in a host group.
        /// </summary>
        /// <param name="token">Auth token.</param>
        /// <param name="azureEnvironment">Azure cloud.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="resourceGroup">Resource group.</param>
        /// <param name="hostGroupName">Dedicated Host group name.</param>
        public virtual async Task<IList<DedicatedHost>> ListDedicatedHosts(
            string token,
            AzureEnvironment azureEnvironment,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (azureEnvironment == null)
            {
                throw new ArgumentNullException(nameof(azureEnvironment));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            var azureCredentials = new AzureCredentials(
                new TokenCredentials(token),
                new TokenCredentials(token),
                tenantId,
                azureEnvironment);
            var computeManagementClient = await _dhmComputeClient.GetComputeManagementClient(
                subscriptionId,
                azureCredentials,
                azureEnvironment);
            var dedicatedHostList = new List<DedicatedHost>();
            var dedicatedHostResponse = await computeManagementClient.DedicatedHosts.ListByHostGroupAsync(resourceGroup, hostGroupName);
            dedicatedHostList.AddRange(dedicatedHostResponse.ToList());
            var nextLink = dedicatedHostResponse.NextPageLink;
            while (!string.IsNullOrEmpty(nextLink))
            {
                dedicatedHostResponse = await computeManagementClient.DedicatedHosts.ListByHostGroupNextAsync(nextLink);
                dedicatedHostList.AddRange(dedicatedHostList.ToList());
                nextLink = dedicatedHostResponse.NextPageLink;
            }

            return dedicatedHostList;
        }
    }
}