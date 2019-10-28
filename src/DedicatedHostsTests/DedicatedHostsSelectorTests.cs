using DedicatedHostsManager.Cache;
using DedicatedHostsManager.ComputeClient;
using DedicatedHostsManager.DedicatedHostEngine;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace DedicatedHostsManagerTests
{
    public class DedicatedHostsSelectorTests
    {
        private const string Token = "test-Token";
        private const string CloudName = "AzureUSGovernment";
        private const string TenantId = "test-teanant-id";
        private const string SubscriptionId = "test-sub";
        private const string ResourceGroup = "test-rg";
        private const string HostGroupName = "test-dhg";
        private const string VmSize = "Standard_D64s_v3";

        [Fact]
        public async Task SelectDedicatedHostTest()
        {
            const string expectedHostId = "/subscriptions/6e412d70-9128-48a7-97b4-04e5bd35cefc/resourceGroups/63296244-ce2c-46d8-bc36-3e558792fbee/providers/Microsoft.Compute/hostGroups/citrix-dhg/hosts/20887a6e-0866-4bae-82b7-880839d9e76b";
            var loggerMock = new Mock<ILogger<DedicatedHostSelector>>();
            var cacheProviderMock = new Mock<ICacheProvider>();
            var dhmComputeClientMock = new Mock<IDhmComputeClient>();
            cacheProviderMock.Setup(s => s.KeyExists(It.IsAny<string>())).Returns(false);
            var dedicatedHostList =
                JsonConvert.DeserializeObject<List<DedicatedHost>>(
                    File.ReadAllText(@"TestData\dedicatedHostsInput1.json"));
            var mockDedicatedHostResponse1 = new AzureOperationResponse<DedicatedHost>
            {
                Body = dedicatedHostList.First(d => d.Name.Equals("dh1", StringComparison.InvariantCultureIgnoreCase))
            };
            var mockDedicatedHostResponse2 = new AzureOperationResponse<DedicatedHost>
            {
                Body = dedicatedHostList.First(d => d.Name.Equals("dh2", StringComparison.InvariantCultureIgnoreCase))
            };
            var computeManagementClientMock = new Mock<IComputeManagementClient>();
            computeManagementClientMock
                .Setup(
                    s => s.DedicatedHosts.GetWithHttpMessagesAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        "dh1",
                        InstanceViewTypes.InstanceView,
                        null,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockDedicatedHostResponse1);
            computeManagementClientMock
                .Setup(
                    s => s.DedicatedHosts.GetWithHttpMessagesAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        "dh2",
                        InstanceViewTypes.InstanceView,
                        null,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockDedicatedHostResponse2);
            dhmComputeClientMock.Setup(s =>
                    s.GetComputeManagementClient(
                        It.IsAny<string>(),
                        It.IsAny<AzureCredentials>(),
                        It.IsAny<AzureEnvironment>()))
                .ReturnsAsync(computeManagementClientMock.Object);

            var dedicatedHostSelector = new DedicatedHostSelectorTest(loggerMock.Object, cacheProviderMock.Object, dhmComputeClientMock.Object);
            var actualHostId = await dedicatedHostSelector.SelectDedicatedHost(
                Token,
                CloudName,
                TenantId,
                SubscriptionId,
                ResourceGroup,
                HostGroupName,
                VmSize);

            Assert.Equal(actualHostId, expectedHostId);
        }

        private class DedicatedHostSelectorTest : DedicatedHostSelector
        {
            public DedicatedHostSelectorTest(
                ILogger<DedicatedHostSelector> logger, 
                ICacheProvider cacheProvider,
                IDhmComputeClient dhmComputeClient) 
                : base(logger, cacheProvider, dhmComputeClient)
            {
            }

            public override async Task<IList<DedicatedHost>> ListDedicatedHosts(
                string token,
                string cloudName,
                string tenantId,
                string subscriptionId,
                string resourceGroup,
                string hostGroupName)
            {
                return JsonConvert.DeserializeObject<List<DedicatedHost>>(
                    File.ReadAllText(@"TestData\dedicatedHostsInput1.json"));
            }

            public override async Task GetAllocatableVmsOnHost(string token,
                string cloudName,
                string tenantId,
                string subscriptionId,
                string resourceGroup,
                string hostGroupName,
                DedicatedHost dedicatedHost,
                IDictionary<DedicatedHost, List<DedicatedHostAllocatableVM>> dictionary)
            {
                var dedicatedHostList =
                    JsonConvert.DeserializeObject<List<DedicatedHost>>(
                        File.ReadAllText(@"TestData\dedicatedHostsInput1.json"));

                var host = dedicatedHostList.First(h =>
                    h.Name.Equals(dedicatedHost.Name, StringComparison.InvariantCultureIgnoreCase));
                dictionary[dedicatedHost] = host.InstanceView.AvailableCapacity.AllocatableVMs.ToList();
            }
        }
    }
}