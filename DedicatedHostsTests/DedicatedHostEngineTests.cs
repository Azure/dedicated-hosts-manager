using DedicatedHostsManager.ComputeClient;
using DedicatedHostsManager.DedicatedHostEngine;
using DedicatedHostsManager.DedicatedHostStateManager;
using DedicatedHostsManager.Sync;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DedicatedHostGroup = Microsoft.Azure.Management.Compute.Models.DedicatedHostGroup;

namespace DedicatedHostsManagerTests
{
    /// <summary>
    /// Unit tests for Dedicated Hosts Engine.
    /// </summary>
    public class DedicatedHostEngineTests
    {
        private const string Token = "test-Token";
        private const string CloudName = "AzureUSGovernment";
        private const string TenantId = "test-teanant-id";
        private const string SubscriptionId = "test-sub";
        private const string ResourceGroup = "test-rg";
        private const string DhgName = "test-dhg-name";
        private const int PlatformFaultDomainCount = 1;
        private const string Location = "test-Location";
        private const string HostGroupName = "test-dhg";
        private const string VmSize = "Standard-D2s-v3";
        private static AzureOperationResponse<DedicatedHostGroup> _dedicatedHostGroupResponseMock;        

        [Fact]
        public async Task CreateDedicatedHostGroupTest()
        {
            var mockDhg = new DedicatedHostGroup(Location, PlatformFaultDomainCount, null, HostGroupName);
            var loggerMock = new Mock<ILogger<DedicatedHostEngine>>();
            var configurationMock = new Mock<IConfiguration>();
            configurationMock.Setup(s => s["DhgCreateRetryCount"]).Returns("1");
            var dedicatedHostSelectorMock = new Mock<IDedicatedHostSelector>();
            var syncProviderMock = new Mock<ISyncProvider>();
            var dedicatedHostStateManagerMock = new Mock<IDedicatedHostStateManager>();
            var dhmComputeClientMock = new Mock<IDhmComputeClient>();
            var computeManagementClientMock = new Mock<IComputeManagementClient>();

            _dedicatedHostGroupResponseMock = new Microsoft.Rest.Azure.AzureOperationResponse<DedicatedHostGroup>
            {
                Body = mockDhg,
            };
            _dedicatedHostGroupResponseMock.Body.Location = Location;
            _dedicatedHostGroupResponseMock.Body.PlatformFaultDomainCount = PlatformFaultDomainCount;
            computeManagementClientMock
                .Setup(
                    s => s.DedicatedHostGroups.CreateOrUpdateWithHttpMessagesAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<DedicatedHostGroup>(),
                        null,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(_dedicatedHostGroupResponseMock);
            dhmComputeClientMock.Setup(s =>
                    s.GetComputeManagementClient(It.IsAny<string>(), It.IsAny<AzureCredentials>(),
                        It.IsAny<AzureEnvironment>()))
                .ReturnsAsync(computeManagementClientMock.Object);

            var dedicatedHostEngineTest = new DedicatedHostEngineTest(
                loggerMock.Object,
                configurationMock.Object,
                dedicatedHostSelectorMock.Object,
                syncProviderMock.Object,
                dedicatedHostStateManagerMock.Object,
                dhmComputeClientMock.Object);
            var createDedicatedHostGroupResponse = await dedicatedHostEngineTest.CreateDedicatedHostGroup(
                Token,
                AzureEnvironment.AzureUSGovernment,
                TenantId,
                SubscriptionId,
                ResourceGroup,
                DhgName,
                "",
                PlatformFaultDomainCount,
                Location);

            Assert.Equal(createDedicatedHostGroupResponse.Body.PlatformFaultDomainCount, PlatformFaultDomainCount);
            Assert.Equal(createDedicatedHostGroupResponse.Body.Location, Location);
            Assert.Equal(createDedicatedHostGroupResponse.Body.Name, HostGroupName);
        }

        [Fact]
        public async Task GetDedicatedHostForVmPlacementTest()
        {
            var loggerMock = new Mock<ILogger<DedicatedHostEngine>>();
            var configurationMock = new Mock<IConfiguration>();
            var dedicatedHostSelectorMock = new Mock<IDedicatedHostSelector>();
            var syncProviderMock = new Mock<ISyncProvider>();
            var dedicatedHostStateManagerMock = new Mock<IDedicatedHostStateManager>();
            var dhmComputeClientMock = new Mock<IDhmComputeClient>();
            var computeManagementClientMock = new Mock<IComputeManagementClient>();
            var mockDhg = new DedicatedHostGroup(Location, PlatformFaultDomainCount, null, HostGroupName);

            _dedicatedHostGroupResponseMock = new Microsoft.Rest.Azure.AzureOperationResponse<DedicatedHostGroup>
            {
                Body = mockDhg,
            };
            _dedicatedHostGroupResponseMock.Body.Location = Location;
            _dedicatedHostGroupResponseMock.Body.PlatformFaultDomainCount = PlatformFaultDomainCount;
            computeManagementClientMock
                .Setup(
                    s => s.DedicatedHostGroups.CreateOrUpdateWithHttpMessagesAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        It.IsAny<DedicatedHostGroup>(),
                        null,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(_dedicatedHostGroupResponseMock);
            dhmComputeClientMock.Setup(s =>
                    s.GetComputeManagementClient(It.IsAny<string>(), It.IsAny<AzureCredentials>(),
                        It.IsAny<AzureEnvironment>()))
                .ReturnsAsync(computeManagementClientMock.Object);
            var dedicatedHostList =
                JsonConvert.DeserializeObject<List<DedicatedHost>>(
                    File.ReadAllText(@"TestData\dedicatedHostsInput1.json"));
            dedicatedHostSelectorMock
                .Setup(
                    s => s.ListDedicatedHosts(Token, AzureEnvironment.AzureUSGovernment, TenantId, SubscriptionId, ResourceGroup, HostGroupName))
                .ReturnsAsync(dedicatedHostList);
            dedicatedHostStateManagerMock.Setup(s => s.IsHostAtCapacity(It.IsAny<string>())).Returns(false);
            dedicatedHostSelectorMock
                .Setup(
                s => s.SelectDedicatedHost(Token, AzureEnvironment.AzureUSGovernment, TenantId, SubscriptionId, ResourceGroup, HostGroupName, VmSize))
                .ReturnsAsync("/subscriptions/6e412d70-9128-48a7-97b4-04e5bd35cefc/resourceGroups/63296244-ce2c-46d8-bc36-3e558792fbee/providers/Microsoft.Compute/hostGroups/citrix-dhg/hosts/20887a6e-0866-4bae-82b7-880839d9e76b");

            var dedicatedHostEngine = new DedicatedHostEngine(
                loggerMock.Object, 
                configurationMock.Object,
                dedicatedHostSelectorMock.Object,
                syncProviderMock.Object,
                dedicatedHostStateManagerMock.Object,
                dhmComputeClientMock.Object);
            var host = await dedicatedHostEngine.GetDedicatedHostForVmPlacement(Token, AzureEnvironment.AzureUSGovernment, TenantId, SubscriptionId,
                ResourceGroup, HostGroupName, VmSize, "test-vm", Location);

            Assert.Equal(host, "/subscriptions/6e412d70-9128-48a7-97b4-04e5bd35cefc/resourceGroups/63296244-ce2c-46d8-bc36-3e558792fbee/providers/Microsoft.Compute/hostGroups/citrix-dhg/hosts/20887a6e-0866-4bae-82b7-880839d9e76b");
        }

        private class DedicatedHostEngineTest : DedicatedHostEngine
        {
            public DedicatedHostEngineTest(
                ILogger<DedicatedHostEngine> logger, 
                IConfiguration configuration, 
                IDedicatedHostSelector dedicatedHostSelector, 
                ISyncProvider syncProvider,
                IDedicatedHostStateManager dedicatedHostStateManager,
                IDhmComputeClient dhmComputeClient) 
                : base(
                    logger, 
                    configuration, 
                    dedicatedHostSelector, 
                    syncProvider,
                    dedicatedHostStateManager,
                    dhmComputeClient)
            {
            }
        }
    }
}
