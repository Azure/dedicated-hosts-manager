﻿using DedicatedHosts;
using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Rest.Azure;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DedicatedHostsTests
{
    public class DedicatedHostsSelectorUnitTests
    {
        private const string Token = "test-Token";
        private const string CloudName = "AzureUSGovernment";
        private const string TenantId = "test-teanant-id";
        private const string SubscriptionId = "test-sub";
        private const string ResourceGroup = "test-rg";
        private const string HostGroupName = "test-dhg";
        private const string VmSize = "Standard_D4s_v3";

        [Fact]
        public async Task SelectDedicatedHostBasedOnProvisioningTimeTest()
        {
            var loggerMock = new Mock<ILogger<DedicatedHostSelector>>();
            var dedicatedHostList =
                JsonConvert.DeserializeObject<List<DedicatedHost>>(
                    File.ReadAllText(@"TestData\dedicatedHostsInput1.json"));
            const string expectedHostId = "/subscriptions/6e412d70-9128-48a7-97b4-04e5bd35cefc/resourceGroups/63296244-ce2c-46d8-bc36-3e558792fbee/providers/Microsoft.Compute/hostGroups/citrix-dhg/hosts/20887a6e-0866-4bae-82b7-880839d9e76b";

            var dedicatedHostSelector = new DedicatedHostSelectorTest(loggerMock.Object);
            var actualHostId = await dedicatedHostSelector.SelectDedicatedHost(
                Token,
                CloudName,
                TenantId,
                SubscriptionId,
                ResourceGroup,
                HostGroupName,
                VmSize,
                dedicatedHostList);

            Assert.Equal(actualHostId, expectedHostId);
        }

        private class DedicatedHostSelectorTest : DedicatedHostSelector
        {
            public DedicatedHostSelectorTest(ILogger<DedicatedHostSelector> logger) : base(logger)
            {
            }

            protected override IComputeManagementClient ComputeManagementClient(
                string subscriptionId,
                AzureCredentials azureCredentials)
            {
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
                            default(CancellationToken)))
                    .ReturnsAsync(mockDedicatedHostResponse1);
                computeManagementClientMock
                    .Setup(
                        s => s.DedicatedHosts.GetWithHttpMessagesAsync(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            "dh2",
                            InstanceViewTypes.InstanceView,
                            null,
                            default(CancellationToken)))
                    .ReturnsAsync(mockDedicatedHostResponse2);
                return computeManagementClientMock.Object;
            }
        }
    }
}