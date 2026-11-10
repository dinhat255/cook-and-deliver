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
        public Image[] starIcons;

        [Header("Action Buttons")]
        public Button nextDayButton;
        public Button codexButton;
        public Button mainMenuButton;

        bool isInitialized;
        bool suppressHideOnAwake;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public Action OnNextDayClicked;
        public Action OnCodexClicked;
        public Action OnMainMenuClicked;

        void EnsureInitialized()
        {
            if (isInitialized) return;
            isInitialized = true;
            if (nextDayButton != null) nextDayButton.onClick.AddListener(() => OnNextDayClicked?.Invoke());
            if (codexButton != null) codexButton.onClick.AddListener(() => OnCodexClicked?.Invoke());
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
        }

        void Awake()
        {
            EnsureInitialized();
            if (!suppressHideOnAwake && panelRoot != null) panelRoot.SetActive(false);
        }

        public void Show(int day, int dineInOrders, int dineInIncome, int deliveryOrders, int deliveryGoal, int deliveryIncome, int penalty, int netIncome, int stars)
        {
            suppressHideOnAwake = true;
            EnsureInitialized();

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
                penaltySummaryText.color = penalty > 0 ? new Color(0.76f, 0.16f, 0.12f) : new Color(0.42f, 0.39f, 0.35f);
            }
            if (netEarningsText != null) netEarningsText.text = $"LÃI RÒNG: +{netIncome} XU";

            if (starIcons != null && starIcons.Length > 0)
            {
                Color earnedColor = new Color(0.91f, 0.55f, 0.06f, 1f);
                Color emptyColor = new Color(0.79f, 0.75f, 0.67f, 1f);
                for (int i = 0; i < starIcons.Length; i++)
                {
                    if (starIcons[i] != null)
                    {
                        starIcons[i].color = i < stars ? earnedColor : emptyColor;
                    }
                }
            }

            if (starRatingText != null)
            {
                string ratingLabel = stars >= 3 ? "XUẤT SẮC" : stars == 2 ? "TỐT" : "ĐẠT";
                starRatingText.text = (starIcons != null && starIcons.Length > 0) ? ratingLabel : $"{stars}/3 SAO · {ratingLabel}";
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
