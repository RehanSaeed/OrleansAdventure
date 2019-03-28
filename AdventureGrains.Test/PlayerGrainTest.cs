using System;
using System.Threading.Tasks;
using AdventureGrainInterfaces;
using Orleans.TestingHost;
using Xunit;

namespace AdventureGrains.Test
{
    [Collection(ClusterCollection.Name)]
    public class HelloGrainTests
    {
        private readonly TestCluster _cluster;

        public HelloGrainTests(ClusterFixture fixture)
        {
            _cluster = fixture.Cluster;
        }

        [Fact]
        public async Task SetName_NameSet_ReturnsName()
        {
            var player = _cluster.GrainFactory.GetGrain<IPlayerGrain>(Guid.NewGuid());
            await player.SetName("Foo");

            Assert.Equal("Foo", await player.Name());
        }
    }
}
