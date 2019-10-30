using System;
using System.Diagnostics;
using System.Threading.Tasks;
using DedicatedHostsManager.DedicatedHostEngine;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace DedicatedHostsManager
{
    public class DedicatedHostsFunction
    {
        private readonly IDedicatedHostEngine _dedicatedHostEngine;
        private readonly IConfiguration _configuration;

        public DedicatedHostsFunction(IDedicatedHostEngine dedicatedHostEngine, IConfiguration configuration)
        {
            _dedicatedHostEngine = dedicatedHostEngine;
            _configuration = configuration;
        }

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
    }
}
