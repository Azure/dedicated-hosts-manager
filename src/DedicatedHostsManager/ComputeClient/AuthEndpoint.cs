using Newtonsoft.Json;

namespace DedicatedHostsManager.ComputeClient
{
    public class AuthEndpoint
    {
        /// <summary>
        /// Gets or sets the authentication endpoint.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string LoginEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the resource manager resource identifier.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string[] Audiences { get; set; }

        /// <summary>
        /// Gets or sets the tenant.
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string Tenant { get; set; }

        /// <summary>
        /// Gets or sets the identity provider type (AAD or ADFS).
        /// </summary>
        [JsonProperty(Required = Required.Default)]
        public string IdentityProvider { get; set; }
    }
}