using AdventureGrainInterfaces;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AdventureGrains
{
    public class PlayerGrain : Orleans.Grain, IPlayerGrain
    {
        private readonly MyComponent component;
        IRoomGrain roomGrain; // Current room
        List<Thing> things = new List<Thing>(); // Things that the player is carrying
        bool killed = false;
        PlayerInfo myInfo;

        public PlayerGrain(MyComponent component)
        {
            Console.WriteLine("Started: " + RequestContext.Get("TraceId"));
            this.component = component;
        }

        public override Task OnActivateAsync()
        {
            this.myInfo = new PlayerInfo { Key = this.GetPrimaryKey(), Name = "nobody" };
            return base.OnActivateAsync();
        }

        public override Task OnDeactivateAsync()
        {
            return base.OnDeactivateAsync();
        }

        public override void Participate(IGrainLifecycle lifecycle)
        {
            base.Participate(lifecycle);
            lifecycle.Subscribe(this.GetType().FullName, GrainLifecycleStage.SetupState, OnSetupState);
        }

        private Task OnSetupState(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        ValueTask<string> IPlayerGrain.Name()
        {
            var moster = this.GrainFactory.GetGrain<IMonsterGrain>(0);

            return new ValueTask<string>(myInfo.Name);
        }

        Task<IRoomGrain> IPlayerGrain.RoomGrain()
        {
            return Task.FromResult(roomGrain);
        }


        async Task<string> IPlayerGrain.Die()
        {
            myInfo.Health -= 2;

            if (myInfo.Health == 0)
            {
                // Drop everything
                var tasks = new List<Task<string>>();
                foreach (var thing in new List<Thing>(things))
                {
                    tasks.Add(this.Drop(thing));
                }
                await Task.WhenAll(tasks);

                // Exit the game
                if (this.roomGrain != null)
                {
                    await this.roomGrain.Exit(myInfo);
                    this.roomGrain = null;
                    killed = true;
                }

                return this.myInfo.Name + " is now dead.";
            }
            else
            {
                return $"Ouch! {this.myInfo.Name} now has {this.myInfo.Health} health";
            }
        }

        async Task<string> Drop(Thing thing)
        {
            if ( killed )
                return await CheckAlive();

            if (thing != null)
            {
                this.things.Remove(thing);
                await this.roomGrain.Drop(thing);
                return "Okay.";
            }
            else
                return "I don't understand.";
        }

        async Task<string> Take(Thing thing)
        {
            if (killed)
                return await CheckAlive();

            if (thing != null)
            {
                this.things.Add(thing);
                await this.roomGrain.Take(thing);
                return "Okay.";
            }
            else
                return "I don't understand.";
        }


        Task IPlayerGrain.SetName(string name)
        {
            this.myInfo.Name = name;
            return Task.CompletedTask;
        }

        Task IPlayerGrain.SetRoomGrain(IRoomGrain room)
        {
            this.roomGrain = room;
            return room.Enter(myInfo);
        }

        async Task<string> Go(string direction)
        {
            IRoomGrain destination = await this.roomGrain.ExitTo(direction);

            StringBuilder description = new StringBuilder();

            if (destination != null)
            {
                await this.roomGrain.Exit(myInfo);
                await destination.Enter(myInfo);

                this.roomGrain = destination;
                var desc = await destination.Description(myInfo);

                if (desc != null)
                    description.Append(desc);
            }
            else
            {
                description.Append("You cannot go in that direction.");
            }

            if (things.Count > 0)
            {
                description.AppendLine("You are holding the following items:");
                foreach (var thing in things)
                {
                    description.AppendLine(thing.Name);
                }
            }

            return description.ToString();
        }

        async Task<string> CheckAlive()
        {
            if (!killed)
                return null;

            // Go to room '-2', which is the place of no return.
            var room = GrainFactory.GetGrain<IRoomGrain>(-2);
            return await room.Description(myInfo);
        }

        async Task<string> Kill(string target)
        {
            if (things.Count == 0)
                return "With what? Your bare hands?";

            var player = await this.roomGrain.FindPlayer(target);
            if (player != null)
            {
                var weapon = things.Where(t => t.Category == "weapon").FirstOrDefault();
                if (weapon != null)
                {
                    return await GrainFactory.GetGrain<IPlayerGrain>(player.Key).Die();
                }
                return "With what? Your bare hands?";
            }

            var monster = await this.roomGrain.FindMonster(target);
            if (monster != null)
            {
                var weapons = monster.KilledBy.Join(things, id => id, t => t.Id, (id, t) => t);
                if (weapons.Count() > 0)
                {
                    await GrainFactory.GetGrain<IMonsterGrain>(monster.Id).Kill(this.roomGrain);
                    return target + " is now dead.";
                }
                return "With what? Your bare hands?";
            }
            return "I can't see " + target + " here. Are you sure?";
        }

        private string RemoveStopWords(string s)
        {
            string[] stopwords = new string[] { " on ", " the ", " a " };

            StringBuilder sb = new StringBuilder(s);
            foreach (string word in stopwords)
            {
                sb.Replace(word, " ");
            }

            return sb.ToString();
        }

        private Thing FindMyThing(string name)
        {
            return things.Where(x => x.Name == name).FirstOrDefault();
        }

        private string Rest(string[] words)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 1; i < words.Length; i++)
                sb.Append(words[i] + " ");

            return sb.ToString().Trim().ToLower();
        }

        async Task<string> IPlayerGrain.Play(string command)
        {
            Thing thing;
            command = RemoveStopWords(command);

            string[] words = command.Split(' ');

            string verb = words[0].ToLower();

            if (killed && verb != "end")
                return await CheckAlive();

            switch (verb)
            {
                case "look":
                    return $"{myInfo.Name} has {myInfo.Health} health." + Environment.NewLine +
                        await this.roomGrain.Description(myInfo);

                case "go":
                    if (words.Length == 1)
                        return "Go where?";
                    return await Go(words[1]);

                case "north":
                case "south":
                case "east":
                case "west":
                    return await Go(verb);

                case "kill":
                    if (words.Length == 1)
                        return "Kill what?";
                    var target = command.Substring(verb.Length + 1);
                    return await Kill(target);

                case "drop":
                    thing = FindMyThing(Rest(words));
                    return await Drop(thing);

                case "take":
                    thing = await roomGrain.FindThing(Rest(words));
                    return await Take(thing);

                case "inv":
                case "inventory":
                    return "You are carrying: " + string.Join(" ", things.Select(x => x.Name));

                case "end":
                    return "";
            }
            return "I don't understand.";
        }
    }
}
