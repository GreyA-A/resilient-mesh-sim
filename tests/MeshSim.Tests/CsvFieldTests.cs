using MeshSim.Engine.IO;

namespace MeshSim.Tests;

public class CsvFieldTests
{
    [Theory]
    [InlineData("=SUM(A1)", "'=SUM(A1)")]
    [InlineData("+1", "'+1")]
    [InlineData("-1", "'-1")]
    [InlineData("@cmd", "'@cmd")]
    [InlineData("\tx", "'\tx")]
    [InlineData("\rx", "\"'\rx\"")]
    [InlineData("=a,b", "\"'=a,b\"")]
    [InlineData("a=b", "a=b")]
    [InlineData("plain", "plain")]
    [InlineData("a,b", "\"a,b\"")]
    [InlineData("say \"hi\"", "\"say \"\"hi\"\"\"")]
    [InlineData("line1\nline2", "\"line1\nline2\"")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void EscapeNeutralizesFormulasAndQuotesSpecialCharacters(string? value, string expected)
    {
        Assert.Equal(expected, CsvField.Escape(value));
    }
}