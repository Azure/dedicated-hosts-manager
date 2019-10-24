using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace DedicatedHostsManager
{
    public class SyncProvider : ISyncProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SyncProvider> _logger;
        private readonly CloudBlobContainer _cloudBlobContainer;
        private string _lease;

        public SyncProvider(IConfiguration configuration, ILogger<SyncProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;
            var storageAccount = CloudStorageAccount.Parse(_configuration.GetConnectionString("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            _cloudBlobContainer = blobClient.GetContainerReference(_configuration["LockContainerName"]);
        }

        public async Task StartSerialRequests(string blobName)
        {
            await _cloudBlobContainer.CreateIfNotExistsAsync();
            var blockBlob = _cloudBlobContainer.GetBlockBlobReference(blobName);
            if (!await blockBlob.ExistsAsync())
            {
                await blockBlob.UploadTextAsync(blobName);
            }

            var lockIntervalInSeconds = int.Parse(_configuration["LockIntervalInSeconds"]);
            _lease = await blockBlob.AcquireLeaseAsync(TimeSpan.FromSeconds(lockIntervalInSeconds), null);
            _logger.LogInformation($"Acquired lock for {blockBlob}");
        }

        public async Task EndSerialRequests(string blobName)
        {
            if (!string.IsNullOrEmpty(_lease))
            {
                var blockBlob = _cloudBlobContainer.GetBlockBlobReference(blobName);
                var accessCondition = AccessCondition.GenerateLeaseCondition(_lease);
                await blockBlob.ReleaseLeaseAsync(accessCondition);
                _logger.LogInformation($"Released lock for {blockBlob}");
            }
        }
    }
}