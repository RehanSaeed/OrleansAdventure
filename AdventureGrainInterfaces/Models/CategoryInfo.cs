using Orleans.Concurrency;
using System;
using System.Collections.Generic;

namespace AdventureGrainInterfaces
{
    [Immutable]
    public class CategoryInfo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public List<String> Commands { get; set; }
    }
}
