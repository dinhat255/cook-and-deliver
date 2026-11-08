using System;
using System.Collections.Generic;
using CookAndDeliver.Cooking.Data;

namespace CookAndDeliver.Cooking.Core.Services
{
    // Interface managing recipe unlock progression, persistence, and academy learning
    public interface IRecipeProgressionService : IService
    {
        event Action<RecipeSO> OnRecipeUnlocked;

        bool IsRecipeUnlocked(RecipeSO recipe);
        bool IsRecipeUnlocked(string recipeId);
        bool CanUnlockRecipe(RecipeSO recipe, int currentCoins);
        bool TryUnlockRecipe(RecipeSO recipe, ref int currentCoins);
        List<RecipeSO> GetUnlockedRecipes(List<RecipeSO> allRecipes);
        void ResetProgression();
    }
}
