using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DedicatedHostsManager.Helpers;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;

namespace DedicatedHostsManager
{
    public class DedicatedHostSelector : IDedicatedHostSelector
    {
        private readonly ILogger<DedicatedHostSelector> _logger;

        public DedicatedHostSelector(ILogger<DedicatedHostSelector> logger)
        {
            _logger = logger;
        }

        public async Task<string> SelectDedicatedHost(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName,
            string requiredVmSize,
            IList<DedicatedHost> dedicatedHostList)
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (string.IsNullOrEmpty(cloudName))
            {
                throw new ArgumentNullException(nameof(cloudName));
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

            if (dedicatedHostList == null)
            {
                throw new ArgumentException(nameof(dedicatedHostList));
            }

            var hostToAvailableVmMapping = new ConcurrentDictionary<DedicatedHost, List<DedicatedHostAllocatableVM>>();
            var taskList = dedicatedHostList.Select(
                dedicatedHost => GetAllocatableVmsOnHost(
                    token,
                    cloudName,
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

        public async Task GetAllocatableVmsOnHost(
            string token,
            string cloudName,
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

            if (string.IsNullOrEmpty(cloudName))
            {
                throw new ArgumentNullException(nameof(cloudName));
            }

            if (string.IsNullOrEmpty(tenantId))
            {
                throw new ArgumentNullException(nameof(tenantId));
            }

            var azureCredentials = new AzureCredentials(
                new TokenCredentials(token),
                new TokenCredentials(token),
                tenantId,
                AzureEnvironment.FromName(cloudName));

            var computeManagementClient = ComputeManagementClient(subscriptionId, azureCredentials);
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

        protected virtual IComputeManagementClient ComputeManagementClient(
            string subscriptionId,
            AzureCredentials azureCredentials)
        {
            var computeManagementClient = DedicatedHostHelpers.ComputeManagementClient(subscriptionId, azureCredentials);
            return computeManagementClient;
        }
    }
}