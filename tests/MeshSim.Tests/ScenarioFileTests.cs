using System.Text;
using MeshSim.Engine.IO;
using static MeshSim.Tests.ScenarioTestData;

namespace MeshSim.Tests;

public class ScenarioFileTests
{
    [Fact]
    public void SaveThenLoadRestoresIdenticalScenario()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("scenario.json");
        ScenarioDefinition original = JammedChain();

        ScenarioFile.Save(path, original);
        ScenarioDefinition loaded = ScenarioFile.Load(path);

        Assert.Equal(ScenarioSerializer.Serialize(original), ScenarioSerializer.Serialize(loaded));
    }

    [Fact]
    public void SaveOverwritesExistingFileAndLeavesNoTemporaryFiles()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("scenario.json");
        ScenarioFile.Save(path, Scenario(name: "First"));

        ScenarioFile.Save(path, Scenario(name: "Second"));

        Assert.Equal("Second", ScenarioFile.Load(path).Name);
        Assert.Empty(Directory.GetFiles(temp.DirectoryPath, "*.tmp"));
    }

    [Fact]
    public void SaveDoesNotWriteFileForInvalidScenario()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("scenario.json");

        Assert.Throws<InvalidDataException>(() => ScenarioFile.Save(path, Scenario(schemaVersion: 99)));

        Assert.False(File.Exists(path));
    }

    [Fact]
    public void LoadThrowsFileNotFoundExceptionForMissingFile()
    {
        using var temp = new TempDirectory();

        Assert.Throws<FileNotFoundException>(() => ScenarioFile.Load(temp.FilePath("missing.json")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("{ broken")]
    [InlineData("{\"schemaVersion\":99,\"name\":\"x\"}")]
    [InlineData("{\"schemaVersion\":1,\"name\":\"x\",\"nodes\":[{\"id\":\"A\",\"transmissionRangeMeters\":0}]}")]
    public void LoadThrowsInvalidDataExceptionForEmptyCorruptedOrInvalidFile(string content)
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("scenario.json");
        File.WriteAllText(path, content);

        Assert.Throws<InvalidDataException>(() => ScenarioFile.Load(path));
    }

    [Fact]
    public void LoadAcceptsFileWithUtf8ByteOrderMark()
    {
        using var temp = new TempDirectory();
        string path = temp.FilePath("scenario.json");
        File.WriteAllText(path, ScenarioSerializer.Serialize(Scenario(name: "З BOM")), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        Assert.Equal("З BOM", ScenarioFile.Load(path).Name);
    }

    [Fact]
    public void SaveAndLoadValidateArguments()
    {
        Assert.ThrowsAny<ArgumentException>(() => ScenarioFile.Save("   ", Scenario()));
        Assert.Throws<ArgumentNullException>(() => ScenarioFile.Save("scenario.json", null!));
        Assert.ThrowsAny<ArgumentException>(() => ScenarioFile.Load("   "));
    }
}