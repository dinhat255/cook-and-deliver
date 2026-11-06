using CookAndDeliver.Cooking.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // View representing a button in the bottom ingredient thumb tray using TextMeshPro
    public class TrayButtonView : MonoBehaviour
    {
        [Header("Ingredient Configuration")]
        public IngredientSO boundIngredient;
        public bool isTrashButton;

        [Header("UI Components")]
        public Image iconImage;
        public TextMeshProUGUI labelText;
        public TextMeshProUGUI trashXText;
        public Button clickButton;
        public Image backgroundImage;

        public void Bind(IngredientSO ingredient, bool trash = false)
        {
            isTrashButton = trash;
            boundIngredient = ingredient;

            if (isTrashButton)
            {
                if (iconImage != null) iconImage.enabled = false;
                if (trashXText != null) trashXText.gameObject.SetActive(true);
                if (labelText != null) labelText.text = "Thùng rác";
                if (backgroundImage != null) backgroundImage.color = new Color(0.65f, 0.12f, 0.12f);
            }
            else if (ingredient != null)
            {
                if (trashXText != null) trashXText.gameObject.SetActive(false);
                if (iconImage != null)
                {
                    iconImage.enabled = true;
                    iconImage.sprite = ingredient.rawSprite;
                }
                if (labelText != null) labelText.text = ingredient.displayName;
                if (backgroundImage != null)
                {
                    backgroundImage.color = ingredient.route == CookingRoute.Direct
                        ? new Color(0.24f, 0.2f, 0.18f)
                        : new Color(0.45f, 0.2f, 0.12f);
                }
            }
        }
    }
}
