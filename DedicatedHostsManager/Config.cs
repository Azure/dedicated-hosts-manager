using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DedicatedHostsManager
{
    public class Config
    {
        public string LockContainerName { get; set; }
        public int LockRetryCount { get; set; }
        public int LockIntervalInSeconds { get; set; }
        public int MinIntervalToCheckForVmInSeconds { get; set; }
        public int MaxIntervalToCheckForVmInSeconds { get; set; }
        public int RetryCountToCheckVmState { get; set; }
        public int MaxRetriesToCreateVm { get; set; }
        public int RedisConnectTimeoutMilliseconds { get; set; }
        public int RedisSyncTimeoutMilliseconds { get; set; }
        public int RedisConnectRetryCount { get; set; }
        public int DhgCreateRetryCount { get; set; }
        public int ComputeClientLongRunningOperationRetryTimeoutSeconds { get; set; }
        public int ComputeClientHttpTimeoutMin { get; set; }
        public int GetArmMetadataRetryCount { get; set; }
        public int DedicatedHostCacheTtlMin { get; set; }
        public string GetArmMetadataUrl { get; set; }
        public string HostSelectorVmSize { get; set; }
        public bool IsRunningInFairfax { get; set; }
        public Connectionstrings ConnectionStrings { get; set; }

        public string VmToHostMapping
        {
            get { return "VirtualMachineToHostMapping to be referenced for details"; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new Exception("VmToHostMapping must be specified in the configuration.");
                }

                VirtualMachineToHostMapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(value); 
            }
        }

        public Dictionary<string, string> VirtualMachineToHostMapping { get; private set; } = new Dictionary<string, string>();

        public string DedicatedHostMapping
        {
            get { return "DedicatedHostConfigurationTable to be referenced for details"; }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new Exception("DedicatedHostMapping must be specified in the configuration.");
                }

                var mapping = JsonConvert.DeserializeObject<IList<DedicatedHostConfiguration.JsonRepresentation>>(value);

                this.DedicatedHostConfigurationTable = mapping
                    .SelectMany(hm => hm.HostMapping
                        .Select(h => new { hm.Family, h.Region, h.Host.Type, h.Host.VmMapping }))
                    .SelectMany(vm => vm.VmMapping
                        .Select(fo => new DedicatedHostConfiguration()
                        {
                            DhFamily = vm.Family,
                            Location = vm.Region,
                            DhSku = vm.Type,
                            VmSku = fo.Size,
                            VmCapacity = fo.Capacity
                        }
                        )).ToList(); 
            }
        }

        public IList<DedicatedHostConfiguration> DedicatedHostConfigurationTable { get; set; } = new List<DedicatedHostConfiguration>();

        public class Connectionstrings
        {
            public string StorageConnectionString { get; set; }
            public string RedisConnectionString { get; set; }
        }

        public class DedicatedHostConfiguration
        {
            public string DhFamily { get; set; }
            public string Location { get; set; }
            public string DhSku { get; set; }
            public string VmSku { get; set; }
            public int VmCapacity { get; set; }

            public class JsonRepresentation
            {
                public string Family { get; set; }
                public Hostmapping[] HostMapping { get; set; }

                public class Hostmapping
                {
                    public string Region { get; set; }
                    public Host Host { get; set; }
                }

                public class Host
                {
                    public string Type { get; set; }
                    public VmMapping[] VmMapping { get; set; }
                }

                public class VmMapping
                {
                    public string Size { get; set; }
                    public int Capacity { get; set; }
                }
            }
        }
    }
}

