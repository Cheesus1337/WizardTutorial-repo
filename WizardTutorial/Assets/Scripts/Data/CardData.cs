using System;
using UnityEngine;
using static CardEnums;

[Serializable]
public struct CardData
{
    public CardColor color;
    public CardValue value;

    public CardData(CardColor color, CardValue value)
    {
        this.color = color;
        this.value = value;
    }

    // Eine kleine Hilfsfunktion für Debugging-Ausgaben
    public override string ToString()
    {
        return $"{color} {value}";
    }
}