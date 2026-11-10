using CookAndDeliver.Cooking.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // View representing an active order ticket on the kitchen order rail (KDS) using TextMeshPro
    public class TicketView : MonoBehaviour
    {
        [Header("UI Components")]
        public Image background;
        public Image activeFrame;
        public Image patienceProgressBar;
        public TextMeshProUGUI orderTypeText;
        public TextMeshProUGUI recipeTitleText;
        public TicketSlotView[] requirementSlots;
        public Button clickButton;

        [Header("Ticket Background Sprites")]
        public Sprite dineInSprite;
        public Sprite deliverySprite;

        [Header("Ticket Color Styling")]
        public Color dineInColor = new Color(0.97f, 0.96f, 0.92f);
        public Color deliveryColor = new Color(1f, 0.55f, 0.15f);
        public Color dineInTextColor = new Color(0.15f, 0.15f, 0.15f);
        public Color deliveryTextColor = new Color(0.12f, 0.08f, 0.02f);

        public OrderModel BoundOrder { get; private set; }
        public System.Action<TicketView> OnTicketClicked;

        void Awake()
        {
            if (clickButton != null)
            {
                clickButton.onClick.AddListener(() => OnTicketClicked?.Invoke(this));
            }
        }

        public void BindOrder(OrderModel order)
        {
            BoundOrder = order;
            SetAssignedToWorkstation(false);
            if (order == null || order.Recipe == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            bool isShip = order.IsShipOrder;

            if (background != null)
            {
                Sprite targetSprite = isShip ? deliverySprite : dineInSprite;
                if (targetSprite != null)
                {
                    background.sprite = targetSprite;
                    background.color = Color.white;
                }
                else
                {
                    background.color = isShip ? deliveryColor : dineInColor;
                }
            }

            Color targetTextColor = isShip ? deliveryTextColor : dineInTextColor;

            if (orderTypeText != null)
            {
                orderTypeText.text = isShip ? "GIAO SHIP" : "TẠI BÀN";
                orderTypeText.color = targetTextColor;
            }

            if (recipeTitleText != null)
            {
                recipeTitleText.text = order.Recipe.recipeName;
                recipeTitleText.color = targetTextColor;
            }

            var reqs = order.Recipe.requirements;
            if (requirementSlots != null)
            {
                for (int i = 0; i < requirementSlots.Length; i++)
                {
                    if (requirementSlots[i] == null) continue;

                    if (i < reqs.Count && reqs[i] != null)
                    {
                        requirementSlots[i].Bind(reqs[i].ingredient, reqs[i].requiredAmount);
                    }
                    else
                    {
                        requirementSlots[i].Bind(null, 0);
                    }
                }
            }
        }

        public void UpdatePatience(float ratio)
        {
            if (patienceProgressBar == null) return;
            patienceProgressBar.fillAmount = ratio;
            patienceProgressBar.color = ratio > 0.5f ? new Color(0.25f, 0.8f, 0.3f)
                : ratio > 0.25f ? new Color(1f, 0.8f, 0.2f)
                : new Color(0.9f, 0.2f, 0.2f);
        }

        public void SetAssignedToWorkstation(bool isAssigned)
        {
            if (activeFrame == null) return;
            activeFrame.color = new Color(1f, 0.85f, 0.2f, isAssigned ? 0.95f : 0f);
        }
    }
}
