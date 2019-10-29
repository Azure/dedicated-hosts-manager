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
using ScaleTestHelpers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TrafficGeneratorFunction
{
    public class Function1TrafficGenTestFunction
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private const string ResourceGroupName = "resourceGroupName";
        private const string HostGroupName = "hostGroupName";
        private const string VmCount = "vmCount";
        private const string VmSku = "vmSku";

        public Function1TrafficGenTestFunction(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _httpClient = _httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        [FunctionName("TestHostManager")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            var parameters = req.GetQueryParameterDictionary();
            if (!parameters.ContainsKey(ResourceGroupName) || string.IsNullOrEmpty(parameters[ResourceGroupName]))
            {
                return new BadRequestObjectResult("VmCount was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(HostGroupName) || string.IsNullOrEmpty(parameters[HostGroupName]))
            {
                return new BadRequestObjectResult("Vm SKU was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(VmCount) || string.IsNullOrEmpty(parameters[VmCount]))
            {
                return new BadRequestObjectResult("VmCount was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(VmSku) || string.IsNullOrEmpty(parameters[VmSku]))
            {
                return new BadRequestObjectResult("Vm SKU was missing in the query parameters.");
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
                AzureEnvironment.AzureUSGovernment);
            var client = RestClient
                .Configure()
                .WithEnvironment(AzureEnvironment.AzureUSGovernment)
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
                    $"&cloudName=AzureUSGovernment" +
                    $"&tenantId={tenantId}" +
                    $"&subscriptionId={subscriptionId}" +
                    $"&resourceGroup={resourceGroupName}" +
                    $"&location={location}" +
                    $"&vmSku={virtualMachineSize}" +
                    $"&vmName={vmName}" +
                    $"&platformFaultDomainCount=1";
#else
                var createVmUri =
                    _configuration["DhmFunctionUri"] +
                    $"&token={token}" +
                    $"&cloudName=AzureUSGovernment" +
                    $"&tenantId={tenantId}" +
                    $"&subscriptionId={subscriptionId}" +
                    $"&resourceGroup={resourceGroupName}" +
                    $"&location={location}" +
                    $"&vmSku={virtualMachineSize}" +
                    $"&vmName={vmName}" +
                    $"&platformFaultDomainCount=1";
#endif
                var httpContent = new StringContent(JsonConvert.SerializeObject(virtualMachine), Encoding.UTF8, "application/json");
                inputDictionary[createVmUri] = httpContent;
            }

            foreach (var item in inputDictionary)
            {
                taskList.Add(_httpClient.PostAsync(item.Key, item.Value));
            }
            
            return new OkObjectResult($"VM provisioning kicked off successfully for {numVirtualMachines} VMs - exiting.");
        }
    }
}
