using System.Collections.Generic;
using UnityEngine;

namespace CookAndDeliver.Cooking.Data
{
    // ScriptableObject defining a food recipe with multi-item ingredient quantities
    [CreateAssetMenu(fileName = "Recipe_", menuName = "Cooking/Clean/Recipe", order = 2)]
    public class RecipeSO : ScriptableObject
    {
        [Header("Recipe Information")]
        public string id;
        public string recipeName;
        public string region; // VN, KR, Fusion
        public int basePrice = 100;

        [Header("Finished Visual")]
        public Sprite finishedDishSprite;

        [Header("Ingredients with Required Quantities")]
        public List<IngredientRequirement> requirements = new List<IngredientRequirement>();

        public int GetRequiredAmount(IngredientSO ingredient)
        {
            if (ingredient == null || requirements == null) return 0;
            var req = requirements.Find(r => r.ingredient == ingredient || (r.ingredient != null && r.ingredient.id == ingredient.id));
            return req != null ? req.requiredAmount : 0;
        }

        public bool Requires(IngredientSO ingredient)
        {
            return GetRequiredAmount(ingredient) > 0;
        }

        public bool IsComplete(Dictionary<string, int> currentIngredients)
        {
            if (currentIngredients == null || requirements == null) return false;

            foreach (var req in requirements)
            {
                if (req.ingredient == null) continue;
                currentIngredients.TryGetValue(req.ingredient.id, out int currentCount);
                if (currentCount < req.requiredAmount) return false;
            }
            return true;
        }

        public bool CanAccept(IngredientSO ingredient, Dictionary<string, int> currentIngredients, Dictionary<string, int> pendingIngredients)
        {
            if (ingredient == null) return false;

            int required = GetRequiredAmount(ingredient);
            if (required <= 0) return false;

            currentIngredients.TryGetValue(ingredient.id, out int currentCount);
            pendingIngredients.TryGetValue(ingredient.id, out int pendingCount);

            // Rejects if current + pending already satisfies requirement
            if (currentCount + pendingCount >= required) return false;

            // Broth requires at least 1 noodle already in the bowl before ladling
            if (ingredient.id == "broth")
            {
                currentIngredients.TryGetValue("noodle", out int noodleCount);
                if (noodleCount <= 0) return false;
            }

            return true;
        }
    }
}
