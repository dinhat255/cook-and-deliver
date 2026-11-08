using System;
using System.Collections.Generic;
using CookAndDeliver.Cooking.Data;
using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Services
{
    // Implementation of IRecipeProgressionService managing persistent recipe mastery and learning
    public class RecipeProgressionService : MonoBehaviour, IRecipeProgressionService
    {
        const string SaveKeyPrefix = "RECIPE_UNLOCKED_";

        public event Action<RecipeSO> OnRecipeUnlocked;

        void Awake()
        {
            CookingServiceLocator.Register<IRecipeProgressionService>(this);
        }

        void OnDestroy()
        {
            CookingServiceLocator.Unregister<IRecipeProgressionService>();
        }

        public bool IsRecipeUnlocked(RecipeSO recipe)
        {
            if (recipe == null) return false;
            return IsRecipeUnlocked(recipe.id, recipe.isUnlockedByDefault);
        }

        public bool IsRecipeUnlocked(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return false;
            return PlayerPrefs.GetInt(SaveKeyPrefix + recipeId, 0) == 1;
        }

        bool IsRecipeUnlocked(string recipeId, bool defaultUnlocked)
        {
            if (string.IsNullOrEmpty(recipeId)) return false;
            string key = SaveKeyPrefix + recipeId;
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetInt(key) == 1;
            }
            return defaultUnlocked;
        }

        public bool CanUnlockRecipe(RecipeSO recipe, int currentCoins)
        {
            if (recipe == null) return false;
            if (IsRecipeUnlocked(recipe)) return false;
            return currentCoins >= recipe.unlockCostCoins;
        }

        public bool TryUnlockRecipe(RecipeSO recipe, ref int currentCoins)
        {
            if (recipe == null) return false;
            if (!CanUnlockRecipe(recipe, currentCoins)) return false;

            currentCoins -= recipe.unlockCostCoins;
            PlayerPrefs.SetInt(SaveKeyPrefix + recipe.id, 1);
            PlayerPrefs.Save();

            OnRecipeUnlocked?.Invoke(recipe);
            return true;
        }

        public List<RecipeSO> GetUnlockedRecipes(List<RecipeSO> allRecipes)
        {
            var result = new List<RecipeSO>();
            if (allRecipes == null) return result;

            foreach (var recipe in allRecipes)
            {
                if (recipe != null && IsRecipeUnlocked(recipe))
                {
                    result.Add(recipe);
                }
            }

            // Fallback safety guard: If nothing is unlocked, return default unlocked recipes
            if (result.Count == 0)
            {
                foreach (var recipe in allRecipes)
                {
                    if (recipe != null && recipe.isUnlockedByDefault)
                    {
                        result.Add(recipe);
                    }
                }
            }

            // Secondary fallback: If still empty, return all recipes
            if (result.Count == 0)
            {
                result.AddRange(allRecipes);
            }

            return result;
        }

        public void ResetProgression()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
