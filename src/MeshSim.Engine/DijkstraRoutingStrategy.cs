using MeshSim.Core;

namespace MeshSim.Engine;

/// <summary>
/// Реалізує пошук найнадійнішого маршруту за алгоритмом Дейкстри з обходом зон радіозавад.
/// </summary>
public sealed class DijkstraRoutingStrategy : IRoutingStrategy
{
    public IReadOnlyList<string> FindRoute(NetworkTopology topology, string sourceNodeId, string destinationNodeId)
    {
        ArgumentNullException.ThrowIfNull(topology);

        if (string.IsNullOrWhiteSpace(sourceNodeId))
        {
            throw new ArgumentException("Ідентифікатор вузла-відправника не може бути порожнім.", nameof(sourceNodeId));
        }

        if (string.IsNullOrWhiteSpace(destinationNodeId))
        {
            throw new ArgumentException("Ідентифікатор вузла-отримувача не може бути порожнім.", nameof(destinationNodeId));
        }

        if (!topology.Nodes.TryGetValue(sourceNodeId, out MeshNode? sourceNode) ||
            !topology.Nodes.TryGetValue(destinationNodeId, out MeshNode? destinationNode))
        {
            return [];
        }

        if (!sourceNode.IsActive || !destinationNode.IsActive)
        {
            return [];
        }

        if (string.Equals(sourceNodeId, destinationNodeId, StringComparison.Ordinal))
        {
            return [sourceNodeId];
        }

        var distances = new Dictionary<string, double>(StringComparer.Ordinal);
        var previous = new Dictionary<string, string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var queue = new PriorityQueue<string, double>();

        foreach (string nodeId in topology.Nodes.Keys)
        {
            distances[nodeId] = double.PositiveInfinity;
        }

        distances[sourceNodeId] = 0.0;
        queue.Enqueue(sourceNodeId, 0.0);

        while (queue.Count > 0)
        {
            string currentId = queue.Dequeue();

            if (!visited.Add(currentId))
            {
                continue;
            }

            if (string.Equals(currentId, destinationNodeId, StringComparison.Ordinal))
            {
                break;
            }

            MeshNode currentNode = topology.Nodes[currentId];

            foreach (MeshNode neighbor in topology.Nodes.Values)
            {
                if (string.Equals(currentId, neighbor.Id, StringComparison.Ordinal) || visited.Contains(neighbor.Id))
                {
                    continue;
                }

                double quality = topology.CalculateLinkQuality(currentNode, neighbor);
                if (quality <= 0.0)
                {
                    continue;
                }

                // Вага ребра: геометрична відстань, поділена на квадрат якості каналу (штраф за наближення до РЕБ)
                double distance = currentNode.Position.DistanceTo(neighbor.Position);
                double edgeCost = distance / (quality * quality);
                double newDistance = distances[currentId] + edgeCost;

                if (newDistance < distances[neighbor.Id])
                {
                    distances[neighbor.Id] = newDistance;
                    previous[neighbor.Id] = currentId;
                    queue.Enqueue(neighbor.Id, newDistance);
                }
            }
        }

        if (!previous.ContainsKey(destinationNodeId))
        {
            return [];
        }

        var path = new List<string>();
        string? step = destinationNodeId;

        while (step is not null)
        {
            path.Add(step);
            previous.TryGetValue(step, out step);
        }

        path.Reverse();
        return path;
    }
}