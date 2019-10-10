﻿using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest.Azure;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DedicatedHosts
{
    public interface IDedicatedHostEngine
    {
        Task<AzureOperationResponse<DedicatedHostGroup>> CreateDedicatedHostGroup(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string dhgName,
            string azName,
            int platformFaultDomainCount,
            string location);

        Task<AzureOperationResponse<DedicatedHost>> CreateDedicatedHost(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string dhgName,
            string dhName,
            string dhSku,
            string location);

        Task<AzureOperationResponse<VirtualMachine>> CreateVmOnDedicatedHost(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string dhgName,
            string vmSku,
            string vmName,
            Region region,
            VirtualMachine virtualMachine);

        Task<string> GetDedicatedHostForVmPlacement(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName,
            string requiredVmSize,
            string vmName,
            string location);

        Task<IList<DedicatedHost>> ListDedicatedHosts(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName);

        Task<IList<DedicatedHostGroup>> ListDedicatedHostGroups(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId);

        Task<IAzureOperationResponse> DeleteDedicatedHostGroup(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName);

        Task<IAzureOperationResponse> DeleteDedicatedHostGroup(
            string token,
            string cloudName,
            string tenantId,
            string subscriptionId,
            string resourceGroup,
            string hostGroupName,
            string dhName);
    }
}