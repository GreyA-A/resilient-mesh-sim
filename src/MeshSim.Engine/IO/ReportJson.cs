using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeshSim.Engine.IO;

/// <summary>
/// Спільні налаштування JSON для експорту та імпорту звітів.
/// </summary>
internal static class ReportJson
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };
}