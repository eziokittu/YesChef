using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesChef
{
    public enum IngredientType
    {
        Vegetable,
        Cheese,
        Meat
    }

    public enum PreparationState
    {
        Raw,
        Prepared
    }

    public enum GamePhase
    {
        Instructions,
        Playing,
        Paused,
        ConfirmingQuit,
        Results
    }

    public static class IngredientRules
    {
        public static int Score(IngredientType type) => type switch
        {
            IngredientType.Vegetable => 20,
            IngredientType.Cheese => 10,
            IngredientType.Meat => 30,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

        public static bool IsDeliveryReady(IngredientType type, PreparationState state)
        {
            return type == IngredientType.Cheese || state == PreparationState.Prepared;
        }

        public static string ShortName(IngredientType type) => type switch
        {
            IngredientType.Vegetable => "VEG",
            IngredientType.Cheese => "CHEESE",
            IngredientType.Meat => "MEAT",
            _ => type.ToString().ToUpperInvariant()
        };

        public static Color Color(IngredientType type) => type switch
        {
            IngredientType.Vegetable => new Color(0.38f, 0.9f, 0.35f),
            IngredientType.Cheese => new Color(1f, 0.78f, 0.2f),
            IngredientType.Meat => new Color(1f, 0.32f, 0.32f),
            _ => UnityEngine.Color.white
        };
    }

    public static class OrderScoring
    {
        public static int Calculate(IEnumerable<IngredientType> ingredients, float secondsOpen)
        {
            return ingredients.Sum(IngredientRules.Score) - Mathf.FloorToInt(secondsOpen);
        }
    }
}
