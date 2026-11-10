using CookAndDeliver.Cooking.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // View representing an ingredient badge on an order ticket (e.g. icon + "x3")
    public class TicketSlotView : MonoBehaviour
    {
        [Header("UI Components")]
        public Image iconImage;
        public TextMeshProUGUI quantityBadgeText;
        public GameObject badgeRoot;

        public void Bind(IngredientSO ingredient, int amount)
        {
            if (ingredient == null || amount <= 0)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            if (iconImage != null)
            {
                iconImage.sprite = ingredient.rawSprite;
                iconImage.color = ingredient.GetDisplayColor(false);
                iconImage.enabled = true;
            }

            bool showBadge = amount > 1;
            if (badgeRoot != null)
            {
                badgeRoot.SetActive(showBadge);
            }
            if (quantityBadgeText != null)
            {
                quantityBadgeText.gameObject.SetActive(showBadge);
                quantityBadgeText.text = showBadge ? $"x{amount}" : "";
            }
        }
    }
}
