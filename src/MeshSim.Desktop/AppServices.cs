using System.Diagnostics.CodeAnalysis;
using MeshSim.Desktop.Logging;

namespace MeshSim.Desktop;

[ExcludeFromCodeCoverage(Justification = "Композиційний корінь: лише складання залежностей застосунку.")]
public static class AppServices
{
    public static IAppLogger Logger { get; } = new FileLogger(FileLogger.GetDefaultDirectory());
}