using Microsoft.Azure.Management.Compute.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DedicatedHosts
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
            string requiredVmSize,
            IList<DedicatedHost> dedicatedHostList);
    }
}