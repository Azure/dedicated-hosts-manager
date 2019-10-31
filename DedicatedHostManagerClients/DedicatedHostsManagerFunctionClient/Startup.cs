using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using TrafficGeneratorFunction;

[assembly: FunctionsStartup(typeof(Startup))]
namespace TrafficGeneratorFunction
{
    /// <summary>
    /// Function startup.
    /// </summary>
    internal class Startup : FunctionsStartup
    {
        /// <summary>
        /// Function Configuration.
        /// </summary>
        /// <param name="builder">Function Host Builder.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile($"appsettings.json", optional:true, reloadOnChange:true)                
                .AddUserSecrets<Startup>()
                .AddEnvironmentVariables()
                .Build();

            builder.Services.AddSingleton<IConfiguration>(config);
            builder.Services.AddHttpClient();
        }
    }
}
