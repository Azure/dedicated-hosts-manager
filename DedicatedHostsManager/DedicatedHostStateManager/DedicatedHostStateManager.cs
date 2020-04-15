using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;

namespace DedicatedHostsManager.DedicatedHostStateManager
{
    /// <summary>
    /// Dedicated Host state manager.
    /// </summary>
    public class DedicatedHostStateManager : IDedicatedHostStateManager
    {
        private static readonly object LockObject = new object();
        private readonly Config _config;
        private readonly ILogger<DedicatedHostStateManager> _logger;
        private readonly int _hostCapacityDbIndex;
        private readonly int _hostDeletionDbIndex;
        private readonly int _hostUsageDbIndex;
        private readonly string _redisCacheConnection;
        private ConnectionMultiplexer _connectionMultiplexer;

        /// <summary>
        /// Initializes the state manager.
        /// </summary>
        /// <param name="config">Configuration.</param>
        /// <param name="logger">Logging.</param>
        public DedicatedHostStateManager(Config config, ILogger<DedicatedHostStateManager> logger)
        {
            _config = config;
            _logger = logger;
            _redisCacheConnection = _config.ConnectionStrings.RedisConnectionString;
            _hostCapacityDbIndex = 0;
            _hostDeletionDbIndex = 1;
            _hostUsageDbIndex = 2;
        }
       
        /// <summary>
        /// Gets, and initializes, the Redis connection.
        /// </summary>
        public ConnectionMultiplexer ConnectionMultiplexer
        {
            get
            {
                if (_connectionMultiplexer != null && _connectionMultiplexer.IsConnected)
                {
                    return _connectionMultiplexer;
                }

                lock (LockObject)
                {
                    if (_connectionMultiplexer != null && _connectionMultiplexer.IsConnected)
                    {
                        return _connectionMultiplexer;
                    }

                    _connectionMultiplexer?.Dispose();
                    var configurationOptions = ConfigurationOptions.Parse(_redisCacheConnection);
                    configurationOptions.ConnectTimeout = _config.RedisConnectTimeoutMilliseconds;
                    configurationOptions.SyncTimeout = _config.RedisSyncTimeoutMilliseconds;
                    configurationOptions.ConnectRetry = _config.RedisConnectRetryCount;
                    configurationOptions.AbortOnConnectFail = false;
                    configurationOptions.Ssl = true;
                    _connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
                }

                return _connectionMultiplexer;
            }
        }

        /// <summary>
        /// Inserts a key in the cache when a Dedicated Host is at full capacity.
        /// </summary>
        /// <param name="key">Key (host ID).</param>
        /// <param name="data">Data.</param>
        /// <param name="expiry">TTL.</param>
        public bool MarkHostAtCapacity(string key, string data, TimeSpan? expiry = null)
        {
            return this.ConnectionMultiplexer.GetDatabase(_hostCapacityDbIndex).StringSet(key, data, expiry);
        }

        /// <summary>
        /// Checks to see if a Dedicated Host is at full capacity.
        /// </summary>
        /// <param name="key">Key (host ID).</param>
        public bool IsHostAtCapacity(string key)
        {
            return ConnectionMultiplexer.GetDatabase(_hostCapacityDbIndex).KeyExists(key);
        }

        /// <summary>
        /// Inserts a key in the cache when a Dedicated Host is ready for deletion.
        /// </summary>
        /// <param name="key">Key (host ID).</param>
        /// <param name="data">Data.</param>
        /// <param name="expiry">TTL.</param>
        public bool MarkHostForDeletion(string key, string data, TimeSpan? expiry = null)
        {
            return this.ConnectionMultiplexer.GetDatabase(_hostDeletionDbIndex).StringSet(key, data, expiry);
        }

        /// <summary>
        /// Checks to see if a Dedicated Host is pending deletion.
        /// </summary>
        /// <param name="key"></param>
        public bool IsHostMarkedForDeletion(string key)
        {
            return ConnectionMultiplexer.GetDatabase(_hostDeletionDbIndex).KeyExists(key);
        }

        /// <summary>
        /// Inserts a key in the cache when a VM is being provisioned on a Dedicated Host.
        /// </summary>
        /// <param name="key">Key (host ID).</param>
        /// <param name="data">Data.</param>
        /// <param name="expiry">TTL.</param>
        public bool MarkHostUsage(string key, string data, TimeSpan? expiry = null)
        {
            return this.ConnectionMultiplexer.GetDatabase(_hostUsageDbIndex).StringSet(key, data, expiry);
        }

        /// <summary>
        /// Checks to see if a VM is being provisioned on a Dedicated Host.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool IsHostInUsage(string key)
        {
            return ConnectionMultiplexer.GetDatabase(_hostUsageDbIndex).KeyExists(key);
        }

        /// <summary>
        /// Removes the Dedicated Host from the pending deletion list.
        /// </summary>
        /// <param name="key">Key (host ID).</param>
        public void UnmarkHostForDeletion(string key)
        {
            DeleteKey(key, _hostDeletionDbIndex);
        }

        /// <summary>
        /// Deletes a key from an index in Redis
        /// </summary>
        /// <param name="key"></param>
        /// <param name="dbIndex"></param>
        private void DeleteKey(string key, int dbIndex)
        {
            ConnectionMultiplexer.GetDatabase(dbIndex).KeyDelete(key);
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            if (_connectionMultiplexer == null || !_connectionMultiplexer.IsConnected)
            {
                return;
            }

            _connectionMultiplexer.Dispose();
            _connectionMultiplexer = null;
        }
    }
}