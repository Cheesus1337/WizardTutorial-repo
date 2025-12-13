using System.Collections.Generic;
using UnityEngine;

// Vergleicht zwei Karten miteinander, um eine Sortierreihenfolge festzulegen
public class CardSorter : IComparer<CardData>
{
    public int Compare(CardData x, CardData y)
    {
        // 1. Zuerst nach Farben sortieren (Blau, Rot, Gelb, Grün, Zauberer, Narr - je nach Enum-Reihenfolge)
        if (x.color != y.color)
        {
            return x.color.CompareTo(y.color);
        }

        // 2. Wenn die Farbe gleich ist, nach Wert sortieren (1 bis 13)
        return x.value.CompareTo(y.value);
    }
}