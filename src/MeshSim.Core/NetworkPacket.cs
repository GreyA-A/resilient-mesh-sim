namespace MeshSim.Core;

/// <summary>
/// Представляє пакет даних, що передається між вузлами mesh-мережі.
/// </summary>
public sealed class NetworkPacket
{
    public string Id { get; }
    public string SourceNodeId { get; }
    public string DestinationNodeId { get; }
    public int Ttl { get; private set; }
    public int HopCount { get; private set; }
    public int PayloadBytes { get; }

    public NetworkPacket(string id, string sourceNodeId, string destinationNodeId, int ttl, int payloadBytes)
    {
        if(string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Ідентифікатор пакета не може бути порожнім.",  nameof(id));
        }

        if (string.IsNullOrWhiteSpace(sourceNodeId))
        {
            throw new ArgumentException("Ідентифікатор відправника не може бути порожнім.", nameof(sourceNodeId));
        }

        if (string.IsNullOrWhiteSpace(destinationNodeId))
        {
            throw new ArgumentException("Ідентифікатор отримувача не може бути порожнім.", nameof(destinationNodeId));
        }

        if (ttl <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "Початковий TTL має бути більшим за нуль.");
        }

        if (payloadBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(payloadBytes), "Розмір корисного навантаження має бути більшим за нуль");
        }

        Id = id;
        SourceNodeId = sourceNodeId;
        DestinationNodeId = destinationNodeId;
        Ttl = ttl;
        PayloadBytes = payloadBytes;
        HopCount = 0;
    }

    /// <summary>
    /// Фіксує передачу через проміжний вузол: зменшує TTL та збільшує кількість стрибків.
    /// </summary>
    /// <returns>True, якщо пакет все ще дійсний; False, якщо час життя вичерпано. </returns>
    public bool ForwardHop()
    {
        if(Ttl <= 0)
        {
            return false;
        }

        Ttl--;
        HopCount++;
        return Ttl > 0;
    }
}