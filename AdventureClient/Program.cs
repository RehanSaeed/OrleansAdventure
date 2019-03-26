using System;
using AdventureGrainInterfaces;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;

namespace AdventureClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "AdventureApp";
                })
                .UseAzureStorageClustering(options => options.ConnectionString = "UseDevelopmentStorage=true")
                .ConfigureApplicationParts(
                    parts => parts
                        .AddApplicationPart(typeof(IRoomGrain).Assembly)
                        .WithReferences())
                .Build();

            client.Connect().Wait();

            Console.WriteLine(@"
  ___      _                 _                  
 / _ \    | |               | |                 
/ /_\ \ __| |_   _____ _ __ | |_ _   _ _ __ ___ 
|  _  |/ _` \ \ / / _ \ '_ \| __| | | | '__/ _ \
| | | | (_| |\ V /  __/ | | | |_| |_| | | |  __/
\_| |_/\__,_| \_/ \___|_| |_|\__|\__,_|_|  \___|");

            Console.WriteLine();
            Console.WriteLine("What's your name?");
            string name = Console.ReadLine();

            RequestContext.Set("TraceId", new Guid());

            var player = client.GetGrain<IPlayerGrain>(Guid.NewGuid());
            player.SetName(name).Wait();
            var room1 = client.GetGrain<IRoomGrain>(0);
            player.SetRoomGrain(room1).Wait();

            Console.WriteLine(player.Play("look").Result);

            string result = "Start";

            try
            {
                while (result != "")
                {
                    string command = Console.ReadLine();

                    result = player.Play(command).Result;
                    Console.WriteLine(result);
                }
            }
            finally
            {
                Console.WriteLine(player.Die().Result);
                Console.WriteLine("Game over!");
            }
        }
    }
}
