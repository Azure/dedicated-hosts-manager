using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Compute.Models;

namespace DedicatedHostsManager.DedicatedHostEngine
{
    public interface IDedicatedHostSelector
    {
        Task<string> SelectDedicatedHost(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName,
            string requiredVmSize);

        Task<IList<DedicatedHost>> ListDedicatedHosts(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName);
    }
}