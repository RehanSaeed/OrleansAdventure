using Orleans.Concurrency;
using System.Collections.Generic;

namespace AdventureGrainInterfaces
{
    [Immutable]
    public class MapInfo
    {
        public string Name { get; set; }
        public List<RoomInfo> Rooms { get; set; }
        public List<CategoryInfo> Categories { get; set; }
        public List<Thing> Things { get; set; }
        public List<MonsterInfo> Monsters { get; set; }
    }
}
