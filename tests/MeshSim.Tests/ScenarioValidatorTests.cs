using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

public class ScenarioValidatorTests
{
    private static void AssertHasError(IReadOnlyList<string> errors, string fragment)
    {
        Assert.Contains(errors, error => error.Contains(fragment, StringComparison.Ordinal));
    }

    // ---------- Загальна поведінка ----------

    [Fact]
    public void ValidateAcceptsCompleteScenario()
    {
        Assert.Empty(ScenarioValidator.Validate(JammedChain()));
    }

    [Fact]
    public void ValidateAcceptsScenarioWithoutNodesZonesAndPackets()
    {
        ScenarioDefinition scenario = Scenario(nodes: Array.Empty<NodeDefinition>());

        Assert.Empty(ScenarioValidator.Validate(scenario));
    }

    [Fact]
    public void ValidateAndEnsureValidThrowArgumentNullExceptionForNullScenario()
    {
        Assert.Throws<ArgumentNullException>(() => ScenarioValidator.Validate(null!));
        Assert.Throws<ArgumentNullException>(() => ScenarioValidator.EnsureValid(null!));
    }

    [Fact]
    public void ValidateCollectsAllErrorsInsteadOfStoppingAtTheFirstOne()
    {
        ScenarioDefinition scenario = Scenario(
            nodes: new[] { Node("A", range: 0.0, battery: 200.0) },
            zones: new[] { Zone("z", radius: -1.0) });

        IReadOnlyList<string> errors = ScenarioValidator.Validate(scenario);

        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public void EnsureValidDoesNotThrowForValidScenario()
    {
        Exception? exception = Record.Exception(() => ScenarioValidator.EnsureValid(JammedChain()));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureValidThrowsInvalidDataExceptionListingAllErrors()
    {
        ScenarioDefinition scenario = Scenario(
            nodes: new[] { Node("A", range: 0.0) },
            zones: new[] { Zone("z", radius: 0.0) });

        var exception = Assert.Throws<InvalidDataException>(() => ScenarioValidator.EnsureValid(scenario));

        Assert.Contains("nodes[0]", exception.Message, StringComparison.Ordinal);
        Assert.Contains("jammingZones[0]", exception.Message, StringComparison.Ordinal);
    }

    // ---------- Версія схеми, назва, стратегія ----------

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    [InlineData(-1)]
    public void ValidateRejectsUnsupportedSchemaVersionAndStopsEarly(int version)
    {
        // Назва теж порожня, але після помилки версії перевірки припиняються.
        IReadOnlyList<string> errors = ScenarioValidator.Validate(Scenario(schemaVersion: version, name: string.Empty));

        string single = Assert.Single(errors);
        Assert.Contains("версія схеми", single, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateRejectsBlankScenarioName(string? name)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(name: name!)), "Назва сценарію");
    }

    [Theory]
    [InlineData("OSPF")]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateRejectsUnknownRoutingStrategy(string? strategy)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(routingStrategy: strategy!)), "Невідома стратегія");
    }

    [Fact]
    public void ValidateAcceptsRoutingStrategyIgnoringCase()
    {
        Assert.Empty(ScenarioValidator.Validate(Scenario(routingStrategy: "dIjKsTrA")));
    }

    // ---------- Параметри рушія ----------

    [Theory]
    [InlineData(0.0, 1.0)]
    [InlineData(-5.0, 1.0)]
    [InlineData(double.NaN, 1.0)]
    [InlineData(double.PositiveInfinity, 1.0)]
    [InlineData(10.0, -1.0)]
    [InlineData(10.0, double.NaN)]
    [InlineData(10.0, double.PositiveInfinity)]
    public void ValidateRejectsInvalidEngineSettings(double hopDelay, double energy)
    {
        var engine = new EngineSettingsDefinition { HopDelayMs = hopDelay, EnergyPerTransmissionPercent = energy };

        AssertHasError(ScenarioValidator.Validate(Scenario(engine: engine)), "engine.");
    }

    [Fact]
    public void ValidateAcceptsZeroEnergyConsumption()
    {
        var engine = new EngineSettingsDefinition { HopDelayMs = 10.0, EnergyPerTransmissionPercent = 0.0 };

        Assert.Empty(ScenarioValidator.Validate(Scenario(engine: engine)));
    }

    [Fact]
    public void ValidateRejectsNullEngineSection()
    {
        var scenario = new ScenarioDefinition
        {
            SchemaVersion = ScenarioDefinition.CurrentSchemaVersion,
            Name = "No engine",
            Engine = null!
        };

        AssertHasError(ScenarioValidator.Validate(scenario), "'engine'");
    }

    // ---------- Колекції ----------

    [Fact]
    public void ValidateRejectsNullCollections()
    {
        var withoutNodes = new ScenarioDefinition { SchemaVersion = 1, Name = "x", Nodes = null! };
        var withoutZones = new ScenarioDefinition { SchemaVersion = 1, Name = "x", JammingZones = null! };
        var withoutPackets = new ScenarioDefinition { SchemaVersion = 1, Name = "x", Packets = null! };

        AssertHasError(ScenarioValidator.Validate(withoutNodes), "'nodes'");
        AssertHasError(ScenarioValidator.Validate(withoutZones), "'jammingZones'");
        AssertHasError(ScenarioValidator.Validate(withoutPackets), "'packets'");
    }

    [Fact]
    public void ValidateRejectsNullListElements()
    {
        NodeDefinition[] nodes = [null!];

        AssertHasError(ScenarioValidator.Validate(Scenario(nodes: nodes)), "nodes[0]");
    }

    // ---------- Вузли ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateRejectsBlankNodeId(string? id)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(nodes: new[] { Node(id!) })), "nodes[0]");
    }

    [Fact]
    public void ValidateRejectsDuplicateNodeIds()
    {
        ScenarioDefinition scenario = Scenario(nodes: new[] { Node("A"), Node("A", x: 10.0) });

        IReadOnlyList<string> errors = ScenarioValidator.Validate(scenario);

        AssertHasError(errors, "nodes[1]");
        AssertHasError(errors, "дублюється");
    }

    [Fact]
    public void ValidateTreatsNodeIdsAsCaseSensitive()
    {
        Assert.Empty(ScenarioValidator.Validate(Scenario(nodes: new[] { Node("A"), Node("a", x: 10.0) })));
    }

    [Fact]
    public void ValidateAllowsSameIdInDifferentEntityTypes()
    {
        ScenarioDefinition scenario = Scenario(
            nodes: new[] { Node("A"), Node("B", x: 50.0), Node("X", x: 90.0) },
            zones: new[] { Zone("X") },
            packets: new[] { Packet("X", "A", "B") });

        Assert.Empty(ScenarioValidator.Validate(scenario));
    }

    [Theory]
    [InlineData(double.NaN, 0.0)]
    [InlineData(0.0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0.0)]
    [InlineData(0.0, double.NegativeInfinity)]
    public void ValidateRejectsNonFiniteNodeCoordinates(double x, double y)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(nodes: new[] { Node("A", x, y) })), "координати");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ValidateRejectsInvalidTransmissionRange(double range)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(nodes: new[] { Node("A", range: range) })), "transmissionRangeMeters");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(100.1)]
    [InlineData(double.NaN)]
    public void ValidateRejectsBatteryOutsideAllowedRange(double battery)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(nodes: new[] { Node("A", battery: battery) })), "batteryLevelPercent");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(100.0)]
    public void ValidateAcceptsBatteryBoundaryValues(double battery)
    {
        Assert.Empty(ScenarioValidator.Validate(Scenario(nodes: new[] { Node("A", battery: battery) })));
    }

    // ---------- Зони завад ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateRejectsBlankZoneId(string? id)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(zones: new[] { Zone(id!) })), "jammingZones[0]");
    }

    [Fact]
    public void ValidateRejectsDuplicateZoneIds()
    {
        ScenarioDefinition scenario = Scenario(zones: new[] { Zone("z"), Zone("z", x: 10.0) });

        AssertHasError(ScenarioValidator.Validate(scenario), "jammingZones[1]");
    }

    [Theory]
    [InlineData(double.NaN, 0.0)]
    [InlineData(0.0, double.PositiveInfinity)]
    public void ValidateRejectsNonFiniteZoneCoordinates(double x, double y)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(zones: new[] { Zone("z", x, y) })), "координати");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ValidateRejectsInvalidZoneRadius(double radius)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(zones: new[] { Zone("z", radius: radius) })), "radiusMeters");
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    [InlineData(double.NaN)]
    public void ValidateRejectsZoneIntensityOutsideAllowedRange(double intensity)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(zones: new[] { Zone("z", intensity: intensity) })), "jammingIntensity");
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void ValidateAcceptsZoneIntensityBoundaryValues(double intensity)
    {
        Assert.Empty(ScenarioValidator.Validate(Scenario(zones: new[] { Zone("z", intensity: intensity) })));
    }

    [Fact]
    public void ValidateAcceptsInactiveZone()
    {
        Assert.Empty(ScenarioValidator.Validate(Scenario(zones: new[] { Zone("z", isActive: false) })));
    }

    // ---------- Пакети ----------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void ValidateRejectsBlankPacketId(string? id)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(packets: new[] { Packet(id!) })), "packets[0]");
    }

    [Fact]
    public void ValidateRejectsDuplicatePacketIds()
    {
        ScenarioDefinition scenario = Scenario(packets: new[] { Packet("p"), Packet("p") });

        AssertHasError(ScenarioValidator.Validate(scenario), "packets[1]");
    }

    [Theory]
    [InlineData("", "B")]
    [InlineData("A", "   ")]
    [InlineData(null, "B")]
    [InlineData("A", null)]
    public void ValidateRejectsBlankPacketEndpoints(string? source, string? destination)
    {
        ScenarioDefinition scenario = Scenario(packets: new[] { Packet("p", source!, destination!) });

        AssertHasError(ScenarioValidator.Validate(scenario), "packets[0]");
    }

    [Theory]
    [InlineData("GHOST", "B")]
    [InlineData("A", "GHOST")]
    public void ValidateRejectsPacketEndpointsMissingFromNodeList(string source, string destination)
    {
        ScenarioDefinition scenario = Scenario(packets: new[] { Packet("p", source, destination) });

        AssertHasError(ScenarioValidator.Validate(scenario), "не існує");
    }

    [Fact]
    public void ValidateAcceptsPacketWithSameSourceAndDestination()
    {
        Assert.Empty(ScenarioValidator.Validate(Scenario(packets: new[] { Packet("p", "A", "A") })));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateRejectsNonPositiveTtl(int ttl)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(packets: new[] { Packet("p", ttl: ttl) })), "ttl");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateRejectsNonPositivePayload(int payload)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(packets: new[] { Packet("p", payloadBytes: payload) })), "payloadBytes");
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void ValidateRejectsInvalidScheduledTime(double time)
    {
        AssertHasError(ScenarioValidator.Validate(Scenario(packets: new[] { Packet("p", scheduledTimeMs: time) })), "scheduledTimeMs");
    }
}