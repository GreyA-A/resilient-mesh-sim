using MeshSim.Core;
using Xunit;

namespace MeshSim.Tests;

public class NetworkPacketTests
{
    [Fact]
    public void ConstructorInitializesPropertiesCorrectly()
    {
        var packet = new NetworkPacket("pkt-1", "node-a", "node-b", 5, 128);

        Assert.Equal("pkt-1", packet.Id);
        Assert.Equal("node-a", packet.SourceNodeId);
        Assert.Equal("node-b", packet.DestinationNodeId);
        Assert.Equal(5, packet.Ttl);
        Assert.Equal(0, packet.HopCount);
        Assert.Equal(128, packet.PayloadBytes);
    }

    [Theory]
    [InlineData("", "node-a", "node-b", 5, 64)]
    [InlineData("   ", "node-a", "node-b", 5, 64)]
    [InlineData("pkt-1", "", "node-b", 5, 64)]
    [InlineData("pkt-1", "node-a", "", 5, 64)]
    public void ConstructorThrowsArgumentExceptionWhenIdentifiersAreEmpty(string id, string src, string dst, int ttl, int payload)
    {
        Assert.Throws<ArgumentException>(() => new NetworkPacket(id, src, dst, ttl, payload));
    }

    [Theory]
    [InlineData(0, 64)]
    [InlineData(-1, 64)]
    [InlineData(5, -10)]
    public void ConstructorThrowsArgumentOutOfRangeExceptionWhenNumericParametersAreInvalid(int ttl, int payload)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NetworkPacket("pkt-1", "node-a", "node-b", ttl, payload));
    }

    [Fact]
    public void ForwardHopDecrementsTtlAndIncrementsHopCount()
    {
        var packet = new NetworkPacket("pkt-1", "node-a", "node-b", 2, 64);

        bool firstHopAlive = packet.ForwardHop();
        Assert.True(firstHopAlive);
        Assert.Equal(1, packet.Ttl);
        Assert.Equal(1, packet.HopCount);

        bool secondHopAlive = packet.ForwardHop();
        Assert.False(secondHopAlive);
        Assert.Equal(0, packet.Ttl);
        Assert.Equal(2, packet.HopCount);

        bool deadHop = packet.ForwardHop();
        Assert.False(deadHop);
        Assert.Equal(0, packet.Ttl);
        Assert.Equal(2, packet.HopCount);
    }
}