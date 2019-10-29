using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;

namespace DedicatedHostsManager.DedicatedHostStateManager
{
    public class DedicatedHostStateManager : IDedicatedHostStateManager
    {
        private static readonly object LockObject = new object();
        private readonly IConfiguration _configuration;
        private readonly ILogger<DedicatedHostStateManager> _logger;
        private readonly int _hostCapacityDbIndex;
        private readonly int _hostDeletionDbIndex;
        private readonly int _hostUsageDbIndex;
        private readonly string _redisCacheConnection;
        private ConnectionMultiplexer _connectionMultiplexer;

        public DedicatedHostStateManager(IConfiguration configuration, ILogger<DedicatedHostStateManager> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _redisCacheConnection = _configuration.GetConnectionString("RedisConnectionString");
            _hostCapacityDbIndex = 0;
            _hostDeletionDbIndex = 1;
            _hostUsageDbIndex = 2;
        }
       
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
                    configurationOptions.ConnectTimeout = int.Parse(_configuration["RedisConnectTimeoutMilliseconds"]);
                    configurationOptions.SyncTimeout = int.Parse(_configuration["RedisSyncTimeoutMilliseconds"]);
                    configurationOptions.ConnectRetry = int.Parse(_configuration["RedisConnectRetryCount"]);
                    configurationOptions.AbortOnConnectFail = false;
                    configurationOptions.Ssl = true;
                    _connectionMultiplexer = ConnectionMultiplexer.Connect(configurationOptions);
                }

                return _connectionMultiplexer;
            }
        }

        public bool MarkHostAtCapacity(string key, string data, TimeSpan? expiry = null)
        {
            return this.ConnectionMultiplexer.GetDatabase(_hostCapacityDbIndex).StringSet(key, data, expiry);
        }

        public bool IsHostAtCapacity(string key)
        {
            return ConnectionMultiplexer.GetDatabase(_hostCapacityDbIndex).KeyExists(key);
        }

        public bool MarkHostForDeletion(string key, string data, TimeSpan? expiry = null)
        {
            return this.ConnectionMultiplexer.GetDatabase(_hostDeletionDbIndex).StringSet(key, data, expiry);
        }

        public bool IsHostMarkedForDeletion(string key)
        {
            return ConnectionMultiplexer.GetDatabase(_hostDeletionDbIndex).KeyExists(key);
        }

        public bool MarkHostUsage(string key, string data, TimeSpan? expiry = null)
        {
            return this.ConnectionMultiplexer.GetDatabase(_hostUsageDbIndex).StringSet(key, data, expiry);
        }

        public bool IsHostInUsage(string key)
        {
            return ConnectionMultiplexer.GetDatabase(_hostUsageDbIndex).KeyExists(key);
        }

        public void UnmarkHostUsage(string key)
        {
            DeleteKey(key, _hostUsageDbIndex);
        }

        private void DeleteKey(string key, int dbIndex)
        {
            ConnectionMultiplexer.GetDatabase(dbIndex).KeyDelete(key);
        }

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