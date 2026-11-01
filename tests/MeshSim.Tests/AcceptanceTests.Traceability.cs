using System.Globalization;
using System.Reflection;

namespace MeshSim.Tests;

public partial class AcceptanceTests
{
    private const string TestReferenceMarker = "AcceptanceTests.";
    private const int CaseCount = 8;

    [Fact]
    public void AcceptanceDocumentReferencesEveryAcceptanceTest()
    {
        string text = File.ReadAllText(RepositoryPaths.AcceptanceDocument());

        for (int number = 1; number <= CaseCount; number++)
        {
            string id = string.Create(CultureInfo.InvariantCulture, $"AC-{number:00}");

            Assert.Contains(id, text, StringComparison.Ordinal);
        }

        HashSet<string> referenced = ReferencedTestNames(text);
        HashSet<string> implemented = typeof(AcceptanceTests)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(IsAcceptanceCase)
            .Select(method => method.Name)
            .ToHashSet();

        // Кожне посилання в документі веде до наявного тесту.
        Assert.All(referenced, name => Assert.Contains(name, implemented));

        // Кожен приймальний тест описано в документі.
        Assert.All(implemented, name => Assert.Contains(name, referenced));

        // У кожного кейса є хоча б один автотест.
        for (int number = 1; number <= CaseCount; number++)
        {
            string prefix = string.Create(CultureInfo.InvariantCulture, $"Ac{number:00}");

            Assert.Contains(implemented, name => name.StartsWith(prefix, StringComparison.Ordinal));
        }
    }

    private static bool IsAcceptanceCase(MethodInfo method)
    {
        return IsAcceptanceName(method.Name) && method.IsDefined(typeof(FactAttribute), inherit: true);
    }

    private static bool IsAcceptanceName(string name)
    {
        return name.Length > 2
            && name.StartsWith("Ac", StringComparison.Ordinal)
            && char.IsDigit(name[2]);
    }

    private static HashSet<string> ReferencedTestNames(string text)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        int index = 0;

        while ((index = text.IndexOf(TestReferenceMarker, index, StringComparison.Ordinal)) >= 0)
        {
            int start = index + TestReferenceMarker.Length;
            int end = start;

            while (end < text.Length && char.IsLetterOrDigit(text[end]))
            {
                end++;
            }

            string name = text[start..end];

            if (IsAcceptanceName(name))
            {
                names.Add(name);
            }

            index = end;
        }

        return names;
    }
}