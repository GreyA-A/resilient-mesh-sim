using MeshSim.Desktop.Services;

namespace MeshSim.Tests;

public class FileNameHelperTests
{
    [Theory]
    [InlineData("Demo", ".json", "Demo.json")]
    [InlineData("Демонстраційний сценарій", ".json", "Демонстраційний-сценарій.json")]
    [InlineData("A/B C", ".json", "A-B-C.json")]
    [InlineData("  spaced  ", ".csv", "spaced.csv")]
    [InlineData("", ".json", "scenario.json")]
    [InlineData("   ", ".json", "scenario.json")]
    [InlineData("///", ".csv", "scenario.csv")]
    public void SuggestReplacesInvalidAndWhitespaceCharactersAndFallsBackWhenEmpty(string name, string extension, string expected)
    {
        Assert.Equal(expected, FileNameHelper.Suggest(name, extension));
    }

    [Fact]
    public void FallbackStemIsScenario()
    {
        Assert.Equal("scenario", FileNameHelper.FallbackStem);
    }

    [Fact]
    public void SuggestThrowsArgumentNullExceptionForNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => FileNameHelper.Suggest(null!, ".json"));
        Assert.Throws<ArgumentNullException>(() => FileNameHelper.Suggest("name", null!));
    }
}