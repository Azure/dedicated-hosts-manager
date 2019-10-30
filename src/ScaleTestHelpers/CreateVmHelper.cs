using Microsoft.Azure.Management.Compute;
using Microsoft.Azure.Management.Compute.Models;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using System;
using System.Collections.Generic;

namespace ScaleTestHelpers
{
    /// <summary>
    /// Helper class used by testing clients to create VMs.
    /// </summary>
    public class CreateVmHelper
    {
        public static VirtualMachine CreateVirtualMachine(
           IComputeManagementClient computeManagementClient,
           IAzure azure,
           Region region,
           string resourceGroupName,
           string vmName,
           string vmSize,
           string availabilityZone,
           string pipName,
           string vnetName,
           string nicName)
        {
            if (computeManagementClient == null)
            {
                throw new ArgumentNullException(nameof(computeManagementClient));
            }

            if (azure == null)
            {
                throw new ArgumentNullException(nameof(azure));
            }

            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            if (string.IsNullOrEmpty(resourceGroupName))
            {
                throw new ArgumentException(nameof(resourceGroupName));
            }

            if (string.IsNullOrEmpty(vmName))
            {
                throw new ArgumentException(nameof(vmName));
            }

            if (string.IsNullOrEmpty(vmSize))
            {
                throw new ArgumentException(nameof(vmSize));
            }

            if (string.IsNullOrEmpty(pipName))
            {
                throw new ArgumentException(nameof(pipName));
            }

            if (string.IsNullOrEmpty(vnetName))
            {
                throw new ArgumentException(nameof(vnetName));
            }

            if (string.IsNullOrEmpty(nicName))
            {
                throw new ArgumentException(nameof(nicName));
            }

            Console.WriteLine($"Creating resource group {resourceGroupName}");
            var resourceGroup = azure.ResourceGroups.Define(resourceGroupName)
                .WithRegion(region)
                .Create();

            Console.WriteLine($"Creating public IP address {pipName}");
            var publicIpAddress = azure.PublicIPAddresses.Define(pipName)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroupName)
                .WithDynamicIP()
                .Create();

            Console.WriteLine($"Creating virtual network {vnetName}");
            var network = azure.Networks.Define(vnetName)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroupName)
                .WithAddressSpace("10.0.0.0/16")
                .WithSubnet("adhSubnet", "10.0.0.0/24")
                .Create();

            Console.WriteLine($"Creating network interface {nicName}");
            var networkInterface = azure.NetworkInterfaces.Define(nicName)
                .WithRegion(region)
                .WithExistingResourceGroup(resourceGroupName)
                .WithExistingPrimaryNetwork(network)
                .WithSubnet("adhSubnet")
                .WithPrimaryPrivateIPAddressDynamic()
                .WithExistingPrimaryPublicIPAddress(publicIpAddress)
                .Create();

            Console.WriteLine($"Configuring virtual machine {vmName}" + Environment.NewLine);
            var imageRef = new ImageReference
            {
                Publisher = "MicrosoftWindowsServer",
                Sku = "2012-R2-Datacenter",
                Offer = "WindowsServer",
                Version = "latest"
            };

            return new VirtualMachine(
                region.Name,
                Guid.NewGuid().ToString(),
                vmName,
                "",
                null,
                null,
                new Microsoft.Azure.Management.Compute.Models.HardwareProfile { VmSize = vmSize },
                new Microsoft.Azure.Management.Compute.Models.StorageProfile
                {
                    ImageReference = imageRef,
                    OsDisk = new Microsoft.Azure.Management.Compute.Models.OSDisk
                    {
                        Caching = CachingTypes.None,
                        CreateOption = DiskCreateOptionTypes.FromImage,
                        Name = "disk-"+Guid.NewGuid(),
                        Vhd = null,
                        ManagedDisk = new ManagedDiskParameters
                        {
                            StorageAccountType = "Standard_LRS"
                        }
                    },
                },
                null,
                new Microsoft.Azure.Management.Compute.Models.OSProfile
                {
                    AdminUsername = "azureuser",
                    AdminPassword = Guid.NewGuid().ToString(),
                    ComputerName = vmName,
                },
                new Microsoft.Azure.Management.Compute.Models.NetworkProfile
                {
                    NetworkInterfaces = new List<NetworkInterfaceReference>
                    {
                        new NetworkInterfaceReference(networkInterface.Id)
                    }
                },
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null);  
        }
    }
}