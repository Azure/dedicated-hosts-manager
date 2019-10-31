using Newtonsoft.Json;

namespace DedicatedHostsManager.ComputeClient
{
    /// <summary>
    /// ARM metadata schema.
    /// </summary>
    public class ArmMetadata
    {
        /// <summary>
        /// Gets or sets the Portal endpoint.
        /// </summary>
        public string Portal { get; set; }

        /// <summary>
        /// Gets or sets the authentication endpoint details.
        /// </summary>
        public AuthEndpoint Authentication { get; set; }

        /// <summary>
        /// Gets or sets the Media endpoint.
        /// </summary>
        public string Media { get; set; }

        /// <summary>
        /// Gets or sets the GraphAudience endpoint.
        /// </summary>
        public string GraphAudience { get; set; }

        /// <summary>
        /// Gets or sets the Graph endpoint.
        /// </summary>
        public string Graph { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Suffixes endpoint details.
        /// </summary>
        public SuffixEndpoints Suffixes { get; set; }

        /// <summary>
        /// Gets or sets the Batch endpoint.
        /// </summary>
        public string Batch { get; set; }

        /// <summary>
        /// Gets or sets the ResourceManager endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string ResourceManager { get; set; }

        /// <summary>
        /// Gets or sets the VMImageAliasDoc endpoint.
        /// </summary>
        public string VmImageAliasDoc { get; set; }

        /// <summary>
        /// Gets or sets the ActiveDirectoryDataLake endpoint.
        /// </summary>
        public string ActiveDirectoryDataLake { get; set; }

        /// <summary>
        /// Gets or sets the SQLManagement endpoint.
        /// </summary>
        public string SqlManagement { get; set; }

        /// <summary>
        /// Gets or sets the Gallery endpoint.
        /// </summary>
        public string Gallery { get; set; }
    }
}