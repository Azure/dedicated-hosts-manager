using DedicatedHostsManager.DedicatedHostEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace DedicatedHostsManager
{
    /// <summary>
    /// Functions to expose Dedicated Host Manager library.
    /// </summary>
    public class DedicatedHostsFunction
    {
        private readonly IDedicatedHostEngine _dedicatedHostEngine;
       
        /// <summary>
        /// Initialization.
        /// </summary>
        /// <param name="dedicatedHostEngine">Dedicated Host Engine.</param>
        public DedicatedHostsFunction(IDedicatedHostEngine dedicatedHostEngine)
        {
            _dedicatedHostEngine = dedicatedHostEngine;
        }

        /// <summary>
        /// Function to create a VM on a Dedicated Host.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <param name="log">Logger.</param>
        /// <param name="context">Function execution context.</param>
        [FunctionName("CreateVm")]
        public async Task<IActionResult> CreateVmOnDedicatedHost(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            var parameters = req.GetQueryParameterDictionary();

            if (!parameters.ContainsKey(Constants.CloudName) || string.IsNullOrEmpty(parameters[Constants.CloudName]))
            {
                return new BadRequestObjectResult("CloudName was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.TenantId) || string.IsNullOrEmpty(parameters[Constants.TenantId]))
            {
                return new BadRequestObjectResult("TenantId was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.Token) || string.IsNullOrEmpty(parameters[Constants.Token]))
            {
                return new BadRequestObjectResult("Token was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.SubscriptionId) ||
                string.IsNullOrEmpty(parameters[Constants.SubscriptionId]))
            {
                return new BadRequestObjectResult("Subscription ID was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.ResourceGroup) ||
                string.IsNullOrEmpty(parameters[Constants.ResourceGroup]))
            {
                return new BadRequestObjectResult("Resource group was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.DedicatedHostGroupName) ||
                string.IsNullOrEmpty(parameters[Constants.DedicatedHostGroupName]))
            {
                return new BadRequestObjectResult("Dedicated host group was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.Location) || string.IsNullOrEmpty(parameters[Constants.Location]))
            {
                return new BadRequestObjectResult("Location was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.VmSku) || string.IsNullOrEmpty(parameters[Constants.VmSku]))
            {
                return new BadRequestObjectResult("VmSku was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.VmName) || string.IsNullOrEmpty(parameters[Constants.VmName]))
            {
                return new BadRequestObjectResult("VmName was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.PlatformFaultDomainCount)
                || string.IsNullOrEmpty(parameters[Constants.PlatformFaultDomainCount])
                || !int.TryParse(parameters[Constants.PlatformFaultDomainCount], out var platformFaultDomainCount))
            {
                return new BadRequestObjectResult("PlatformFaultDomainCount was missing in the query parameters.");
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var requestBody = await req.ReadAsStringAsync();
                var virtualMachine = JsonConvert.DeserializeObject<VirtualMachine>(requestBody);
                var cloudName = parameters[Constants.CloudName];
                AzureEnvironment azureEnvironment = null;
                if (cloudName.Equals("AzureGlobalCloud", StringComparison.InvariantCultureIgnoreCase)
                    || cloudName.Equals("AzureCloud", StringComparison.InvariantCultureIgnoreCase))
                {
                    azureEnvironment = AzureEnvironment.AzureGlobalCloud;
                }
                else
                {
                    azureEnvironment = AzureEnvironment.FromName(cloudName);
                }

                var createVmResponse = await _dedicatedHostEngine.CreateVmOnDedicatedHost(
                    parameters[Constants.Token],
                    azureEnvironment,
                    parameters[Constants.TenantId],
                    parameters[Constants.SubscriptionId],
                    parameters[Constants.ResourceGroup],
                    parameters[Constants.DedicatedHostGroupName],
                    parameters[Constants.VmSku],
                    parameters[Constants.VmName],
                    Region.Create(parameters[Constants.Location]),
                    virtualMachine);

                log.LogInformation(
                    $"CreateVm: Took {sw.Elapsed.TotalSeconds}s to create {parameters[Constants.VmName]}");
                log.LogMetric("VmCreationTimeSecondsMetric", sw.Elapsed.TotalSeconds);
                log.LogMetric("VmCreationSuccessCountMetric", 1);
                return new OkObjectResult(createVmResponse);
            }
            catch (Exception exception)
            {
                log.LogError(
                    $"CreateVm: Error creating {parameters[Constants.VmName]}, time spent: {sw.Elapsed.TotalSeconds}s, Exception: {exception}");
                log.LogMetric("VmCreationFailureCountMetric", 1);
                return new BadRequestObjectResult(exception.ToString());
            }
        }

        /// <summary>
        /// Function to delete a VM on a Dedicated Host, and the Host itself as well,
        /// when the last VM running on the Host is deleted.
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <param name="log">Logger.</param>
        /// <param name="context">Function execution context.</param>
        [FunctionName("DeleteVm")]
        public async Task<IActionResult> DeleteVmFromDedicatedHost(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]
            HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            var parameters = req.GetQueryParameterDictionary();

            if (!parameters.ContainsKey(Constants.CloudName) || string.IsNullOrEmpty(parameters[Constants.CloudName]))
            {
                return new BadRequestObjectResult("CloudName was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.TenantId) || string.IsNullOrEmpty(parameters[Constants.TenantId]))
            {
                return new BadRequestObjectResult("TenantId was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.Token) || string.IsNullOrEmpty(parameters[Constants.Token]))
            {
                return new BadRequestObjectResult("Token was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.SubscriptionId) ||
                string.IsNullOrEmpty(parameters[Constants.SubscriptionId]))
            {
                return new BadRequestObjectResult("Subscription ID was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.ResourceGroup) ||
                string.IsNullOrEmpty(parameters[Constants.ResourceGroup]))
            {
                return new BadRequestObjectResult("Resource group was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.DedicatedHostGroupName) ||
                string.IsNullOrEmpty(parameters[Constants.DedicatedHostGroupName]))
            {
                return new BadRequestObjectResult("Dedicated host group was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.VmName) || string.IsNullOrEmpty(parameters[Constants.VmName]))
            {
                return new BadRequestObjectResult("VmName was missing in the query parameters.");
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var cloudName = parameters[Constants.CloudName];
                AzureEnvironment azureEnvironment = null;
                if (cloudName.Equals("AzureGlobalCloud", StringComparison.InvariantCultureIgnoreCase)
                    || cloudName.Equals("AzureCloud", StringComparison.InvariantCultureIgnoreCase))
                {
                    azureEnvironment = AzureEnvironment.AzureGlobalCloud;
                }
                else
                {
                    azureEnvironment = AzureEnvironment.FromName(cloudName);
                }

                await _dedicatedHostEngine.DeleteVmOnDedicatedHost(
                    parameters[Constants.Token],
                    azureEnvironment,
                    parameters[Constants.TenantId],
                    parameters[Constants.SubscriptionId],
                    parameters[Constants.ResourceGroup],
                    parameters[Constants.DedicatedHostGroupName],
                    parameters[Constants.VmName]);

                log.LogInformation(
                    $"DeleteVm: Took {sw.Elapsed.TotalSeconds}s to delete {parameters[Constants.VmName]}");
                log.LogMetric("VmDeletionTimeSecondsMetric", sw.Elapsed.TotalSeconds);
                log.LogMetric("VmDeletionSuccessCountMetric", 1);
                return new OkObjectResult($"Deleted {parameters[Constants.VmName]}.");
            }
            catch (Exception exception)
            {
                log.LogError(
                    $"DeleteVm: Error deleting {parameters[Constants.VmName]}, time spent: {sw.Elapsed.TotalSeconds}s, Exception: {exception}");
                log.LogMetric("VmDeletionFailureCountMetric", 1);
                return new BadRequestObjectResult(exception.ToString());
            }
        }


        /// <summary>
        /// This call to the Dedicated Host Manager library will prepare the host group by creating sufficient number of dedicated hosts so that a future call to VM or VMSS creation will be successful. 
        /// </summary>
        /// <param name="req">HTTP request.</param>
        /// <param name="log">Logger.</param>
        /// <param name="context">Function execution context.</param>
        [FunctionName("PrepareDedicatedHostGroup")]         // TODO: SJP - Why Pascal case ? 
        public async Task<IActionResult> PrepareDedicatedHostGroup(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] // TODO: SJP - Why both Get/Post
            HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            var parameters = req.GetQueryParameterDictionary();

            if (!parameters.ContainsKey(Constants.CloudName) || string.IsNullOrEmpty(parameters[Constants.CloudName]))
            {
                return new BadRequestObjectResult("CloudName was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.TenantId) || string.IsNullOrEmpty(parameters[Constants.TenantId]))
            {
                return new BadRequestObjectResult("TenantId was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.Token) || string.IsNullOrEmpty(parameters[Constants.Token]))
            {
                return new BadRequestObjectResult("Token was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.SubscriptionId) ||
                string.IsNullOrEmpty(parameters[Constants.SubscriptionId]))
            {
                return new BadRequestObjectResult("Subscription ID was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.ResourceGroup) ||
                string.IsNullOrEmpty(parameters[Constants.ResourceGroup]))
            {
                return new BadRequestObjectResult("Resource group was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.DedicatedHostGroupName) ||
                string.IsNullOrEmpty(parameters[Constants.DedicatedHostGroupName]))
            {
                return new BadRequestObjectResult("Dedicated host group was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.VmSku) || string.IsNullOrEmpty(parameters[Constants.VmSku]))
            {
                return new BadRequestObjectResult("VmSku was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.VmCount) || !int.TryParse(parameters[Constants.VmCount], out int vmCount))
            {
                return new BadRequestObjectResult("VmCount was missing in the query parameters or not numeric value");
            }

            int? platformFaultDomain;
            if (!parameters.ContainsKey(Constants.PlatformFaultDomain))
            {
                platformFaultDomain = null;
            }
            else if (int.TryParse(parameters[Constants.PlatformFaultDomain], out int parsedFD) && parsedFD >= 0 && parsedFD <= 2)
            {
                platformFaultDomain = parsedFD;
            }
            else
            {
                return new BadRequestObjectResult("PlatformFaultDomain if specificed must be a value between 0-2");
            }

            var sw = Stopwatch.StartNew();
            try
            {
                var requestBody = await req.ReadAsStringAsync();
                var virtualMachine = JsonConvert.DeserializeObject<VirtualMachine>(requestBody);
                var cloudName = parameters[Constants.CloudName];
                AzureEnvironment azureEnvironment = null;
                if (cloudName.Equals("AzureGlobalCloud", StringComparison.InvariantCultureIgnoreCase)
                    || cloudName.Equals("AzureCloud", StringComparison.InvariantCultureIgnoreCase))
                {
                    azureEnvironment = AzureEnvironment.AzureGlobalCloud;
                }
                else
                {
                    azureEnvironment = AzureEnvironment.FromName(cloudName);
                } 

                var prepareDedicatedHostGroupResponse = await _dedicatedHostEngine.PrepareDedicatedHostGroup(
                    parameters[Constants.Token],
                    azureEnvironment,
                    parameters[Constants.TenantId],
                    parameters[Constants.SubscriptionId],
                    parameters[Constants.ResourceGroup], 
                    parameters[Constants.DedicatedHostGroupName],
                    parameters[Constants.VmSku],
                    vmCount,
                    platformFaultDomain);

                log.LogInformation(
                    $"PrepareDedicatedHostGroup: Took {sw.Elapsed.TotalSeconds}s");
                log.LogMetric("PrepareDedicatedHostGroupTimeSecondsMetric", sw.Elapsed.TotalSeconds);
                log.LogMetric("PrepareDedicatedHostGroupSuccessCountMetric", 1);
                 
                return new OkObjectResult(prepareDedicatedHostGroupResponse);
            }
            catch (Exception exception)
            {
                log.LogError(
                    $"PrepareDedicatedHostGroup: Error creating {parameters[Constants.DedicatedHostGroupName]}, time spent: {sw.Elapsed.TotalSeconds}s, Exception: {exception}");
                log.LogMetric("PrepareDedicatedHostGroupFailureCountMetric", 1);
                return new BadRequestObjectResult(exception.ToString());
            }
        }
    }
}
