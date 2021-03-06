using System;
using System.Threading.Tasks;
using AdventureGrainInterfaces;
using Orleans;
using Orleans.Runtime;

namespace AdventureGrains
{
    public class MonsterGrain : Grain, IMonsterGrain, IRemindable
    {
        private MonsterInfo monsterInfo = new MonsterInfo();
        private IRoomGrain roomGrain; // Current room
        private IGrainReminder moveMonsterReminder;

        public MonsterGrain()
        {
        }

        public override async Task OnActivateAsync()
        {
            this.monsterInfo.Id = this.GetPrimaryKeyLong();
            this.moveMonsterReminder = await RegisterOrUpdateReminder(
                "MoveMonster",
                TimeSpan.FromSeconds(150),
                TimeSpan.FromSeconds(60));
            await base.OnActivateAsync();
        }

        private Task OnTimerTick(object arg)
        {
            return Move();
        }

        Task IMonsterGrain.SetInfo(MonsterInfo info)
        {
            this.monsterInfo = info;
            return Task.CompletedTask;
        }

        Task<string> IMonsterGrain.Name()
        {
            return Task.FromResult(this.monsterInfo.Name);
        }

        async Task IMonsterGrain.SetRoomGrain(IRoomGrain room)
        {
            if (this.roomGrain != null)
                await this.roomGrain.Exit(this.monsterInfo);
            this.roomGrain = room;
            await this.roomGrain.Enter(this.monsterInfo);
        }

        Task<IRoomGrain> IMonsterGrain.RoomGrain()
        {
            return Task.FromResult(roomGrain);
        }

        async Task Move()
        {
            var directions = new string [] { "north", "south", "west", "east" };

            var rand = new Random().Next(0, 4);
            IRoomGrain nextRoom = await this.roomGrain.ExitTo(directions[rand]);

            if (null == nextRoom)
                return;

            await this.roomGrain.Exit(this.monsterInfo);
            await nextRoom.Enter(this.monsterInfo);

            this.roomGrain = nextRoom;
        }


        Task<string> IMonsterGrain.Kill(IRoomGrain room)
        {
            if (this.roomGrain != null)
            {
                if (this.roomGrain.GetPrimaryKey() != room.GetPrimaryKey())
                {
                    return Task.FromResult(monsterInfo.Name + " snuck away. You were too slow!");
                }
                return this.roomGrain.Exit(this.monsterInfo).ContinueWith(t => monsterInfo.Name + " is dead.");
            }
            return Task.FromResult(monsterInfo.Name + " is already dead. You were too slow and someone else got to him!");
        }

        public Task ReceiveReminder(string reminderName, TickStatus status)
        {
            Console.WriteLine("Thanks for reminding me-- I almost forgot!");
            return this.Move();
        }
    }
}
