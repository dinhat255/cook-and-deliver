using System;
using System.Collections.Generic;
using CookAndDeliver.Cooking.Data;
using CookAndDeliver.SaveLoad.Core;
using CookAndDeliver.SaveLoad.Objects;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Services
{
    // Implementation of IRecipeProgressionService managing persistent recipe mastery and learning
    public class RecipeProgressionService : MonoBehaviour, IRecipeProgressionService
    {
        public const string GameProgressSaveKey = "GameProgress";
        private const string LegacyPlayerPrefsPrefix = "RECIPE_UNLOCKED_";

        public event Action<RecipeSO> OnRecipeUnlocked;

        private GameProgressSLO _progress;

        void Awake()
        {
            CookingServiceLocator.Register<IRecipeProgressionService>(this);
            LoadProgressAsync().Forget();
        }

        void OnDestroy()
        {
            CookingServiceLocator.Unregister<IRecipeProgressionService>();
        }

        private async UniTaskVoid LoadProgressAsync()
        {
            try
            {
                _progress = await SaveLoadManager.LoadAsync<GameProgressSLO>(GameProgressSaveKey);
                if (_progress == null)
                {
                    _progress = new GameProgressSLO();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[RecipeProgressionService] Failed to load GameProgress: {ex.Message}");
                _progress = new GameProgressSLO();
            }
        }

        private GameProgressSLO GetOrEnsureProgress()
        {
            if (_progress == null)
            {
                _progress = new GameProgressSLO();
            }
            return _progress;
        }

        public bool IsRecipeUnlocked(RecipeSO recipe)
        {
            if (recipe == null) return false;
            return IsRecipeUnlocked(recipe.id, recipe.isUnlockedByDefault);
        }

        public bool IsRecipeUnlocked(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId)) return false;

            var progress = GetOrEnsureProgress();
            if (progress.IsRecipeUnlocked(recipeId)) return true;

            // Backward compatibility fallback with legacy PlayerPrefs
            return PlayerPrefs.GetInt(LegacyPlayerPrefsPrefix + recipeId, 0) == 1;
        }

        bool IsRecipeUnlocked(string recipeId, bool defaultUnlocked)
        {
            if (string.IsNullOrEmpty(recipeId)) return false;

            var progress = GetOrEnsureProgress();
            if (progress.IsRecipeUnlocked(recipeId)) return true;

            string key = LegacyPlayerPrefsPrefix + recipeId;
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

            var progress = GetOrEnsureProgress();
            progress.UnlockRecipe(recipe.id);

            // Save via SaveLoadManager
            SaveLoadManager.SaveAsync(GameProgressSaveKey, progress).Forget();

            // Maintain legacy PlayerPrefs sync for compatibility
            PlayerPrefs.SetInt(LegacyPlayerPrefsPrefix + recipe.id, 1);
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
            _progress = new GameProgressSLO();
            SaveLoadManager.SaveAsync(GameProgressSaveKey, _progress).Forget();

            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
