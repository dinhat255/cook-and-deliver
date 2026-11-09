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
        public Image innerBackgroundImage;

        private void Awake()
        {
            if (boundIngredient != null || isTrashButton)
            {
                Bind(boundIngredient, isTrashButton);
            }
        }

        private void OnValidate()
        {
            Color targetColor = isTrashButton
                ? new Color(0.78f, 0.16f, 0.16f)
                : (boundIngredient != null ? boundIngredient.slotBackgroundColor : new Color(0.24f, 0.2f, 0.18f));

            if (backgroundImage != null)
            {
                backgroundImage.color = targetColor;
            }

            var inner = innerBackgroundImage != null ? innerBackgroundImage : transform.Find("Bg/Bg (1)")?.GetComponent<Image>();
            if (inner != null)
            {
                inner.color = targetColor;
            }
        }

        public void Bind(IngredientSO ingredient, bool trash = false)
        {
            isTrashButton = trash;
            boundIngredient = ingredient;

            Color targetColor = isTrashButton
                ? new Color(0.78f, 0.16f, 0.16f)
                : (boundIngredient != null ? boundIngredient.slotBackgroundColor : new Color(0.24f, 0.2f, 0.18f));

            if (isTrashButton)
            {
                if (iconImage != null) iconImage.enabled = false;
                if (trashXText != null) trashXText.gameObject.SetActive(true);
                if (labelText != null) labelText.text = "Thùng rác";
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
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = targetColor;
            }

            var inner = innerBackgroundImage != null ? innerBackgroundImage : transform.Find("Bg/Bg (1)")?.GetComponent<Image>();
            if (inner != null)
            {
                inner.color = targetColor;
            }
        }
    }
}
