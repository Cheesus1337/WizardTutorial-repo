using Unity.Netcode;
using Unity.Collections; // Wichtig für FixedString
using System;

// Dieses Struct speichert alles, was im "Block der Wahrheit" steht
public struct WizardPlayerData : INetworkSerializable, IEquatable<WizardPlayerData>
{
    public ulong clientId;
    public FixedString64Bytes playerName; // Strings müssen im Netzwerk "Fixed" sein
    public int score;
    public int currentBid;   // Angesagte Stiche
    public int tricksTaken;  // Aktuell gemachte Stiche
    public bool hasBidded;   // Hat der Spieler schon "Ok" gedrückt?

    // Pflicht-Methode für Netzwerk-Übertragung
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref playerName);
        serializer.SerializeValue(ref score);
        serializer.SerializeValue(ref currentBid);
        serializer.SerializeValue(ref tricksTaken);
        serializer.SerializeValue(ref hasBidded);
    }

    // Vergleichs-Methode (für Updates)
    public bool Equals(WizardPlayerData other)
    {
        return clientId == other.clientId &&
               playerName == other.playerName &&
               score == other.score &&
               currentBid == other.currentBid &&
               tricksTaken == other.tricksTaken &&
               hasBidded == other.hasBidded;
    }
}