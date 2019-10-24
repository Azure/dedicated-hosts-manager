using System;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;

namespace DedicatedHostsManager.Helpers
{
    public static class DedicatedHostHelpers
    {
        private static ComputeManagementClient _computeManagementClient;

        // TODO: refactor hard coded URL; need to use this URL for non-public clouds
        //
        public static ComputeManagementClient ComputeManagementClient(string subscriptionId, AzureCredentials azureCredentials)
        {
            return _computeManagementClient ?? (_computeManagementClient = new ComputeManagementClient(azureCredentials)
            {
                SubscriptionId = subscriptionId,
                BaseUri = new Uri("https://management.usgovcloudapi.net/"),
                LongRunningOperationRetryTimeout = 5,
                HttpClient = {Timeout = TimeSpan.FromMinutes(30)}
            });
        }
    }
}