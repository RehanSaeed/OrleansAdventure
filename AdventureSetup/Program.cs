using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using AdventureGrainInterfaces;
using AdventureGrainInterfaces.Constants;
using AdventureGrains;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace AdventureSetup
{
    class Program
    {
        static async Task<int> Main(string [] args)
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string mapFileName = Path.Combine (path, "AdventureMap.json");

            switch (args.Length)
            {
                default:
                    Console.WriteLine("*** Invalid command line arguments.");
                    return -1;
                case 0:
                    break;
                case 1:
                    mapFileName = args[0];
                    break;
            }

            if (!File.Exists(mapFileName))
            {
                Console.WriteLine("*** File not found: {0}", mapFileName);
                return -2;
            }

            var siloHost = CreateSiloHostBuilder().Build();
            var clusterClient = CreateClientBuilder().Build();

            await RunAsync(siloHost, clusterClient, mapFileName);

            Console.ReadLine();

            await StopAsync(siloHost, clusterClient);

            return 0;
        }

        private static ISiloHostBuilder CreateSiloHostBuilder() =>
            new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = Cluster.ClusterId;
                    options.ServiceId = Cluster.ServiceId;
                })
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureServices(
                    services =>
                    {
                    })
                .ConfigureApplicationParts(
                    parts => parts.AddApplicationPart(typeof(RoomGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddConsole())
                // .AddMemoryGrainStorage("Default")
                .AddAzureTableGrainStorageAsDefault(options => options.ConnectionString = "UseDevelopmentStorage=true")
                .UseAzureTableReminderService(options => options.ConnectionString = "UseDevelopmentStorage=true")
                .UseTransactions(withStatisticsReporter: true)
                .AddAzureTableTransactionalStateStorageAsDefault(options => options.ConnectionString = "UseDevelopmentStorage=true");

        private static IClientBuilder CreateClientBuilder() =>
            new ClientBuilder()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = Cluster.ClusterId;
                    options.ServiceId = Cluster.ServiceId;
                })
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IRoomGrain).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddConsole())
                .UseAzureStorageClustering(options => options.ConnectionString = "UseDevelopmentStorage=true");

        static async Task RunAsync(ISiloHost silo, IClusterClient client, string mapFileName)
        {
            await silo.StartAsync();
            await client.Connect();

            Console.WriteLine("Map file name is '{0}'.", mapFileName);
            Console.WriteLine("Setting up Adventure, please wait ...");
            Adventure adventure = new Adventure(client);
            adventure.Configure(mapFileName).Wait();
            Console.WriteLine("Adventure setup completed.");
        }

        static async Task StopAsync(ISiloHost silo, IClusterClient client)
        {
            await client.Close();
            await silo.StopAsync();
        }
    }
}
