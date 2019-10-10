using System;
using System.Collections.Generic;
using System.Linq;
using DedicatedHostsManager;
using Microsoft.ApplicationInsights.AspNetCore;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation.ApplicationId;
using Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse;
using Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

[assembly: FunctionsStartup(typeof(Startup))]
namespace DedicatedHostsManager
{
    internal class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile($"local.settings.json", optional:true, reloadOnChange:true)                
                .AddUserSecrets<Startup>()
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddSingleton<IConfiguration>(config);

            #region Begin App Insights configuration workaround 
            builder.Services.AddOptions<TelemetryConfiguration>()
                .Configure<IEnumerable<ITelemetryModuleConfigurator>, IEnumerable<ITelemetryModule>>((telemetryConfig, configurators, modules) =>
                {
                    // Run through the registered configurators
                    foreach (var configurator in configurators)
                    {
                        ITelemetryModule telemetryModule = modules.FirstOrDefault((module) => module.GetType() == configurator.TelemetryModuleType);
                        if (telemetryModule != null)
                        {
                            // next line of code is giving us a compiler warning (OBSOLETE)
                            configurator.Configure(telemetryModule);
                        }
                    }
                });

            // The ConfigureTelemetryModule() call on the next line results in the following exception:
            // FunctionAppTest: Method Microsoft.Extensions.DependencyInjection.ApplicationInsightsExtensions.ConfigureTelemetryModule: 
            // type argument 'Microsoft.ApplicationInsights.Extensibility.PerfCounterCollector.QuickPulse.QuickPulseTelemetryModule' violates the constraint of type parameter 'T'.
            builder.Services.ConfigureTelemetryModule<QuickPulseTelemetryModule>((module, o) => module.QuickPulseServiceEndpoint = "https://quickpulse.applicationinsights.us/QuickPulseService.svc");
            builder.Services.AddSingleton<IApplicationIdProvider>(_ => new ApplicationInsightsApplicationIdProvider() { ProfileQueryEndpoint = "https://dc.applicationinsights.us/api/profiles/{0}/appId" });
            builder.Services.AddSingleton<ITelemetryChannel>(s =>
            {
                // HACK: Need to force the options factory to run somewhere so it'll run through our Configurators.
                var ignore = s.GetService<IOptions<TelemetryConfiguration>>().Value;

                return new ServerTelemetryChannel { EndpointAddress = "https://dc.applicationinsights.us/v2/track", DeveloperMode = true };
            });
            #endregion

            builder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>("", LogLevel.Information);
                loggingBuilder.AddConsole();
                loggingBuilder.AddFilter<Microsoft.Extensions.Logging.Console.ConsoleLoggerProvider>("", LogLevel.Information);
            });

            builder.Services.AddTransient<IDedicatedHostEngine, DedicatedHostEngine>();
            builder.Services.AddTransient<IDedicatedHostSelector, DedicatedHostSelector>();
            builder.Services.AddTransient<ISyncProvider, SyncProvider>();
        }
    }
}
