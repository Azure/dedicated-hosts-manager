using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Rest;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DedicatedHostClientHelpers;
using System.Net;
using System.Web.Http;
using System.Linq;

namespace DedicatedHostsManagerFunctionClient
{
    /// <summary>
    /// Testing client for the Dedicated Hosts Manager library.
    /// </summary>
    public class DedicatedHostsManagerFunctionClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private const string ResourceGroupName = "resourceGroupName";
        private const string HostGroupName = "hostGroupName";
        private const string VmCount = "vmCount";
        private const string VmSku = "vmSku";
        private const string VmName = "vmName";

        /// <summary>
        /// Initialization.
        /// </summary>
        /// <param name="httpClientFactory">HTTP client factory.</param>
        /// <param name="configuration">Configuration.</param>
        public DedicatedHostsManagerFunctionClient(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = _httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        /// <summary>
        /// Test Dedicated Host Manager VM creation.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <param name="log">Logger.</param>
        /// <returns></returns>
        [FunctionName("TestDhmConcurrentVmCreation")]
        public async Task<IActionResult> TestDhmConcurrentVmCreation(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var parameters = req.GetQueryParameterDictionary();
            if (!parameters.ContainsKey(ResourceGroupName) || string.IsNullOrEmpty(parameters[ResourceGroupName]))
            {
                return new BadRequestObjectResult("Resource group name was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(HostGroupName) || string.IsNullOrEmpty(parameters[HostGroupName]))
            {
                return new BadRequestObjectResult("Host group name was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(VmCount) || string.IsNullOrEmpty(parameters[VmCount]))
            {
                return new BadRequestObjectResult("VmCount was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(VmSku) || string.IsNullOrEmpty(parameters[VmSku]))
            {
                return new BadRequestObjectResult("VM SKU was missing in the query parameters.");
            }

            var authEndpoint = _configuration["AuthEndpoint"];
            var azureRmEndpoint = _configuration["AzureRmEndpoint"];
            var location = _configuration["Location"];
            var virtualMachineSize = parameters[VmSku];
            var numVirtualMachines = int.Parse(parameters[VmCount]);
            var tenantId = _configuration["TenantId"];
            var clientId = _configuration["ClientId"];
            var clientSecret = _configuration["FairfaxClientSecret"];
            var subscriptionId = _configuration["SubscriptionId"];
            var resourceGroupName = parameters[ResourceGroupName];
            var hostGroupName = parameters[HostGroupName];

            log.LogInformation($"Generating auth token...");

            var token = await TokenHelper.GetToken(
                authEndpoint,
                azureRmEndpoint,
                tenantId,
                clientId,
                clientSecret);
            var customTokenProvider = new AzureCredentials(
                new TokenCredentials(token),
                new TokenCredentials(token),
                tenantId,
                AzureEnvironment.FromName(_configuration["CloudName"]));
            var client = RestClient
                .Configure()
                .WithEnvironment(AzureEnvironment.FromName(_configuration["CloudName"]))
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .WithCredentials(customTokenProvider)
                .Build();

            var azure = Azure.Authenticate(client, tenantId).WithSubscription(subscriptionId);
            var computeManagementClient = new ComputeManagementClient(customTokenProvider)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri(_configuration["ResourceManagerUri"]),
                LongRunningOperationRetryTimeout = 5
            };           

            log.LogInformation($"Creating resource group ({resourceGroupName}), if needed");
            var resourceGroup = azure.ResourceGroups.Define(resourceGroupName)
                .WithRegion(location)
                .Create();
            log.LogInformation($"Creating host group ({hostGroupName}), if needed");
            var newDedicatedHostGroup = new DedicatedHostGroup()
            {
                Location = location,
                PlatformFaultDomainCount = 1
            };
            await computeManagementClient.DedicatedHostGroups.CreateOrUpdateAsync(
                resourceGroupName,
                hostGroupName,
                newDedicatedHostGroup);

            var taskList = new List<Task<HttpResponseMessage>>();
            var inputDictionary = new Dictionary<string, StringContent>();
            for (var i = 0; i < numVirtualMachines; i++)
            {
                var vmName = $"vm{i}-{new Random().Next(1, 10000)}";

                log.LogInformation($"Configuring (not provisioning) VM: {vmName}");
                var virtualMachine = CreateVmHelper.CreateVirtualMachine(
                    computeManagementClient,
                    azure,
                    Region.Create(location),
                    resourceGroupName,
                    vmName,
                    virtualMachineSize,
                    null,
                    "pip-" + Guid.NewGuid(),
                    "adh-poc-vnet",
                    "nic-" + Guid.NewGuid());

#if DEBUG
                var createVmUri =
                    $"http://localhost:7071/api/CreateVm" +
                    $"?token={token}" +
                    $"&cloudName={_configuration["CloudName"]}" +
                    $"&tenantId={tenantId}" +
                    $"&subscriptionId={subscriptionId}" +
                    $"&resourceGroup={resourceGroupName}" +
                    $"&location={location}" +
                    $"&vmSku={virtualMachineSize}" +
                    $"&vmName={vmName}" +
                    $"&dedicatedHostGroupName={hostGroupName}" +
                    $"&platformFaultDomainCount=1";
#else
                var createVmUri =
                    _configuration["DhmCreateVmnUri"] +
                    $"&token={token}" +
                    $"&cloudName={_configuration["CloudName"]}" +
                    $"&tenantId={tenantId}" +
                    $"&subscriptionId={subscriptionId}" +
                    $"&resourceGroup={resourceGroupName}" +
                    $"&location={location}" +
                    $"&vmSku={virtualMachineSize}" +
                    $"&vmName={vmName}" +
                    $"&dedicatedHostGroupName={hostGroupName}" +
                    $"&platformFaultDomainCount=1";
#endif
                var httpContent = new StringContent(JsonConvert.SerializeObject(virtualMachine), Encoding.UTF8, "application/json");
                inputDictionary[createVmUri] = httpContent;
            }

            foreach (var item in inputDictionary)
            {
                taskList.Add(_httpClient.PostAsync(item.Key, item.Value));
            }

            await Task.WhenAll(taskList);
            return new OkObjectResult($"VM provisioning kicked off successfully for {numVirtualMachines} VMs - exiting.");
        }

        /// <summary>
        /// Test Dedicated Host Manager VM creation.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <param name="log">Logger.</param>
        /// <returns></returns>
        [FunctionName("TestDhmVmDeletion")]
        public async Task<IActionResult> TestDhmConcurrentVmDeletion(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var parameters = req.GetQueryParameterDictionary();

            if (!parameters.ContainsKey(ResourceGroupName) || string.IsNullOrEmpty(parameters[ResourceGroupName]))
            {
                return new BadRequestObjectResult("ResourceGroupName was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(HostGroupName) || string.IsNullOrEmpty(parameters[HostGroupName]))
            {
                return new BadRequestObjectResult("HostGroupName was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(VmName) || string.IsNullOrEmpty(parameters[VmName]))
            {
                return new BadRequestObjectResult("VmName was missing in the query parameters.");
            }

            var authEndpoint = _configuration["AuthEndpoint"];
            var azureRmEndpoint = _configuration["AzureRmEndpoint"];
            var location = _configuration["Location"];
            var vmName = parameters[VmName];
            var tenantId = _configuration["TenantId"];
            var clientId = _configuration["ClientId"];
            var clientSecret = _configuration["FairfaxClientSecret"];
            var subscriptionId = _configuration["SubscriptionId"];
            var resourceGroupName = parameters[ResourceGroupName];
            var hostGroupName = parameters[HostGroupName];

            log.LogInformation($"Generating auth token...");

            var token = await TokenHelper.GetToken(
                authEndpoint,
                azureRmEndpoint,
                tenantId,
                clientId,
                clientSecret);
            var customTokenProvider = new AzureCredentials(
                new TokenCredentials(token),
                new TokenCredentials(token),
                tenantId,
                AzureEnvironment.FromName(_configuration["CloudName"]));
            var client = RestClient
                .Configure()
                .WithEnvironment(AzureEnvironment.FromName(_configuration["CloudName"]))
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .WithCredentials(customTokenProvider)
                .Build();

            var azure = Azure.Authenticate(client, tenantId).WithSubscription(subscriptionId);
            var computeManagementClient = new ComputeManagementClient(customTokenProvider)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri(_configuration["ResourceManagerUri"]),
                LongRunningOperationRetryTimeout = 5
            };

#if DEBUG
            var deleteVmUri =
                $"http://localhost:7071/api/DeleteVm" +
                $"?token={token}" +
                $"&cloudName={_configuration["CloudName"]}" +
                $"&tenantId={tenantId}" +
                $"&subscriptionId={subscriptionId}" +
                $"&resourceGroup={resourceGroupName}" +
                $"&dedicatedHostGroupName={hostGroupName}" +
                $"&vmName={vmName}";
#else
            var deleteVmUri =
                _configuration["DhmDeleteVmnUri"] +
                $"&token={token}" +
                $"&cloudName={_configuration["CloudName"]}" +
                $"&tenantId={tenantId}" +
                $"&subscriptionId={subscriptionId}" +
                $"&resourceGroup={resourceGroupName}" +
                $"&dedicatedHostGroupName={hostGroupName}" +
                $"&vmName={vmName}";
#endif

            await _httpClient.GetAsync(deleteVmUri);
            return new OkObjectResult($"Deleted {vmName} VM.");
        }

        /// <summary>
        /// Test Prepare Dedicated Host.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <param name="log">Logger.</param>
        /// <returns></returns>
        [FunctionName("TestPrepareDedicatedHostGroup")]
        public async Task<IActionResult> TestPrepareDedicatedHostGroup(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var parameters = req.GetQueryParameterDictionary();
            if (!parameters.ContainsKey(ResourceGroupName) || string.IsNullOrEmpty(parameters[ResourceGroupName]))
            {
                return new BadRequestObjectResult("Resource group name was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(HostGroupName) || string.IsNullOrEmpty(parameters[HostGroupName]))
            {
                return new BadRequestObjectResult("Host group name was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(VmCount) || !Int32.TryParse(parameters[VmCount], out int numVirtualMachines))
            {
                return new BadRequestObjectResult("VmCount was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(VmSku) || string.IsNullOrEmpty(parameters[VmSku]))
            {
                return new BadRequestObjectResult("VM SKU was missing in the query parameters.");
            }

            var authEndpoint = _configuration["AuthEndpoint"];
            var azureRmEndpoint = _configuration["AzureRmEndpoint"];
            var location = _configuration["Location"];
            var virtualMachineSize = parameters[VmSku];
            var tenantId = _configuration["TenantId"];
            var clientId = _configuration["ClientId"];
            var clientSecret = _configuration["FairfaxClientSecret"];
            var subscriptionId = _configuration["SubscriptionId"];
            var resourceGroupName = parameters[ResourceGroupName];
            var hostGroupName = parameters[HostGroupName];

            log.LogInformation($"Generating auth token...");

            var token = await TokenHelper.GetToken(
                authEndpoint,
                azureRmEndpoint,
                tenantId,
                clientId,
                clientSecret);
            var customTokenProvider = new AzureCredentials(
                new TokenCredentials(token),
                new TokenCredentials(token),
                tenantId,
                AzureEnvironment.FromName(_configuration["CloudName"]));
            var client = RestClient
                .Configure()
                .WithEnvironment(AzureEnvironment.FromName(_configuration["CloudName"]))
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.Basic)
                .WithCredentials(customTokenProvider)
                .Build();

            var azure = Azure.Authenticate(client, tenantId).WithSubscription(subscriptionId);
            var computeManagementClient = new ComputeManagementClient(customTokenProvider)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri(_configuration["ResourceManagerUri"]),
                LongRunningOperationRetryTimeout = 5
            };

            log.LogInformation($"Creating resource group ({resourceGroupName}), if needed");
            var resourceGroup = azure.ResourceGroups.Define(resourceGroupName)
                .WithRegion(location)
                .Create();
            log.LogInformation($"Creating host group ({hostGroupName}), if needed");
            var newDedicatedHostGroup = new DedicatedHostGroup()
            {
                Location = location,
                PlatformFaultDomainCount = 1
            };
            await computeManagementClient.DedicatedHostGroups.CreateOrUpdateAsync(
                resourceGroupName,
                hostGroupName,
                newDedicatedHostGroup);

             
#if DEBUG
                var prepareDHGroup =
                    $"http://localhost:7071/api/PrepareDedicatedHostGroup" +
                    $"?token={token}" +
                    $"&cloudName={_configuration["CloudName"]}" +
                    $"&tenantId={tenantId}" +
                    $"&subscriptionId={subscriptionId}" +
                    $"&resourceGroup={resourceGroupName}" +
                    $"&vmSku={virtualMachineSize}" +
                    $"&dedicatedHostGroupName={hostGroupName}" +
                    $"&vmCount={numVirtualMachines}" +
                    $"&platformFaultDomain=0";
#else
                var prepareDHGroup =
                    _configuration["PrepareDHGroupUri"] +
                    $"&token={token}" +
                    $"&cloudName={_configuration["CloudName"]}" +
                    $"&tenantId={tenantId}" +
                    $"&subscriptionId={subscriptionId}" +
                    $"&resourceGroup={resourceGroupName}" +
                    $"&vmSku={virtualMachineSize}" +
                    $"&dedicatedHostGroupName={hostGroupName}" +
                    $"&vmCount={numVirtualMachines}" +
                    $"&platformFaultDomain=0";
#endif
            var response = await _httpClient.GetAsync(prepareDHGroup);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return new ObjectResult(new { error = $"Exception thrown by {await response.Content.ReadAsStringAsync()}" })
                {
                    StatusCode = (int)response.StatusCode
                };
            }
            var dhCreated = await response.Content.ReadAsAsync<List<DedicatedHost>>();
            return new OkObjectResult($"Prepared Dedicated Host Group completed successfully {string.Join(",", dhCreated.Select(c => c.Name))} VM.");
        }
    }
}
