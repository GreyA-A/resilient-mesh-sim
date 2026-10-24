using MeshSim.Engine;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

public class ScenarioSerializerTests
{
    [Fact]
    public void SerializeUsesCamelCaseAndWritesSchemaVersion()
    {
        string json = ScenarioSerializer.Serialize(JammedChain());

        Assert.Contains("\"schemaVersion\": 1", json);
        Assert.Contains("\"routingStrategy\": \"Dijkstra\"", json);
        Assert.Contains("\"jammingZones\"", json);
        Assert.Contains("\"transmissionRangeMeters\"", json);
    }

    [Fact]
    public void DeserializeRestoresEveryFieldOfSerializedScenario()
    {
        ScenarioDefinition original = JammedChain();
        string json = ScenarioSerializer.Serialize(original);

        ScenarioDefinition restored = ScenarioSerializer.Deserialize(json);

        Assert.Equal(original.Name, restored.Name);
        Assert.Equal(original.Engine.HopDelayMs, restored.Engine.HopDelayMs);
        Assert.Equal(original.Nodes.Count, restored.Nodes.Count);
        Assert.Equal("B", restored.Nodes[1].Id);
        Assert.Equal(60.0, restored.Nodes[1].Y);
        Assert.Equal(25.0, restored.JammingZones[0].RadiusMeters);
        Assert.Equal("C", restored.Packets[0].DestinationNodeId);
        // Повторна серіалізація має дати ідентичний текст: жодне поле не загубилось.
        Assert.Equal(json, ScenarioSerializer.Serialize(restored));
    }

    [Fact]
    public void DeserializeAppliesDefaultsForOptionalSections()
    {
        ScenarioDefinition scenario = ScenarioSerializer.Deserialize("{\"schemaVersion\":1,\"name\":\"Minimal\"}");

        Assert.Equal("Minimal", scenario.Name);
        Assert.Equal("Dijkstra", scenario.RoutingStrategy);
        Assert.Equal(SimulationEngine.DefaultHopDelayMs, scenario.Engine.HopDelayMs);
        Assert.Empty(scenario.Nodes);
        Assert.Empty(scenario.JammingZones);
        Assert.Empty(scenario.Packets);
    }

    [Fact]
    public void DeserializeDoesNotValidateSemantics()
    {
        ScenarioDefinition scenario = ScenarioSerializer.Deserialize("{\"schemaVersion\":99}");

        Assert.Equal(99, scenario.SchemaVersion);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    [InlineData("{")]
    [InlineData("[]")]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\":\"one\"}")]
    [InlineData("{\"schemaVersion\":1,\"unexpected\":true}")]
    [InlineData("{\"schemaVersion\":1,\"nodes\":[{\"id\":\"A\",\"x\":\"far\"}]}")]
    public void DeserializeThrowsInvalidDataExceptionForBrokenOrUnexpectedContent(string json)
    {
        Assert.Throws<InvalidDataException>(() => ScenarioSerializer.Deserialize(json));
    }

    [Fact]
    public void SerializeAndDeserializeThrowArgumentNullExceptionForNull()
    {
        Assert.Throws<ArgumentNullException>(() => ScenarioSerializer.Serialize(null!));
        Assert.Throws<ArgumentNullException>(() => ScenarioSerializer.Deserialize(null!));
    }
}