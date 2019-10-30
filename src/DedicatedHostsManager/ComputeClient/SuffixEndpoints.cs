using Newtonsoft.Json;

namespace DedicatedHostsManager.ComputeClient
{
    /// <summary>
    /// ARM suffix endpoints schema.
    /// </summary>
    public class SuffixEndpoints
    {
        /// <summary>
        /// Gets or sets the AzureDataLakeStoreFileSystem endpoint.
        /// </summary>        
        [JsonProperty(Required = Required.Default)]
        public string AzureDataLakeStoreFileSystem { get; set; }

        /// <summary>
        /// Gets or sets the ACRLoginServer endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string AcrLoginServer { get; set; }

        /// <summary>
        /// Gets or sets the SQLServerHostname endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string SqlServerHostname { get; set; }

        /// <summary>
        /// Gets or sets the AzureDataLakeAnalyticsCatalogAndJob endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string AzureDataLakeAnalyticsCatalogAndJob { get; set; }

        /// <summary>
        /// Gets or sets the KeyVaultDNS endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string KeyVaultDns { get; set; }

        /// <summary>
        /// Gets or sets the Storage endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string Storage { get; set; }
    }
}