using Newtonsoft.Json;

namespace DedicatedHostsManager.ComputeClient
{
    /// <summary>
    /// ARM authentication schema.
    /// </summary>
    public class AuthEndpoint
    {
        /// <summary>
        /// Gets or sets the authentication endpoint.
        /// </summary>
        public string LoginEndpoint { get; set; }

        /// <summary>
        /// Gets or sets the resource manager resource identifier.
        /// </summary>
        public string[] Audiences { get; set; }

        /// <summary>
        /// Gets or sets the tenant.
        /// </summary>
        public string Tenant { get; set; }

        /// <summary>
        /// Gets or sets the identity provider.
        /// </summary>
        public string IdentityProvider { get; set; }
    }
}