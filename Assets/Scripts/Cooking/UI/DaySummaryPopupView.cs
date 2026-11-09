using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // View controlling the End-of-Day summary receipt popup with earnings, rating, and day transition controls
    public class DaySummaryPopupView : MonoBehaviour
    {
        [Header("Root & Panels")]
        public GameObject panelRoot;
        public RectTransform cardContainer;
        public Image backdropImage;

        [Header("Summary Information")]
        public TextMeshProUGUI dayTitleText;
        public TextMeshProUGUI dineInSummaryText;
        public TextMeshProUGUI deliverySummaryText;
        public TextMeshProUGUI penaltySummaryText;
        public TextMeshProUGUI netEarningsText;
        public TextMeshProUGUI starRatingText;

        [Header("Action Buttons")]
        public Button nextDayButton;
        public Button codexButton;
        public Button mainMenuButton;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public Action OnNextDayClicked;
        public Action OnCodexClicked;
        public Action OnMainMenuClicked;

        void Awake()
        {
            if (nextDayButton != null) nextDayButton.onClick.AddListener(() => OnNextDayClicked?.Invoke());
            if (codexButton != null) codexButton.onClick.AddListener(() => OnCodexClicked?.Invoke());
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
            if (panelRoot != null) panelRoot.SetActive(false);
        }

        public void Show(int day, int dineInOrders, int dineInIncome, int deliveryOrders, int deliveryGoal, int deliveryIncome, int penalty, int netIncome, int stars)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                if (cardContainer != null)
                {
                    cardContainer.DOKill();
                    cardContainer.localScale = Vector3.one * 0.9f;
                    cardContainer.DOScale(1f, 0.25f).SetEase(Ease.OutBack);
                }
            }

            if (dayTitleText != null) dayTitleText.text = $"HẾT CA · NGÀY {day}";
            if (dineInSummaryText != null) dineInSummaryText.text = $"Khách tại bàn: {dineInOrders} đơn  (+{dineInIncome} xu)";
            if (deliverySummaryText != null) deliverySummaryText.text = $"Giao hàng: {deliveryOrders}/{deliveryGoal} đơn  (+{deliveryIncome} xu)";
            if (penaltySummaryText != null)
            {
                penaltySummaryText.text = penalty > 0 ? $"Phạt bồi thường: -{penalty} xu" : "Không vi phạm: 0 xu";
                penaltySummaryText.color = penalty > 0 ? new Color(1f, 0.4f, 0.4f) : new Color(0.7f, 0.7f, 0.7f);
            }
            if (netEarningsText != null) netEarningsText.text = $"LÃI RÒNG: +{netIncome} XU";

            if (starRatingText != null)
            {
                string starStr = stars >= 3 ? "[ ★ ★ ★ ] XUẤT SẮC" : stars == 2 ? "[ ★ ★ ☆ ] TỐT" : "[ ★ ☆ ☆ ] ĐẠT";
                starRatingText.text = starStr;
            }
        }

        public void Close()
        {
            if (cardContainer != null)
            {
                cardContainer.DOKill();
                cardContainer.DOScale(0.9f, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    if (panelRoot != null) panelRoot.SetActive(false);
                });
            }
            else
            {
                if (panelRoot != null) panelRoot.SetActive(false);
            }
        }
    }
}
