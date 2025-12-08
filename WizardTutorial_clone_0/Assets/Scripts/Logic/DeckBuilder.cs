using System;
using System.Collections.Generic;
using static CardEnums;

public static class DeckBuilder
{
    public static List<CardData> GenerateStandardDeck()
    {
        List<CardData> newDeck = new List<CardData>();

        // Wir iterieren durch alle 4 Farben
        foreach (CardColor color in Enum.GetValues(typeof(CardColor)))
        {
            // Für jede Farbe fügen wir die Werte hinzu
            foreach (CardValue value in Enum.GetValues(typeof(CardValue)))
            {
                newDeck.Add(new CardData(color, value));
            }
        }

        return newDeck;
    }

    // Die berühmte Fisher-Yates Misch-Methode für faire Zufälle
    public static void ShuffleDeck(List<CardData> deck)
    {
        System.Random rng = new System.Random();
        int n = deck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            CardData value = deck[k];
            deck[k] = deck[n];
            deck[n] = value;
        }
    }
}