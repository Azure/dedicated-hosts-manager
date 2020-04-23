using DedicatedHostsManager;
using DedicatedHostsManager.ComputeClient;
using DedicatedHostsManager.DedicatedHostEngine;
using DedicatedHostsManager.DedicatedHostStateManager;
using DedicatedHostsManager.Sync;
using FluentAssertions;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;
using Moq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
            var config = new Config();
            config.DhgCreateRetryCount = 1;
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
                config,
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
            var configurationMock = new Mock<Config>();
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

        [Theory]
        [MemberData(nameof(PrepareDedicatedHostGroupTestData))]
        public async Task PrepareDedicatedHostGroup_Scenarios_Test(string location, int platformFDCount, int? platformFD, int vmInstanceToCreate, List<DedicatedHost> existingHosts, List<DedicatedHost> expectedHostsToBeCreated)
        {
            // Arrange
            var hostGroupName = "TestDH";
            var loggerMock = new Mock<ILogger<DedicatedHostEngine>>();
            var config = new Config();
            var dedicatedHostSelectorMock = new Mock<IDedicatedHostSelector>();
            var syncProviderMock = new Mock<ISyncProvider>();
            var dedicatedHostStateManagerMock = new Mock<IDedicatedHostStateManager>();
            var dhmComputeClientMock = new Mock<IDhmComputeClient>();
            var computeManagementClientMock = new Mock<IComputeManagementClient>();

            // *** Mock Configuration
            config.DedicatedHostMapping = File.ReadAllText(@"TestData\PrepareDedicatedHostMappingConfig.json");
            // *** Mock Get Host Group call
            computeManagementClientMock
                 .Setup(
                    s => s.DedicatedHostGroups.GetWithHttpMessagesAsync(
                        It.IsAny<string>(),
                        It.IsAny<string>(),
                        null,
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(new AzureOperationResponse<DedicatedHostGroup>() { 
                    Response = new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK),
                                    Body = new DedicatedHostGroup(location, platformFDCount, null, hostGroupName) });

            // *** Mock Existing Host information call
            existingHosts.ForEach(dh =>
                        computeManagementClientMock
                            .Setup(
                                s => s.DedicatedHosts.GetWithHttpMessagesAsync(
                                    It.IsAny<string>(),
                                    hostGroupName,
                                    dh.Name,
                                    InstanceViewTypes.InstanceView,
                                    null,
                                    It.IsAny<CancellationToken>()))
                            .ReturnsAsync(new AzureOperationResponse<DedicatedHost>() { Body = existingHosts.Single(d => d.Name == dh.Name) }));

            // *** Mock Create Dedicated Host Call
            computeManagementClientMock
                .Setup(s => s.DedicatedHosts.CreateOrUpdateWithHttpMessagesAsync(
                                    It.IsAny<string>(),
                                    hostGroupName,
                                    It.IsAny<string>(),
                                    It.IsAny<DedicatedHost>(),
                                    null,
                                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((string rg, string dhgName, string dhName, DedicatedHost dh, Dictionary<string, List<string>> headers, CancellationToken ctk) =>
                    {
                        return new AzureOperationResponse<DedicatedHost>()
                        {
                            Body = new DedicatedHost(location: dh.Location, sku: dh.Sku, name: dhName, platformFaultDomain: dh.PlatformFaultDomain)
                        };
                    });

            dhmComputeClientMock.Setup(s =>
                    s.GetComputeManagementClient(It.IsAny<string>(), It.IsAny<AzureCredentials>(),
                        It.IsAny<AzureEnvironment>()))
                .ReturnsAsync(computeManagementClientMock.Object);

            // *** Mock List Hosts call
            dedicatedHostSelectorMock
              .Setup(
                  s => s.ListDedicatedHosts(
                      It.IsAny<string>(), It.IsAny<AzureEnvironment>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), hostGroupName))
              .ReturnsAsync(existingHosts);

            var dedicatedHostEngine = new DedicatedHostEngine(
                loggerMock.Object,
                config,
                dedicatedHostSelectorMock.Object,
                syncProviderMock.Object,
                dedicatedHostStateManagerMock.Object,
                dhmComputeClientMock.Object);

            // Act
            var addedHosts = await dedicatedHostEngine.PrepareDedicatedHostGroup(
                Token,
                AzureEnvironment.AzureUSGovernment,
                TenantId,
                SubscriptionId,
                ResourceGroup,
                hostGroupName,
                "Standard_D2s_v3",
                vmInstanceToCreate,
                platformFD);

            (addedHosts ?? (new List<DedicatedHost>()))
                .Select(p => new { p.Location, p.PlatformFaultDomain, p.Sku })
                .Should().BeEquivalentTo(expectedHostsToBeCreated
                    .Select(p => new { p.Location, p.PlatformFaultDomain, p.Sku }));
        }

        private class DedicatedHostEngineTest : DedicatedHostEngine
        {
            public DedicatedHostEngineTest(
                ILogger<DedicatedHostEngine> logger,
                Config config,
                IDedicatedHostSelector dedicatedHostSelector,
                ISyncProvider syncProvider,
                IDedicatedHostStateManager dedicatedHostStateManager,
                IDhmComputeClient dhmComputeClient)
                : base(
                    logger,
                    config,
                    dedicatedHostSelector,
                    syncProvider,
                    dedicatedHostStateManager,
                    dhmComputeClient)
            {
            }
        }

        public static IEnumerable<object[]> PrepareDedicatedHostGroupTestData =>
        new List<object[]>
        {
            // Single PlatformFaultDomain in DH Group
            new object[] {
                Region.GovernmentUSVirginia.Name,
                1,
                0,
                50,             // 50 - 10 = 40 Required with 32 instance capacity, 2  host required
                new List<DedicatedHost>(){
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="NotRelevant"},
                        name: "",
                        platformFaultDomain: 0,
                        instanceView: new DedicatedHostInstanceView(availableCapacity: new DedicatedHostAvailableCapacity(
                            new List<DedicatedHostAllocatableVM>()
                            {
                                new DedicatedHostAllocatableVM("Standard_D2s_v3", 10)
                            })))
                },
                new List<DedicatedHost>()
                {
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="DSv3-Type1"},
                        platformFaultDomain: 0),
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="DSv3-Type1"},
                        platformFaultDomain: 0),
                }
            },       
            // Existing Hosts has enough capacity
            new object[] {
                Region.GovernmentUSVirginia.Name,
                1,
                0,
                5,             // 10 Available, 5 required, 0  host required
                new List<DedicatedHost>(){
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="NotRelevant"},
                        name: "",
                        platformFaultDomain: 0,
                        instanceView: new DedicatedHostInstanceView(availableCapacity: new DedicatedHostAvailableCapacity(
                            new List<DedicatedHostAllocatableVM>()
                            {
                                new DedicatedHostAllocatableVM("Standard_D2s_v3", 10)
                            })))
                },
                new List<DedicatedHost>()
                {
                }
            },
            // PlatformFaultDomainCount for DHG > 1, null in PlatformFaultDomain specified, should round assign equal hosts 
            new object[] {
                Region.GovernmentUSVirginia.Name,
                2,
                null,
                40,             // 40 / 2 Fault Domains = 20 each required to be added per FD
                new List<DedicatedHost>(){
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="NotRelevant"},
                        name: "Host-1",
                        platformFaultDomain: 0,
                        instanceView: new DedicatedHostInstanceView(availableCapacity: new DedicatedHostAvailableCapacity(
                            new List<DedicatedHostAllocatableVM>()
                            {
                                new DedicatedHostAllocatableVM("Standard_D2s_v3", 10)
                            }))),
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="NotRelevant"},
                        name: "Host-2",
                        platformFaultDomain: 1,
                        instanceView: new DedicatedHostInstanceView(availableCapacity: new DedicatedHostAvailableCapacity(
                            new List<DedicatedHostAllocatableVM>()
                            {
                                new DedicatedHostAllocatableVM("Standard_D2s_v3", 30)
                            })))
                },
                new List<DedicatedHost>()
                {
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="DSv3-Type1"},
                        platformFaultDomain: 0) 
                }
            },
            // PlatformFaultDomainCount for DHG > 1, 0 in PlatformFaultDomain specified, should add hosts for all to requested PlatformFaultDomain ONLY
            new object[] {
                Region.GovernmentUSVirginia.Name,
                2,
                0,
                60,             // 60 / 1 Fault Domains = 60  required to be added per FD 0
                new List<DedicatedHost>(){
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="NotRelevant"},
                        name: "Host-1",
                        platformFaultDomain: 0,
                        instanceView: new DedicatedHostInstanceView(availableCapacity: new DedicatedHostAvailableCapacity(
                            new List<DedicatedHostAllocatableVM>()
                            {
                                new DedicatedHostAllocatableVM("Standard_D2s_v3", 10)
                            }))),
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="NotRelevant"},
                        name: "Host-2",
                        platformFaultDomain: 1,
                        instanceView: new DedicatedHostInstanceView(availableCapacity: new DedicatedHostAvailableCapacity(
                            new List<DedicatedHostAllocatableVM>()
                            {
                                new DedicatedHostAllocatableVM("Standard_D2s_v3", 10)
                            })))
                },
                new List<DedicatedHost>()
                {
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="DSv3-Type1"},
                        platformFaultDomain: 0),
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="DSv3-Type1"},
                        platformFaultDomain: 0)
                }
            },
            // PlatformFaultDomainCount for DHG > 1, No existing Hosts, should create as expected
            new object[] {
                Region.GovernmentUSVirginia.Name,
                1,
                null,
                30,             // 30 / 1 Fault Domains = 30  requires creating 1 host
                new List<DedicatedHost>(){                   
                },
                new List<DedicatedHost>()
                {
                    new DedicatedHost(
                        location: Region.GovernmentUSVirginia.Name,
                        sku: new Sku(){Name="DSv3-Type1"},
                        platformFaultDomain: 0)                 }
            },
            // Region specific configuration to be applied if location specific config exists
            new object[] {
                Region.GovernmentUSIowa.Name,
                1,
                null,
                30,             // 30 / 1 Fault Domains  -> requires creating 1 Host but of type "DSv3-type2"
                new List<DedicatedHost>(){
                },
                new List<DedicatedHost>()
                {
                    new DedicatedHost(
                        location: Region.GovernmentUSIowa.Name,
                        sku: new Sku(){Name="DSv3-Type2"},
                        platformFaultDomain: 0) 
                }
            }
        };
    }
}
