﻿using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;
        private readonly ILogger<SyncProvider> _logger;
        private readonly CloudBlobContainer _cloudBlobContainer;
        private string _lease;

        /// <summary>
        /// Initialize sync provider.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="logger">Logging.</param>
        public SyncProvider(IConfiguration configuration, ILogger<SyncProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;
            var storageAccount = CloudStorageAccount.Parse(_configuration.GetConnectionString("StorageConnectionString"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            _cloudBlobContainer = blobClient.GetContainerReference(_configuration["LockContainerName"]);
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

            var lockIntervalInSeconds = int.Parse(_configuration["LockIntervalInSeconds"]);
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