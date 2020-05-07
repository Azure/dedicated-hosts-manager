using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;

namespace DedicatedHostsManager.Sync
{
    /// <summary>
    /// Synchronization handling.
    /// </summary>
    public class SyncProvider : ISyncProvider
    {
        private readonly Config _config;
        private readonly ILogger<SyncProvider> _logger;
        private readonly CloudBlobContainer _cloudBlobContainer;
        private string _lease;

        /// <summary>
        /// Initialize sync provider.
        /// </summary>
        /// <param name="config">Configuration.</param>
        /// <param name="logger">Logging.</param>
        public SyncProvider(Config config, ILogger<SyncProvider> logger)
        {
            _config = config;
            _logger = logger;
            var storageAccount = CloudStorageAccount.Parse(_config.ConnectionStrings.StorageConnectionString);
            var blobClient = storageAccount.CreateCloudBlobClient();
            _cloudBlobContainer = blobClient.GetContainerReference(_config.LockContainerName);
        }

        /// <summary>
        /// Serializing logic (start) using storage leases.
        /// </summary>
        /// <param name="blobName">Storage blob name.</param>
        public async Task StartSerialRequests(string blobName)
        {
            await _cloudBlobContainer.CreateIfNotExistsAsync();
            var blockBlob = _cloudBlobContainer.GetBlockBlobReference(blobName);
            if (!await blockBlob.ExistsAsync())
            {
                await blockBlob.UploadTextAsync(blobName);
            }

            var lockIntervalInSeconds = _config.LockIntervalInSeconds;
            _lease = await blockBlob.AcquireLeaseAsync(TimeSpan.FromSeconds(lockIntervalInSeconds), null);
            _logger.LogInformation($"Acquired lock for {blockBlob}");
        }

        /// <summary>
        /// Serializing logic (stop) using storage leases.
        /// </summary>
        /// <param name="blobName">Storage blob name.</param>
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