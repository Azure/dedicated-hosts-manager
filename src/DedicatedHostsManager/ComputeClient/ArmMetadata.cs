using Newtonsoft.Json;

namespace DedicatedHostsManager.ComputeClient
{
    public class ArmMetadata
    {
        /// <summary>
        /// Gets or sets the Portal endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Portal { get; set; }

        /// <summary>
        /// Gets or sets the authentication endpoint details.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public AuthEndpoint Authentication { get; set; }

        /// <summary>
        /// Gets or sets the Media endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Media { get; set; }

        /// <summary>
        /// Gets or sets the GraphAudience endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string GraphAudience { get; set; }

        /// <summary>
        /// Gets or sets the Graph endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Graph { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Suffixes endpoint details.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public SuffixEndpoints Suffixes { get; set; }

        /// <summary>
        /// Gets or sets the Batch endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Batch { get; set; }

        /// <summary>
        /// Gets or sets the ResourceManager endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ResourceManager { get; set; }

        /// <summary>
        /// Gets or sets the VMImageAliasDoc endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string VmImageAliasDoc { get; set; }

        /// <summary>
        /// Gets or sets the ActiveDirectoryDataLake endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string ActiveDirectoryDataLake { get; set; }

        /// <summary>
        /// Gets or sets the SQLManagement endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string SqlManagement { get; set; }

        /// <summary>
        /// Gets or sets the Gallery endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Gallery { get; set; }
    }
}