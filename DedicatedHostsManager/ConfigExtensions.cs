using DedicatedHostsManager.ComputeClient;
using DedicatedHostsManager.DedicatedHostEngine;
using DedicatedHostsManager.DedicatedHostStateManager;
using DedicatedHostsManager.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DedicatedHostsManager
{
    public static class ConfigExtensions
    {
        public static void ConfigureCommonServices(this IServiceCollection services)
        {
            services.AddTransient<ServiceFactory>();
            services.AddSingleton(p => p.GetService<ServiceFactory>().CreatePocoConfig());
            services.AddHttpClient();
            services.AddSingleton<IDhmComputeClient, DhmComputeClient>();
            services.AddTransient<IDedicatedHostEngine, DedicatedHostEngine.DedicatedHostEngine>();
            services.AddTransient<IDedicatedHostSelector, DedicatedHostSelector>();
            services.AddTransient<ISyncProvider, SyncProvider>();
            services.AddTransient<IDedicatedHostStateManager, DedicatedHostStateManager.DedicatedHostStateManager>();
        }

        private class ServiceFactory
        {
            private Config config = new Config();

            public ServiceFactory(IConfiguration configuration)
            {
                config = configuration.Get<Config>();
            }

            public Config CreatePocoConfig()
            {
                return config;
            }
        }
    }
}
