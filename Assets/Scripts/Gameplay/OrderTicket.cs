using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Plain C# runtime data for one customer's order.
    /// Keeping this separate from CustomerWindow makes the rules easy to test
    /// without needing a scene or a GameObject.
    /// </summary>
    public sealed class OrderTicket
    {
        private readonly List<IngredientType> requiredIngredients;
        private readonly List<IngredientType> remainingIngredients;

        public IReadOnlyList<IngredientType> RequiredIngredients => requiredIngredients;
        public IReadOnlyList<IngredientType> RemainingIngredients => remainingIngredients;
        public float SecondsOpen { get; private set; }
        public bool IsComplete => remainingIngredients.Count == 0;

        public OrderTicket(IEnumerable<IngredientType> ingredients)
        {
            requiredIngredients = ingredients.ToList();
            if (requiredIngredients.Count is < 2 or > 3)
            {
                throw new ArgumentException("An order must contain exactly two or three ingredients.", nameof(ingredients));
            }

            remainingIngredients = new List<IngredientType>(requiredIngredients);
        }

        public void Tick(float deltaTime)
        {
            SecondsOpen += Mathf.Max(0f, deltaTime);
        }

        /// <summary>
        /// Removes one matching occurrence. This is why duplicate ingredients,
        /// such as Meat + Meat + Cheese, work correctly.
        /// </summary>
        public bool TryDeliver(IngredientType ingredient)
        {
            var matchingIndex = remainingIngredients.IndexOf(ingredient);
            if (matchingIndex < 0)
            {
                return false;
            }

            remainingIngredients.RemoveAt(matchingIndex);
            return true;
        }

        public int CalculateScore()
        {
            return OrderScoring.Calculate(requiredIngredients, SecondsOpen);
        }
    }

    public static class OrderGenerator
    {
        private static readonly IngredientType[] IngredientTypes =
        {
            IngredientType.Vegetable,
            IngredientType.Cheese,
            IngredientType.Meat
        };

        /// <summary>
        /// First chooses two or three ingredients with an exact 50/50 chance,
        /// then chooses every slot independently so duplicates remain possible.
        /// </summary>
        public static OrderTicket CreateRandom()
        {
            var ingredientCount = UnityEngine.Random.value < 0.5f ? 2 : 3;
            var ingredients = new IngredientType[ingredientCount];
            for (var index = 0; index < ingredients.Length; index++)
            {
                ingredients[index] = IngredientTypes[UnityEngine.Random.Range(0, IngredientTypes.Length)];
            }

            return new OrderTicket(ingredients);
        }

        /// <summary>
        /// Returns all 3^2 + 3^3 = 36 ordered recipes. Used by the editor
        /// validator to prove every variation, including duplicates, is supported.
        /// </summary>
        public static IReadOnlyList<IngredientType[]> CreateAllRecipeVariations()
        {
            var recipes = new List<IngredientType[]>();
            AddVariationsOfLength(2, new IngredientType[2], 0, recipes);
            AddVariationsOfLength(3, new IngredientType[3], 0, recipes);
            return recipes;
        }

        private static void AddVariationsOfLength(
            int length,
            IngredientType[] recipe,
            int index,
            ICollection<IngredientType[]> recipes)
        {
            if (index == length)
            {
                recipes.Add((IngredientType[])recipe.Clone());
                return;
            }

            foreach (var ingredient in IngredientTypes)
            {
                recipe[index] = ingredient;
                AddVariationsOfLength(length, recipe, index + 1, recipes);
            }
        }
    }
}
