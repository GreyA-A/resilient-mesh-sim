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
    [InlineData("pkt-1", "   ", "node-b", 5, 64)]
    [InlineData("pkt-1", "node-a", "", 5, 64)]
    [InlineData("pkt-1", "node-a", "   ", 5, 64)]
    public void ConstructorThrowsArgumentExceptionWhenIdentifiersAreEmpty(string id, string src, string dst, int ttl, int payload)
    {
        Assert.Throws<ArgumentException>(() => new NetworkPacket(id, src, dst, ttl, payload));
    }

    [Theory]
    [InlineData(null, "node-a", "node-b")]
    [InlineData("pkt-1", null, "node-b")]
    [InlineData("pkt-1", "node-a", null)]
    public void ConstructorThrowsArgumentExceptionWhenIdentifiersAreNull(string? id, string? src, string? dst)
    {
        Assert.Throws<ArgumentException>(() => new NetworkPacket(id!, src!, dst!, 5, 64));
    }

    [Theory]
    [InlineData(0, 64)]
    [InlineData(-1, 64)]
    [InlineData(int.MinValue, 64)]
    [InlineData(5, -10)]
    [InlineData(5, int.MinValue)]
    public void ConstructorThrowsArgumentOutOfRangeExceptionWhenNumericParametersAreInvalid(int ttl, int payload)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new NetworkPacket("pkt-1", "node-a", "node-b", ttl, payload));
    }

    [Fact]
    public void ConstructorRejectsZeroPayload()
    {
        // Фіксує поточну поведінку: нульовий payload заборонений (перевірка <= 0).
        // Якщо порожні пакети мають бути дозволені — змініть перевірку на "< 0" і цей тест.
        Assert.Throws<ArgumentOutOfRangeException>(() => new NetworkPacket("pkt-1", "node-a", "node-b", 5, 0));
    }

    [Fact]
    public void ConstructorAcceptsMinimumAndMaximumValidNumericValues()
    {
        var minimal = new NetworkPacket("pkt-1", "node-a", "node-b", 1, 1);
        var maximal = new NetworkPacket("pkt-2", "node-a", "node-b", int.MaxValue, int.MaxValue);

        Assert.Equal(1, minimal.Ttl);
        Assert.Equal(1, minimal.PayloadBytes);
        Assert.Equal(int.MaxValue, maximal.Ttl);
        Assert.Equal(int.MaxValue, maximal.PayloadBytes);
    }

    [Fact]
    public void ConstructorAllowsSameSourceAndDestination()
    {
        var packet = new NetworkPacket("pkt-1", "node-a", "node-a", 3, 10);

        Assert.Equal(packet.SourceNodeId, packet.DestinationNodeId);
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

    [Fact]
    public void ForwardHopWithTtlOfOneExpiresImmediately()
    {
        var packet = new NetworkPacket("pkt-1", "node-a", "node-b", 1, 64);

        bool alive = packet.ForwardHop();

        Assert.False(alive);
        Assert.Equal(0, packet.Ttl);
        // Поточна поведінка: стрибок, що вичерпав TTL, все одно зараховується в HopCount.
        Assert.Equal(1, packet.HopCount);
    }

    [Fact]
    public void ForwardHopKeepsTtlPlusHopCountEqualToInitialTtl()
    {
        const int initialTtl = 5;
        var packet = new NetworkPacket("pkt-1", "node-a", "node-b", initialTtl, 64);

        for (int hop = 1; hop <= initialTtl; hop++)
        {
            bool alive = packet.ForwardHop();

            Assert.Equal(hop < initialTtl, alive);
            Assert.Equal(initialTtl - hop, packet.Ttl);
            Assert.Equal(hop, packet.HopCount);
            Assert.Equal(initialTtl, packet.Ttl + packet.HopCount);
        }
    }

    [Fact]
    public void ForwardHopDoesNotChangeImmutableProperties()
    {
        var packet = new NetworkPacket("pkt-1", "node-a", "node-b", 3, 256);

        packet.ForwardHop();

        Assert.Equal("pkt-1", packet.Id);
        Assert.Equal("node-a", packet.SourceNodeId);
        Assert.Equal("node-b", packet.DestinationNodeId);
        Assert.Equal(256, packet.PayloadBytes);
    }
}