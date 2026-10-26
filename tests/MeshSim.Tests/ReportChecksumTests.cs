using System.Text;
using MeshSim.Engine.IO;

namespace MeshSim.Tests;

public class ReportChecksumTests
{
    private const string AbcDigest = "BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD";

    [Fact]
    public void ComputeMatchesKnownSha256TestVector()
    {
        Assert.Equal(AbcDigest, ReportChecksum.Compute(Encoding.UTF8.GetBytes("abc")));
    }

    [Theory]
    [InlineData("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")]
    [InlineData("  BA7816BF8F01CFEA414140DE5DAE2223B00361A396177A9CB410FF61F20015AD\n")]
    public void MatchesIgnoresCaseAndSurroundingWhitespace(string expected)
    {
        Assert.True(ReportChecksum.Matches(Encoding.UTF8.GetBytes("abc"), expected));
    }

    [Fact]
    public void MatchesReturnsFalseForDifferentContent()
    {
        Assert.False(ReportChecksum.Matches(Encoding.UTF8.GetBytes("abd"), AbcDigest));
    }

    [Fact]
    public void GetSidecarPathAppendsExtension()
    {
        Assert.Equal("report.json.sha256", ReportChecksum.GetSidecarPath("report.json"));
    }

    [Fact]
    public void MethodsValidateArguments()
    {
        Assert.ThrowsAny<ArgumentException>(() => ReportChecksum.GetSidecarPath("  "));
        Assert.Throws<ArgumentNullException>(() => ReportChecksum.Compute(null!));
        Assert.Throws<ArgumentNullException>(() => ReportChecksum.Matches([1], null!));
    }
}