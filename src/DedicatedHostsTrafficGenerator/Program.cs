using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Rest;
using Newtonsoft.Json;
using ScaleTestHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DedicatedHostsTrafficGenerator
{
    internal class Program
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public static async Task Main(string[] args)
        {
            var config = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddUserSecrets<Program>()
             .Build();

            var resourceGroupName = config["ResourceGroupName"];
            var authEndpoint = config["AuthEndpoint"];
            var azureRmEndpoint = config["AzureRmEndpoint"];
            var location = config["Location"];
            var virtualMachineSize = config["VirtualMachineSize"];
            var numVirtualMachines = int.Parse(config["NumVirtualMachines"]);
            var tenantId = config["TenantId"];
            var clientId = config["ClientId"];
            var clientSecret = config["FairfaxClientSecret"];
            var subscriptionId = config["SubscriptionId"];

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
                BaseUri = new Uri("https://management.usgovcloudapi.net/"),
                LongRunningOperationRetryTimeout = 5
            };
         
            var resourceGroup = azure.ResourceGroups.Define(resourceGroupName)
                .WithRegion(location)
                .Create();

            var newDedicatedHostGroup = new DedicatedHostGroup()
            {
                Location = location,
                PlatformFaultDomainCount = 1
            };

            await computeManagementClient.DedicatedHostGroups.CreateOrUpdateAsync(
                resourceGroupName,
                "citrix-dhg",
                newDedicatedHostGroup);

            var taskList = new List<Task<HttpResponseMessage>>();
            var inputDictionary = new Dictionary<string, StringContent>();
            for (var i = 0; i < numVirtualMachines; i++)
            {
                var vmName = $"vm{i}-{new Random().Next(1,10000)}";
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
                    _config["DhmFunctionUri"] +
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
                taskList.Add(HttpClient.PostAsync(item.Key, item.Value));
            }

            var results = await Task.WhenAll(taskList);
            var outputDictionary = new Dictionary<string, string>();
            foreach (var result in results)
            {
                outputDictionary[result.RequestMessage.RequestUri.ToString()] =
                    await result.Content.ReadAsStringAsync();
            }

            // list all virtual machines in a RG
            var vmList = new List<VirtualMachine>();
            var vmListResponse = await computeManagementClient.VirtualMachines.ListAllAsync();
            vmList.AddRange(vmListResponse.ToList());
            var nextLink = vmListResponse.NextPageLink;

            // TODO: fortify?
            while (!string.IsNullOrEmpty(nextLink))
            {
                vmListResponse = await computeManagementClient.VirtualMachines.ListAllNextAsync(nextLink);
                vmList.AddRange(vmListResponse.ToList());
                nextLink = vmListResponse.NextPageLink;
            }

            var vmGroups = vmList.GroupBy(v => v.ProvisioningState);
            foreach (var v in vmGroups)
            {
                Console.WriteLine($"Provisioning state: {v.Key}, VM Count: {v.Count()}");
            }
        }
    }
}
