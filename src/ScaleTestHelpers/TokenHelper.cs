using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace ScaleTestHelpers
{
    public class TokenHelper
    {
        public static async Task<string> GetToken(
            string authEndpoint,
            string azureRmEndpoint,
            string tenantId,
            string clientId,
            string clientSecret)
        {
            var authContext = new AuthenticationContext((new Uri(new Uri(authEndpoint), tenantId)).ToString());
            var credential = new ClientCredential(clientId, clientSecret);
            var token = await authContext.AcquireTokenAsync(azureRmEndpoint, credential);
            return token?.AccessToken;
        }
    }
}