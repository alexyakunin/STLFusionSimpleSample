using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Stl.DependencyInjection;
using Stl.Fusion;
using Stl.Fusion.Blazor;
using Stl.Fusion.Client;
using Stl.OS;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace STLFusionSimpleSample.Client
{
    public class Program
    {
        public const string ClientSideScope = nameof(ClientSideScope);

        public static Task Main(string[] args)
        {
            if (OSInfo.Kind != OSKind.WebAssembly)
                throw new ApplicationException("This app runs only in browser.");

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            ConfigureServices(builder.Services, builder);
            builder.RootComponents.Add<App>("app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            var host = builder.Build();

            var runTask = host.RunAsync();
            Task.Run(async () =>
            {
                var hostedServices = host.Services.GetService<IEnumerable<IHostedService>>();
                foreach (var hostedService in hostedServices)
                    await hostedService.StartAsync(default);
            });
            return runTask;
        }

        public static void ConfigureServices(IServiceCollection services, WebAssemblyHostBuilder builder)
        {
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddDebug();
            });

            var baseUri = new Uri(builder.HostEnvironment.BaseAddress);
            var apiBaseUri = new Uri($"{baseUri}api/");

            var fusion = services.AddFusion();
            var fusionClient = fusion.AddRestEaseClient(
                (c, o) =>
                {
                    o.BaseUri = baseUri;
                    o.MessageLogLevel = LogLevel.Information;
                }).ConfigureHttpClientFactory(
                (c, name, o) =>
                {
                    var isFusionClient = (name ?? "").StartsWith("Stl.Fusion");
                    var clientBaseUri = isFusionClient ? baseUri : apiBaseUri;
                    o.HttpClientActions.Add(client => client.BaseAddress = clientBaseUri);
                });
            var fusionAuth = fusion.AddAuthentication().AddClient().AddBlazor();

            // This method registers services marked with any of ServiceAttributeBase descendants, including:
            // [Service], [ComputeService], [RestEaseReplicaService], [LiveStateUpdater]

            services.AttributeBased(ClientSideScope).AddServicesFrom(Assembly.GetExecutingAssembly());

            //foreach (var item in services)
            //{
            //    if (item.ServiceType == typeof(IListExampleService) || item.ServiceType == typeof(IChatService))
            //    {
            //        Console.WriteLine(item?.ToString());
            //    }

            //}
            ConfigureSharedServices(services);
        }

        public static void ConfigureSharedServices(IServiceCollection services)
        {
            // Default delay for update delayers
            //services.AddSingleton(c => new UpdateDelayer.Options()
            //{
            //    Delay = TimeSpan.FromSeconds(0.1),
            //});

            //services.AddSingleton<IPluralize, Pluralizer>();

            // This method registers services marked with any of ServiceAttributeBase descendants, including:
            // [Service], [ComputeService], [RestEaseReplicaService], [LiveStateUpdater]
            services.AttributeBased().AddServicesFrom(Assembly.GetExecutingAssembly());
        }
    }
}