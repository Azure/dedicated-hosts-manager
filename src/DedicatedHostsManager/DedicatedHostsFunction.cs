using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace DedicatedHosts
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

        //[FunctionName("CreateDhg")]
        public async Task<IActionResult> CreateDedicatedHostGroup(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req, 
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation($"CreateDhg: Started at {DateTime.Now}");
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

            if (!parameters.ContainsKey(Constants.SubscriptionId) || string.IsNullOrEmpty(parameters[Constants.SubscriptionId]))
            {
                return new BadRequestObjectResult("Subscription ID was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.ResourceGroup) || string.IsNullOrEmpty(parameters[Constants.ResourceGroup]))
            {
                return new BadRequestObjectResult("Resource group was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.DedicatedHostGroupName) || string.IsNullOrEmpty(parameters[Constants.DedicatedHostGroupName]))
            {
                return new BadRequestObjectResult("Dedicated Host Group name was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.PlatformFaultDomainCount) 
                || string.IsNullOrEmpty(parameters[Constants.PlatformFaultDomainCount])
                || !int.TryParse(parameters[Constants.PlatformFaultDomainCount], out var platformFaultDomainCount))
            {
                return new BadRequestObjectResult("PlatformFaultDomainCount was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.Location) || string.IsNullOrEmpty(parameters[Constants.Location]))
            {
                return new BadRequestObjectResult("Location was missing in the query parameters.");
            }

            try
            {
                var dedicatedHostGroup = await _dedicatedHostEngine.CreateDedicatedHostGroup(
                    parameters[Constants.Token],
                    parameters[Constants.CloudName],
                    parameters[Constants.TenantId],
                    parameters[Constants.SubscriptionId],
                    parameters[Constants.ResourceGroup],
                    Constants.DedicatedHostGroupName,
                    null,
                    platformFaultDomainCount,
                    parameters[Constants.Location]);

                log.LogInformation($"CreateDhg: Ended at {DateTime.Now}");
                return new OkObjectResult(dedicatedHostGroup);
            }
            catch (Exception exception)
            {
                log.LogError($"CreateDhg: Exception - {exception}");
                return new BadRequestObjectResult(exception.ToString());
            }
        }

        //[FunctionName("CreateDh")]
        public async Task<IActionResult> CreateDedicatedHost(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation($"CreateDh: Started at {DateTime.Now}");
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

            if (!parameters.ContainsKey(Constants.SubscriptionId) || string.IsNullOrEmpty(parameters[Constants.SubscriptionId]))
            {
                return new BadRequestObjectResult("Subscription ID was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.ResourceGroup) || string.IsNullOrEmpty(parameters[Constants.ResourceGroup]))
            {
                return new BadRequestObjectResult("Resource group was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.DedicatedHostGroupName) || string.IsNullOrEmpty(parameters[Constants.DedicatedHostGroupName]))
            {
                return new BadRequestObjectResult("Dedicated Host Group name was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.DedicatedHostName) || string.IsNullOrEmpty(parameters[Constants.DedicatedHostName]))
            {
                return new BadRequestObjectResult("Dedicated host name was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.Location) || string.IsNullOrEmpty(parameters[Constants.Location]))
            {
                return new BadRequestObjectResult("Location was missing in the query parameters.");
            }

            try
            {
                var dedicatedHost = await _dedicatedHostEngine.CreateDedicatedHost(
                    parameters[Constants.Token],
                    parameters[Constants.CloudName],
                    parameters[Constants.TenantId],
                    parameters[Constants.SubscriptionId],
                    parameters[Constants.ResourceGroup],
                    parameters[Constants.DedicatedHostGroupName],
                    parameters[Constants.DedicatedHostName],
                    parameters[Constants.DedicatedHostSku],
                    parameters[Constants.Location]);

                log.LogInformation($"CreateDh: Ended at {DateTime.Now}");
                return new OkObjectResult(dedicatedHost);
            }
            catch (Exception exception)
            {
                log.LogError($"CreateDh: Exception - {exception}");
                return new BadRequestObjectResult(exception.ToString());
            }
        }

        [FunctionName("CreateVm")]
        public async Task<IActionResult> CreateVmOnDedicatedHost(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequest req,
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

            if (!parameters.ContainsKey(Constants.SubscriptionId) || string.IsNullOrEmpty(parameters[Constants.SubscriptionId]))
            {
                return new BadRequestObjectResult("Subscription ID was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.ResourceGroup) || string.IsNullOrEmpty(parameters[Constants.ResourceGroup]))
            {
                return new BadRequestObjectResult("Resource group was missing in the query parameters.");
            }

            if (!parameters.ContainsKey(Constants.Location) || string.IsNullOrEmpty(parameters[Constants.Location]))
            {
                return new BadRequestObjectResult("Location was missing in the query parameters.");
            }

            // TODO: remove vmsku, vnname and FD params
            //
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

                // TODO: wrap up all parameters (sub, RG, etc.) in the config object
                // 
                var dedicatedHostCreateVmResponse = await _dedicatedHostEngine.CreateVmOnDedicatedHost(
                    parameters[Constants.Token],
                    parameters[Constants.CloudName],
                    parameters[Constants.TenantId],
                    parameters[Constants.SubscriptionId],
                    parameters[Constants.ResourceGroup],
                    Constants.DedicatedHostGroupName,
                    parameters[Constants.VmSku],
                    parameters[Constants.VmName],
                    Region.Create(parameters[Constants.Location]),
                    virtualMachine);

                log.LogInformation($"CreateVm: Took {sw.Elapsed.TotalSeconds}s to create {parameters[Constants.VmName]}");
                log.LogMetric("VmCreationTimeSecondsMetric", sw.Elapsed.TotalSeconds);
                log.LogMetric("VmCreationSuccessCountMetric", 1);
                return new OkObjectResult(dedicatedHostCreateVmResponse);
            }
            catch (Exception exception)
            {
                log.LogError($"CreateVm: Error creating {parameters[Constants.VmName]}, time spent: {sw.Elapsed.TotalSeconds}s, Exception: {exception}");
                log.LogMetric("VmCreationFailureCountMetric", 1);
                return new BadRequestObjectResult(exception.ToString());
            }
        }
    }
}
