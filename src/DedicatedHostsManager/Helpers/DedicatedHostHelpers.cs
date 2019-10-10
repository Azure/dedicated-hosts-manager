using System;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace DedicatedHosts.Helpers
{
    public static class DedicatedHostHelpers
    {
        // TODO: refactor hard coded URL; need to use this URL for non-public clouds
        //
        public static ComputeManagementClient ComputeManagementClient(string subscriptionId, AzureCredentials azureCredentials)
        {
            return new ComputeManagementClient(azureCredentials)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri("https://management.usgovcloudapi.net/"),
                LongRunningOperationRetryTimeout = 5
            };
        }
    }
}