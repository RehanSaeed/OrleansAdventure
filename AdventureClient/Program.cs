using System;
using System.Threading.Tasks;
using AdventureGrainInterfaces;
using AdventureGrainInterfaces.Constants;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Streams;

namespace AdventureClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var client = new ClientBuilder()
                .UseAzureStorageClustering(options => options.ConnectionString = "UseDevelopmentStorage=true")
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = Cluster.ClusterId;
                    options.ServiceId = Cluster.ServiceId;
                })
                .ConfigureApplicationParts(
                    parts => parts
                        .AddApplicationPart(typeof(IRoomGrain).Assembly)
                        .WithReferences())
                .AddSimpleMessageStreamProvider(StreamProviderName.Default)
                .Build();

            await client.Connect();

            var streamProvider = client.GetStreamProvider(StreamProviderName.Default);
            var monsterEnteredRoomStream = streamProvider.GetStream<(MonsterInfo monsterInfo, RoomInfo roomInfo)>(Guid.Empty, StreamName.MonsterEnteredRoom);
            var playerEnteredRoomStream = streamProvider.GetStream<(PlayerInfo playerInfo, RoomInfo roomInfo)>(Guid.Empty, StreamName.PlayerEnteredRoom);
            var monsterEnteredRoomSubscription = await monsterEnteredRoomStream.SubscribeAsync(
                async (x, token) =>
                {
                    Console.WriteLine($"{x.monsterInfo.Name} entered {x.roomInfo.Name}");
                });
            var playerEnteredRoomSubscription = await playerEnteredRoomStream.SubscribeAsync(
                async (x, token) =>
                {
                    Console.WriteLine($"{x.playerInfo.Name} entered {x.roomInfo.Name}");
                });

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
            await player.SetName(name);
            var room1 = client.GetGrain<IRoomGrain>(0);
            await player.SetRoomGrain(room1);

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
