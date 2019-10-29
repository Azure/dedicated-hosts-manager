using DedicatedHostsManager.Cache;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DedicatedHostsManager.ComputeClient
{
    public class DhmComputeClient : IDhmComputeClient
    {
        private static IComputeManagementClient _computeManagementClient;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<CacheProvider> _logger;

        public DhmComputeClient(
            IConfiguration configuration, 
            ILogger<CacheProvider> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<IComputeManagementClient> GetComputeManagementClient(
            string subscriptionId,
            AzureCredentials azureCredentials,
            AzureEnvironment azureEnvironment)
        {
            var baseUri = await GetResourceManagerEndpoint(azureEnvironment);

            return _computeManagementClient ?? (_computeManagementClient = new ComputeManagementClient(azureCredentials)
            {
                SubscriptionId = subscriptionId,
                BaseUri = baseUri,
                LongRunningOperationRetryTimeout = int.Parse(_configuration["ComputeClientLongRunningOperationRetryTimeoutSeconds"]),
                HttpClient = { Timeout = TimeSpan.FromMinutes(int.Parse(_configuration["ComputeClientHttpTimeoutMin"])) }
            });
        }

        private async Task<Uri> GetResourceManagerEndpoint(AzureEnvironment azureEnvironment)
        {
            HttpResponseMessage armResponseMessage = null;
            await Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    int.Parse(_configuration["GetArmMetadataRetryCount"]), 
                    r => TimeSpan.FromSeconds(2 * r),
                    (ex, ts, r) =>
                        _logger.LogInformation(
                            $"Could not retrieve ARM metadata. Attempt #{r}/3. Will try again in {ts.TotalSeconds} seconds. Exception={ex}"))
                .ExecuteAsync(async () =>
                    {
                        armResponseMessage = await _httpClient.GetAsync(_configuration["GetArmMetadataUrl"]);
                    });

            if (armResponseMessage == null || armResponseMessage?.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Could not retrieve ARM metadata, compute management client cannot be initialized.");
            }

            var armMetadataContent = await armResponseMessage.Content?.ReadAsStringAsync();
            if (string.IsNullOrEmpty(armMetadataContent))
            {
                throw new Exception("Could not read ARM metadata, compute management client cannot be initialized.");
            }

            var armMetadata = JsonConvert.DeserializeObject<List<ArmMetadata>>(armMetadataContent);
            return new Uri(armMetadata
                .First(c => c.Name.Equals(azureEnvironment.Name, StringComparison.InvariantCultureIgnoreCase))
                .ResourceManager);
        }
    }
}