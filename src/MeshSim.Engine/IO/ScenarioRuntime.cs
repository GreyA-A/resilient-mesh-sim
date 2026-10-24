using MeshSim.Core;

namespace MeshSim.Engine.IO;

/// <summary>
/// Готове до запуску середовище, побудоване зі сценарію: топологія та рушій із запланованими пакетами.
/// </summary>
public sealed class ScenarioRuntime
{
    internal ScenarioRuntime(NetworkTopology topology, SimulationEngine simulation)
    {
        Topology = topology;
        Simulation = simulation;
    }

    public NetworkTopology Topology { get; }
    public SimulationEngine Simulation { get; }
}