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
    /// <summary>
    /// Initializes the compute client (from ARM metadata) to use with Dedicated Host calls.
    /// </summary>
    public class DhmComputeClient : IDhmComputeClient
    {
        private static IComputeManagementClient _computeManagementClient;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DedicatedHostStateManager.DedicatedHostStateManager> _logger;

        /// <summary>
        /// Initializes a new instance of the DhmComputeClient class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="httpClientFactory">To create an HTTP client.</param>
        public DhmComputeClient(
            IConfiguration configuration, 
            ILogger<DedicatedHostStateManager.DedicatedHostStateManager> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        /// <summary>
        /// Get an instance of the Dedicated Host compute client.
        /// </summary>
        /// <param name="subscriptionId">Subscription ID.</param>
        /// <param name="azureCredentials">Credentials used for Azure authentication.</param>
        /// <param name="azureEnvironment">Azure cloud.</param>
        
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

        /// <summary>
        /// Gets Azure Resource Endpoint for an Azure cloud.
        /// </summary>
        /// <param name="azureEnvironment">Azure cloud.</param>
        
        private async Task<Uri> GetResourceManagerEndpoint(AzureEnvironment azureEnvironment)
        {
            var armMetadataRetryCount = int.Parse(_configuration["GetArmMetadataRetryCount"]);
            HttpResponseMessage armResponseMessage = null;
            await Policy
                .Handle<HttpRequestException>()
                .WaitAndRetryAsync(
                    armMetadataRetryCount, 
                    r => TimeSpan.FromSeconds(2 * r),
                    (ex, ts, r) =>
                        _logger.LogInformation(
                            $"Could not retrieve ARM metadata. Attempt #{r}/{armMetadataRetryCount}. Will try again in {ts.TotalSeconds} seconds. Exception={ex}"))
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
                .First(c => c.ResourceManager.TrimEnd('/').Equals(azureEnvironment.ResourceManagerEndpoint.TrimEnd('/'),
                    StringComparison.InvariantCultureIgnoreCase))
                .ResourceManager);
        }
    }
}