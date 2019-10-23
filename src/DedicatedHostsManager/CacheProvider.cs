using System;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DedicatedHostsManager
{
    public class CacheProvider : ICacheProvider
    {
        private static readonly object LockObject = new object();
        private readonly IConfiguration _configuration;
        private readonly ILogger<CacheProvider> _logger;
        private readonly int _defaultDbIndex = 0;
        private readonly string _redisCacheConnection;
        private ConnectionMultiplexer _connectionMultiplexer;

        public CacheProvider(IConfiguration configuration, ILogger<CacheProvider> logger, int dbi = 0)
        {
            _configuration = configuration;
            _logger = logger;
            _redisCacheConnection = _configuration.GetConnectionString("RedisConnectionString");
            _defaultDbIndex = dbi;
        }
       
        public ConnectionMultiplexer ConnectionMultiplexer
        {
            get
            {
                if (_connectionMultiplexer == null || !_connectionMultiplexer.IsConnected)
                {
                    lock (LockObject)
                    {
                        if (_connectionMultiplexer == null || !_connectionMultiplexer.IsConnected)
                        {
                            _connectionMultiplexer?.Dispose();
                            var configurationOptions = ConfigurationOptions.Parse(_redisCacheConnection);

                            // TODO: read values from config
                            configurationOptions.ConnectTimeout = 5000;
                            configurationOptions.SyncTimeout = 10000;
                            configurationOptions.ConnectRetry = 3;

                            configurationOptions.AbortOnConnectFail = false;
                            configurationOptions.Ssl = true;
                            var newConnection = ConnectionMultiplexer.Connect(configurationOptions);
                            Interlocked.Exchange(ref _connectionMultiplexer, newConnection);
                        }
                    }
                }

                return _connectionMultiplexer;
            }

            set => _connectionMultiplexer = value;
        }

        public bool AddData(string key, string data, TimeSpan? expiry = null)
        {
            return this.ConnectionMultiplexer.GetDatabase(_defaultDbIndex).StringSet(key, data, expiry);
        }

        public bool KeyExists(string key)
        {
            return ConnectionMultiplexer.GetDatabase(_defaultDbIndex).KeyExists(key);
        }

        public void DeleteKey(string key)
        {
            ConnectionMultiplexer.GetDatabase(_defaultDbIndex).KeyDelete(key);
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