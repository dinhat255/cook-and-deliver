using CookAndDeliver.Cooking.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // View representing a single ingredient slot on the workstation board with quantity progress (e.g. 1/3)
    public class ChecklistSlotView : MonoBehaviour
    {
        [Header("UI Components")]
        public Image iconImage;
        public TextMeshProUGUI amountText;

        [Header("Color States")]
        public Color inProgressColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        public Color completedColor = new Color(0.3f, 0.9f, 0.35f, 1f);
        public Color dimmedIconTint = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        public IngredientSO BoundIngredient { get; private set; }
        public int RequiredCount { get; private set; }

        public void Bind(IngredientSO ingredient, int requiredCount)
        {
            BoundIngredient = ingredient;
            RequiredCount = requiredCount;

            if (ingredient != null)
            {
                gameObject.SetActive(true);
                if (iconImage != null)
                {
                    iconImage.sprite = ingredient.rawSprite;
                    iconImage.enabled = true;
                }
                UpdateProgress(0);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void UpdateProgress(int currentCount)
        {
            bool isComplete = currentCount >= RequiredCount;

            if (amountText != null)
            {
                amountText.text = $"{currentCount}/{RequiredCount}";
                amountText.color = isComplete ? completedColor : inProgressColor;
            }

            if (iconImage != null)
            {
                iconImage.color = currentCount > 0 ? Color.white : dimmedIconTint;
            }
        }
    }
}
