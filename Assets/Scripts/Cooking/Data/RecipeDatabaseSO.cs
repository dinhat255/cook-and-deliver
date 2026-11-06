using System.Collections.Generic;
using UnityEngine;

namespace CookAndDeliver.Cooking.Data
{
    // Co so du lieu dong goi toan bo cong thuc va nguyen lieu
    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "Cooking/Clean/Recipe Database", order = 3)]
    public class RecipeDatabaseSO : ScriptableObject
    {
        public List<RecipeSO> recipes = new List<RecipeSO>();
        public List<IngredientSO> ingredients = new List<IngredientSO>();

        public RecipeSO GetRandomRecipe()
        {
            if (recipes == null || recipes.Count == 0) return null;
            int idx = Random.Range(0, recipes.Count);
            return recipes[idx];
        }

        public RecipeSO FindRecipeById(string id)
        {
            return recipes.Find(r => r != null && r.id == id);
        }

        public IngredientSO FindIngredientById(string id)
        {
            return ingredients.Find(i => i != null && i.id == id);
        }
    }
}
