using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DedicatedHostsManager.Cache
{
    public class CacheProvider : ICacheProvider
    {
        private static readonly object LockObject = new object();
        private readonly IConfiguration _configuration;
        private readonly ILogger<CacheProvider> _logger;
        private readonly int _dbIndex = 0;
        private readonly string _redisCacheConnection;
        private ConnectionMultiplexer _connectionMultiplexer;

        public CacheProvider(IConfiguration configuration, ILogger<CacheProvider> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _redisCacheConnection = _configuration.GetConnectionString("RedisConnectionString");
            _dbIndex = 0;
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

        public bool AddData(string key, string data, TimeSpan? expiry = null)
        {
            return this.ConnectionMultiplexer.GetDatabase(_dbIndex).StringSet(key, data, expiry);
        }

        public bool KeyExists(string key)
        {
            return ConnectionMultiplexer.GetDatabase(_dbIndex).KeyExists(key);
        }

        public void DeleteKey(string key)
        {
            ConnectionMultiplexer.GetDatabase(_dbIndex).KeyDelete(key);
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