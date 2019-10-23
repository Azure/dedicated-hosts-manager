using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DedicatedHostsManager.Helpers;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Microsoft.Rest.Azure;
using Microsoft.WindowsAzure.Storage;
using Newtonsoft.Json;
using Polly;
using SubResource = Microsoft.Azure.Management.Compute.Models.SubResource;

///
namespace DedicatedHostsManager
{
    /// <summary>
    /// 
    /// </summary>
    public class DedicatedHostEngine : IDedicatedHostEngine
    {
        private readonly ILogger<DedicatedHostEngine> _logger;
        private readonly IConfiguration _configuration;
        private readonly IDedicatedHostSelector _dedicatedHostSelector;
        private readonly ISyncProvider _syncProvider;

        public DedicatedHostEngine(
            ILogger<DedicatedHostEngine> logger, 
            IConfiguration configuration,
            IDedicatedHostSelector dedicatedHostSelector,
            ISyncProvider syncProvider)
        {
            _logger = logger;
            _configuration = configuration;
            _dedicatedHostSelector = dedicatedHostSelector;
            _syncProvider = syncProvider;
        }

        public async Task<AzureOperationResponse<DedicatedHostGroup>> CreateDedicatedHostGroup(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId, 
            string resourceGroup, 
            string dhgName,
            string azName,
            int platformFaultDomainCount,
            string location)
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

            if (string.IsNullOrEmpty(dhgName))
            {
                throw new ArgumentException(nameof(dhgName));
            }

            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentException(nameof(location));
            }

            var azureCredentials = new AzureCredentials(
                new TokenCredentials(token),
                new TokenCredentials(token),
                tenantId,
                AzureEnvironment.FromName(cloudName));
        
            var newDedicatedHostGroup = new DedicatedHostGroup()
            {
                Location = location,
                PlatformFaultDomainCount = platformFaultDomainCount
            };

            if (!string.IsNullOrEmpty(azName))
            {
                newDedicatedHostGroup.Zones = new List<string>{ azName };
            }

            var dhgCreateRetryCount = int.Parse(_configuration["DhgCreateRetryCount"]);
            var computeManagementClient = ComputeManagementClient(subscriptionId, azureCredentials);
            var response = new AzureOperationResponse<DedicatedHostGroup>();
            await Policy
                .Handle<CloudException>()
                .WaitAndRetryAsync(
                    dhgCreateRetryCount,
                    r => TimeSpan.FromSeconds(2 * r),
                    (ex, ts, r) =>
                        _logger.LogInformation(
                            $"Create host group {dhgName} failed. Attempt #{r}/10. Will try again in {ts.TotalSeconds} seconds. Exception={ex}"))
                .ExecuteAsync(async () =>
                {
                    response = await computeManagementClient.DedicatedHostGroups.CreateOrUpdateWithHttpMessagesAsync(
                        resourceGroup,
                        dhgName,
                        newDedicatedHostGroup,
                        null,
                        default(CancellationToken));
                });

            return response;
        }
      
        public async Task<AzureOperationResponse<DedicatedHost>> CreateDedicatedHost(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId, 
            string resourceGroup, 
            string dhgName, 
            string dhName,
            string dhSku,
            string location)
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

            if (string.IsNullOrEmpty(dhgName))
            {
                throw new ArgumentException(nameof(dhgName));
            }

            if (string.IsNullOrEmpty(dhName))
            {
                throw new ArgumentException(nameof(dhName));
            }

            if (string.IsNullOrEmpty(dhSku))
            {
                throw new ArgumentException(nameof(dhSku));
            }

            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentException(nameof(location));
            }

            var azureCredentials = new AzureCredentials(
                new TokenCredentials(token),
                new TokenCredentials(token),
                tenantId,
                AzureEnvironment.FromName(cloudName));

            var computeManagementClient = DedicatedHostHelpers.ComputeManagementClient(subscriptionId, azureCredentials);
            if (await computeManagementClient.DedicatedHostGroups.GetAsync(resourceGroup, dhgName) == null)
            {
                await computeManagementClient.DedicatedHostGroups.CreateOrUpdateAsync(
                    resourceGroup,
                    dhgName,
                    new DedicatedHostGroup()
                    {
                        Location = location,
                        PlatformFaultDomainCount = 1
                    });
            }

            return await computeManagementClient.DedicatedHosts.CreateOrUpdateWithHttpMessagesAsync(
                resourceGroup,
                dhgName,
                dhName,
                new DedicatedHost
                {
                    Location = location,
                    Sku = new Sku() {Name = dhSku}
                },
                null);
        }

        public async Task<AzureOperationResponse<VirtualMachine>> CreateVmOnDedicatedHost(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string dhgName,
            string vmSku,
            string vmName,
            Region region,
            VirtualMachine virtualMachine)
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

            if (string.IsNullOrEmpty(dhgName))
            {
                throw new ArgumentException(nameof(dhgName));
            }

            if (string.IsNullOrEmpty(vmSku))
            {
                throw new ArgumentException(nameof(vmSku));
            }

            if (string.IsNullOrEmpty(vmName))
            {
                throw new ArgumentException(nameof(vmName));
            }

            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            if (virtualMachine == null)
            {
                throw new ArgumentNullException(nameof(virtualMachine));
            }

            var azureCredentials = new AzureCredentials(
                new TokenCredentials(token),
                new TokenCredentials(token),
                tenantId,
                AzureEnvironment.FromName(cloudName));

            var computeManagementClient = DedicatedHostHelpers.ComputeManagementClient(subscriptionId, azureCredentials);
            var response = new AzureOperationResponse<VirtualMachine>();
            var vmProvisioningState = virtualMachine.ProvisioningState;
            var minIntervalToCheckForVmInSeconds = int.Parse(_configuration["MinIntervalToCheckForVmInSeconds"]);
            var maxIntervalToCheckForVmInSeconds = int.Parse(_configuration["MaxIntervalToCheckForVmInSeconds"]);
            var retryCountToCheckVmState = int.Parse(_configuration["RetryCountToCheckVmState"]);
            var maxRetriesToCreateVm = int.Parse(_configuration["MaxRetriesToCreateVm"]);
            var vmCreationRetryCount = 0;
            
            while ((string.IsNullOrEmpty(vmProvisioningState)
                   || !string.Equals(vmProvisioningState, "Succeeded", StringComparison.InvariantCultureIgnoreCase))
                   && vmCreationRetryCount < maxRetriesToCreateVm)
            {             
                if (string.IsNullOrEmpty(vmProvisioningState))
                {
                    var dedicatedHostId = await GetDedicatedHostForVmPlacement(
                        token,
                        cloudName,
                        tenantId,
                        subscriptionId,
                        resourceGroup,
                        dhgName,
                        vmSku,
                        vmName,
                        region.Name);

                    virtualMachine.Host = new SubResource(dedicatedHostId);
                    response = await computeManagementClient.VirtualMachines
                        .BeginCreateOrUpdateWithHttpMessagesAsync(
                            resourceGroup,
                            vmName,
                            virtualMachine,
                            null);
                }

                // TODO: Remove below if block once the Compute DH bug is fixed.
                // TODO: Intentional code duplication below to keep logic related to this bug separate.
                if (string.Equals(vmProvisioningState, "Failed", StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.LogMetric("VmProvisioningFailureCountMetric", 1);
                    var dedicatedHostId = await GetDedicatedHostForVmPlacement(
                        token,
                        cloudName,
                        tenantId,
                        subscriptionId,
                        resourceGroup,
                        dhgName,
                        vmSku,
                        vmName,
                        region.Name,
                        virtualMachine.Host.Id);

                    await computeManagementClient.VirtualMachines.DeallocateAsync(resourceGroup, virtualMachine.Name);
                    virtualMachine.Host = new SubResource(dedicatedHostId);
                    response = await computeManagementClient.VirtualMachines
                        .BeginCreateOrUpdateWithHttpMessagesAsync(
                            resourceGroup,
                            vmName,
                            virtualMachine,
                            null);
                    await computeManagementClient.VirtualMachines.StartAsync(resourceGroup, virtualMachine.Name);
                }

                // VM provisioning takes a few seconds, wait for provisioning state to update
                await Task.Delay(TimeSpan.FromSeconds(new Random().Next(minIntervalToCheckForVmInSeconds, maxIntervalToCheckForVmInSeconds)));
                await Policy
                    .Handle<CloudException>()
                    .WaitAndRetryAsync(
                        retryCountToCheckVmState, 
                        r => TimeSpan.FromSeconds(2 * r),
                        onRetry: (ex, ts, r) =>
                            _logger.LogInformation(
                                $"Could not get provisioning state for {virtualMachine.Name}. Attempt #{r}/10. Will try again in {ts.TotalSeconds} seconds. Exception={ex}"))
                    .ExecuteAsync(async () =>
                    {
                        vmProvisioningState =
                            (await computeManagementClient.VirtualMachines.GetAsync(resourceGroup, virtualMachine.Name))
                            .ProvisioningState;
                    });

                _logger.LogInformation($"Provisioning state for {virtualMachine.Name} is {vmProvisioningState}");
                vmCreationRetryCount++;
            }

            return response;
        }

        public async Task<string> GetDedicatedHostForVmPlacement(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName,
            string requiredVmSize,
            string vmName,
            string location,
            string oldHostId = null)
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

            if (string.IsNullOrEmpty(requiredVmSize))
            {
                throw new ArgumentException(nameof(requiredVmSize));
            }

            if (string.IsNullOrEmpty(vmName))
            {
                throw new ArgumentException(nameof(vmName));
            }

            if (string.IsNullOrEmpty(location))
            {
                throw new ArgumentException(nameof(location));
            }

            var matchingHostId = string.Empty;
            var innerLoopStopwatch = Stopwatch.StartNew();

            while (string.IsNullOrEmpty(matchingHostId))
            {
                matchingHostId = await SelectHostFromHostGroup(
                    token, 
                    cloudName, 
                    tenantId, 
                    subscriptionId, 
                    resourceGroup, 
                    hostGroupName, 
                    requiredVmSize);

                if (string.IsNullOrEmpty(matchingHostId)
                    || (!string.IsNullOrEmpty(oldHostId) && string.Equals(matchingHostId, oldHostId, StringComparison.InvariantCultureIgnoreCase)))
                {
                    var lockRetryCount = int.Parse(_configuration["LockRetryCount"]);
                    await Policy
                        .Handle<StorageException>()
                        .WaitAndRetryAsync(
                            lockRetryCount, 
                            r => TimeSpan.FromSeconds(2 * r),
                            (ex, ts, r) => _logger.LogInformation($"Attempt #{r.Count}/10. Will try again in {ts.TotalSeconds} seconds. Exception={ex}"))
                        .ExecuteAsync(async () =>
                        {
                            try
                            {
                                _logger.LogInformation($"About to lock");
                                await _syncProvider.StartSerialRequests(_configuration["LockBlobName"]);

                                matchingHostId = await SelectHostFromHostGroup(
                                    token,
                                    cloudName,
                                    tenantId,
                                    subscriptionId,
                                    resourceGroup,
                                    hostGroupName,
                                    requiredVmSize);

                                if (string.IsNullOrEmpty(matchingHostId) 
                                    || (!string.IsNullOrEmpty(oldHostId) && string.Equals(matchingHostId, oldHostId, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    _logger.LogInformation($"Creating a new host.");
                                    var vmToHostDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(_configuration["VmToHostMapping"]);
                                    if (vmToHostDictionary == null || string.IsNullOrEmpty(vmToHostDictionary[requiredVmSize]))
                                    {
                                        throw new Exception($"Cannot find a dedicated host SKU for the {requiredVmSize}: vm to host mapping was null.");
                                    }

                                    var hostSku = vmToHostDictionary[requiredVmSize];
                                    _logger.LogInformation($"Host SKU {hostSku} will be used to host VM SKU {requiredVmSize}.");
                                    if (string.IsNullOrEmpty(hostSku))
                                    {
                                        throw new Exception(
                                            $"Cannot find a dedicated host SKU for the {requiredVmSize}: vm to host mapping was null.");
                                    }

                                    var newDedicatedHostResponse = await CreateDedicatedHost(
                                        token,
                                        cloudName,
                                        tenantId,
                                        subscriptionId,
                                        resourceGroup,
                                        hostGroupName,
                                        "host-" + (new Random().Next(100,999)),
                                        hostSku,
                                        location);

                                    matchingHostId = newDedicatedHostResponse.Body.Id;
                                    _logger.LogMetric("DedicatedHostCreationCountMetric", 1);
                                }
                            }
                            catch (Exception exception)
                            {
                                _logger.LogError($"Error while finding a DH: {exception}");
                            }
                            finally
                            {
                                _logger.LogInformation($"Releasing the lock");
                                await _syncProvider.EndSerialRequests(_configuration["LockBlobName"]);
                            }
                        });
                }

                _logger.LogInformation($"Retry to find a host for {requiredVmSize}");
            }

            if (string.IsNullOrEmpty(matchingHostId))
            {
                _logger.LogError($"Something went really wrong! Could not find a " +
                                 $"matching host for {requiredVmSize} within {innerLoopStopwatch.Elapsed.TotalSeconds} seconds. ");
            }

            _logger.LogMetric("GetDedicatedHostTimeSecondsMetric", innerLoopStopwatch.Elapsed.TotalSeconds);
            _logger.LogInformation($"GetDedicatedHost: Took {innerLoopStopwatch.Elapsed.TotalSeconds} seconds to find a matching host {matchingHostId} for {vmName} of {requiredVmSize} SKU.");
            return matchingHostId;
        }

        private async Task<string> SelectHostFromHostGroup(
            string token, 
            string cloudName, 
            string tenantId, 
            string subscriptionId,
            string resourceGroup, 
            string hostGroupName, 
            string requiredVmSize)
        {
            var dedicatedHostList = await ListDedicatedHosts(
                token,
                cloudName,
                tenantId,
                subscriptionId,
                resourceGroup,
                hostGroupName);

            var matchingHost = await _dedicatedHostSelector.SelectDedicatedHost(
                token,
                cloudName,
                tenantId,
                subscriptionId,
                resourceGroup,
                hostGroupName,
                requiredVmSize,
                dedicatedHostList);

            return matchingHost;
        }

        public async Task<IList<DedicatedHost>> ListDedicatedHosts(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId, 
            string resourceGroup, 
            string hostGroupName)
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

            var computeManagementClient = DedicatedHostHelpers.ComputeManagementClient(subscriptionId, azureCredentials);
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

        public async Task<IList<DedicatedHostGroup>> ListDedicatedHostGroups(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId)
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

            var computeManagementClient = DedicatedHostHelpers.ComputeManagementClient(subscriptionId, azureCredentials);
            var dedicatedHostGroups = new List<DedicatedHostGroup>();
            var dedicatedHostGroupResponse =
                await computeManagementClient.DedicatedHostGroups.ListBySubscriptionAsync();
            dedicatedHostGroups.AddRange(dedicatedHostGroupResponse.ToList());

            var nextLink = dedicatedHostGroupResponse.NextPageLink;
            while (!string.IsNullOrEmpty(nextLink))
            {
                dedicatedHostGroupResponse =
                    await computeManagementClient.DedicatedHostGroups.ListBySubscriptionNextAsync(nextLink);
                dedicatedHostGroups.AddRange(dedicatedHostGroups.ToList());
                nextLink = dedicatedHostGroupResponse.NextPageLink;
            }

            return dedicatedHostGroups;
        }

        public async Task<IAzureOperationResponse> DeleteDedicatedHostGroup(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName)
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

            var computeManagementClient = DedicatedHostHelpers.ComputeManagementClient(subscriptionId, azureCredentials);
            return await computeManagementClient.DedicatedHostGroups.DeleteWithHttpMessagesAsync(resourceGroup, hostGroupName);
        }

        public async Task<IAzureOperationResponse> DeleteDedicatedHostGroup(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName,
            string dhName)
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

            var computeManagementClient = DedicatedHostHelpers.ComputeManagementClient(subscriptionId, azureCredentials);

            return await computeManagementClient.DedicatedHosts.DeleteWithHttpMessagesAsync(
                resourceGroup,
                hostGroupName, 
                dhName,
                null);
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
