using System;
using UnityEngine;

namespace CookAndDeliver.Cooking.Data
{
    // Defines an ingredient requirement with a specific quantity in a recipe
    [Serializable]
    public class IngredientRequirement
    {
        public IngredientSO ingredient;
        [Range(1, 5)] public int requiredAmount = 1;

        public IngredientRequirement() { }

        public IngredientRequirement(IngredientSO ingredient, int requiredAmount)
        {
            this.ingredient = ingredient;
            this.requiredAmount = requiredAmount;
        }
    }
}
