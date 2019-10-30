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
    public class DedicatedHostSelector : IDedicatedHostSelector
    {
        private readonly ILogger<DedicatedHostSelector> _logger;
        private readonly IDedicatedHostStateManager _dedicatedHostStateManager;
        private readonly IDhmComputeClient _dhmComputeClient;

        public DedicatedHostSelector(
            ILogger<DedicatedHostSelector> logger, 
            IDedicatedHostStateManager dedicatedHostStateManager,
            IDhmComputeClient dhmComputeClient)
        {
            _logger = logger;
            _dedicatedHostStateManager = dedicatedHostStateManager;
            _dhmComputeClient = dhmComputeClient;
        }

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