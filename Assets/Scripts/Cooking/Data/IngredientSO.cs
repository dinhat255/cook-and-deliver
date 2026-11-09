using UnityEngine;

namespace CookAndDeliver.Cooking.Data
{
    // ScriptableObject defining an individual ingredient and its heat cooking parameters
    [CreateAssetMenu(fileName = "Ingredient_", menuName = "Cooking/Clean/Ingredient", order = 1)]
    public class IngredientSO : ScriptableObject
    {
        [Header("Basic Information")]
        public string id;
        public string displayName;
        public CookingRoute route = CookingRoute.Direct;

        [Header("Visuals")]
        public Sprite rawSprite;
        public Sprite cookedSprite;
        public Color cookedTint = Color.white;
        public Color slotBackgroundColor = new Color(0.24f, 0.2f, 0.18f);

        [Header("Heat Processing Timers (Seconds)")]
        public float cookDuration = 3f;
        public float warnDuration = 6f;
        public float burnDuration = 8f;
        public bool canBurn = true;

        public Sprite GetDisplaySprite(bool isCooked)
        {
            if (isCooked && cookedSprite != null) return cookedSprite;
            return rawSprite;
        }

        public Color GetDisplayColor(bool isCooked)
        {
            if (isCooked && cookedSprite == null) return cookedTint;
            return Color.white;
        }
    }
}
