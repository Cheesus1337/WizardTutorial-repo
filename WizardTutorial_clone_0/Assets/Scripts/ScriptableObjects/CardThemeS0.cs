using System.Collections.Generic;
using UnityEngine;
using static CardEnums;

[CreateAssetMenu(fileName = "NewCardTheme", menuName = "Wizard/Card Theme")]
public class CardThemeSO : ScriptableObject
{
    [Header("Basis Grafiken")]
    public Sprite cardBack; // Rückseite
    public Sprite backgroundFrame; // Rahmen für die Vorderseite

    [Header("Bilder pro Wert")]
    // Wir nutzen eine Liste, um jedem Kartenwert (One, Two... Wizard) ein Bild zuzuweisen
    // Tipp: Die Reihenfolge im Editor muss mit dem Enum übereinstimmen, 
    // oder wir schreiben später eine sicherere Zuordnung. Für den Anfang reicht eine Liste.
    public List<Sprite> valueSprites;

    [Header("Farben")]
    public Color colorRed = Color.red;
    public Color colorBlue = Color.blue;
    public Color colorGreen = Color.green;
    public Color colorYellow = Color.yellow;

    public Color GetColor(CardColor c)
    {
        switch (c)
        {
            case CardColor.Red: return colorRed;
            case CardColor.Blue: return colorBlue;
            case CardColor.Green: return colorGreen;
            case CardColor.Yellow: return colorYellow;
            default: return Color.white;
        }
    }
}