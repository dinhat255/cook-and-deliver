using System;
using System.Collections.Generic;
using CookAndDeliver.Cooking.Core.Events;
using CookAndDeliver.Cooking.Core.Services;
using CookAndDeliver.Cooking.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // View controlling the Culinary Codex and Cooking Academy popup for exploring food lore and unlocking recipes
    public class CulinaryCodexPopupView : MonoBehaviour
    {
        [Header("Root & Controls")]
        public GameObject panelRoot;
        public Button closeButton;
        public Button backdropButton;

        [Header("Launcher")]
        public Button openCodexButton;

        [Header("Database & Economy")]
        public RecipeDatabaseSO recipeDatabase;
        public EconomyEventChannelSO economyEventChannel;

        [Header("List View")]
        public Transform recipeListContainer;
        public GameObject recipeCardPrefab;

        [Header("Detail View - Headers")]
        public TextMeshProUGUI dishNameText;
        public TextMeshProUGUI regionBadgeText;
        public TextMeshProUGUI priceText;
        public Image dishImage;

        [Header("Detail View - Cultural Content")]
        public TextMeshProUGUI descriptionText;
        public TextMeshProUGUI culturalStoryText;
        public TextMeshProUGUI flavorProfileText;
        public TextMeshProUGUI chefTipText;

        [Header("Detail View - Action & State")]
        public GameObject unlockSection;
        public Button unlockButton;
        public TextMeshProUGUI unlockCostText;
        public GameObject masteredBadge;

        RecipeSO currentSelectedRecipe;
        readonly List<GameObject> spawnedCards = new List<GameObject>();

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;
        public Action<bool> OnOpenStateChanged;

        void Awake()
        {
            if (panelRoot != null) panelRoot.SetActive(false);

            if (closeButton != null) closeButton.onClick.AddListener(Close);
            if (backdropButton != null) backdropButton.onClick.AddListener(Close);
            if (openCodexButton != null) openCodexButton.onClick.AddListener(Open);
            if (unlockButton != null) unlockButton.onClick.AddListener(HandleUnlockClicked);
        }

        public void Open()
        {
            if (backdropButton != null)
            {
                backdropButton.gameObject.SetActive(true);
            }

            if (panelRoot != null)
            {
                panelRoot.SetActive(true);
                var rt = panelRoot.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.DOKill();
                    rt.localScale = Vector3.one * 0.92f;
                    rt.DOScale(1f, 0.22f).SetEase(Ease.OutBack);
                }
            }

            OnOpenStateChanged?.Invoke(true);
            PopulateList();
        }

        public void Close()
        {
            if (backdropButton != null)
            {
                backdropButton.gameObject.SetActive(false);
            }

            if (panelRoot != null && panelRoot.activeSelf)
            {
                var rt = panelRoot.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.DOKill();
                    rt.DOScale(0.92f, 0.15f).SetEase(Ease.InBack).OnComplete(() =>
                    {
                        panelRoot.SetActive(false);
                    });
                }
                else
                {
                    panelRoot.SetActive(false);
                }
            }

            OnOpenStateChanged?.Invoke(false);
        }

        public void PopulateList()
        {
            ClearList();
            if (recipeDatabase == null || recipeDatabase.recipes == null) return;

            var progression = CookingServiceLocator.Get<IRecipeProgressionService>();

            RecipeSO firstRecipe = null;
            foreach (var recipe in recipeDatabase.recipes)
            {
                if (recipe == null) continue;
                if (firstRecipe == null) firstRecipe = recipe;

                bool isUnlocked = progression != null ? progression.IsRecipeUnlocked(recipe) : recipe.isUnlockedByDefault;
                CreateCard(recipe, isUnlocked);
            }

            if (currentSelectedRecipe == null && firstRecipe != null)
            {
                SelectRecipe(firstRecipe);
            }
            else if (currentSelectedRecipe != null)
            {
                SelectRecipe(currentSelectedRecipe);
            }
        }

        void ClearList()
        {
            foreach (var card in spawnedCards)
            {
                if (card != null) Destroy(card);
            }
            spawnedCards.Clear();
        }

        void CreateCard(RecipeSO recipe, bool isUnlocked)
        {
            if (recipeListContainer == null) return;

            GameObject cardObj = null;
            if (recipeCardPrefab != null)
            {
                cardObj = Instantiate(recipeCardPrefab, recipeListContainer);
            }
            else
            {
                // Fallback runtime button if prefab is not assigned
                cardObj = new GameObject($"Card_{recipe.id}", typeof(RectTransform), typeof(Button), typeof(Image));
                cardObj.transform.SetParent(recipeListContainer, false);
            }

            cardObj.SetActive(true);
            spawnedCards.Add(cardObj);

            var btn = cardObj.GetComponent<Button>();
            if (btn == null) btn = cardObj.gameObject.AddComponent<Button>();

            var imgBg = cardObj.GetComponent<Image>();
            if (imgBg != null)
            {
                imgBg.color = (recipe == currentSelectedRecipe)
                    ? new Color(0.50f, 0.76f, 1.0f, 1f)
                    : (isUnlocked ? new Color(0.36f, 0.40f, 0.48f, 1f) : new Color(0.22f, 0.24f, 0.28f, 0.90f));
            }

            // Configure text components on card
            var texts = cardObj.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var txt in texts)
            {
                if (txt.name.Contains("Name") || txt.name == "Text" || txt.name == "ItemText")
                {
                    txt.text = isUnlocked
                        ? $"<b>{recipe.recipeName}</b>  <color=#94A3B8>[{recipe.region}]</color>  <color=#73E678>{recipe.basePrice} xu</color>"
                        : $"<b>{recipe.recipeName}</b>  <color=#FFA847>[Chưa học - {recipe.unlockCostCoins} xu]</color>";
                }
                else if (txt.name.Contains("Status"))
                {
                    txt.text = isUnlocked ? "Đã mở" : $"{recipe.unlockCostCoins} xu";
                }
            }

            var images = cardObj.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.name.Contains("Icon") && recipe.finishedDishSprite != null)
                {
                    img.sprite = recipe.finishedDishSprite;
                    img.color = isUnlocked ? Color.white : new Color(0.2f, 0.2f, 0.2f, 0.8f);
                }
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                SelectRecipe(recipe);
                RefreshCardHighlights();
            });
        }

        void RefreshCardHighlights()
        {
            if (recipeDatabase == null || recipeDatabase.recipes == null) return;
            var progression = CookingServiceLocator.Get<IRecipeProgressionService>();

            for (int i = 0; i < spawnedCards.Count && i < recipeDatabase.recipes.Count; i++)
            {
                var card = spawnedCards[i];
                var recipe = recipeDatabase.recipes[i];
                if (card == null || recipe == null) continue;

                var imgBg = card.GetComponent<Image>();
                if (imgBg != null)
                {
                    bool isSel = (recipe == currentSelectedRecipe);
                    bool isUnl = progression != null ? progression.IsRecipeUnlocked(recipe) : recipe.isUnlockedByDefault;
                    imgBg.color = isSel
                        ? new Color(0.50f, 0.76f, 1.0f, 1f)
                        : (isUnl ? new Color(0.36f, 0.40f, 0.48f, 1f) : new Color(0.22f, 0.24f, 0.28f, 0.90f));
                }
            }
        }

        public void SelectRecipe(RecipeSO recipe)
        {
            if (recipe == null) return;
            currentSelectedRecipe = recipe;
            RefreshCardHighlights();

            var progression = CookingServiceLocator.Get<IRecipeProgressionService>();
            bool isUnlocked = progression != null ? progression.IsRecipeUnlocked(recipe) : recipe.isUnlockedByDefault;

            if (dishNameText != null)
            {
                dishNameText.text = isUnlocked ? $"<b>{recipe.recipeName}</b>" : $"<b>{recipe.recipeName}</b> <color=#FFA847>(Chưa mở)</color>";
            }

            if (regionBadgeText != null)
            {
                regionBadgeText.text = $"Văn hóa: <color=#FFD700>{recipe.region}</color>";
            }

            if (priceText != null)
            {
                priceText.text = $"Thưởng: <color=#73E678>{recipe.basePrice} xu</color>";
            }

            if (dishImage != null)
            {
                dishImage.sprite = recipe.finishedDishSprite;
                dishImage.color = isUnlocked ? Color.white : new Color(0.35f, 0.35f, 0.35f, 0.85f);
            }

            if (descriptionText != null)
            {
                descriptionText.text = !string.IsNullOrEmpty(recipe.dishDescription)
                    ? $"<i>{recipe.dishDescription}</i>"
                    : "<i>Món ăn đặc sắc trong thực đơn Bếp Giao Hòa.</i>";
            }

            if (culturalStoryText != null)
            {
                culturalStoryText.text = isUnlocked
                    ? (!string.IsNullOrEmpty(recipe.culturalStory) ? $"<b>NGUỒN GỐC & LỊCH SỬ:</b>\n{recipe.culturalStory}" : "Câu chuyện văn hóa đang được cập nhật.")
                    : "<b>KHÓA VĂN HÓA:</b> Hãy học nấu món này để mở khóa câu chuyện nguồn gốc lịch sử và ý nghĩa văn hóa!";
            }

            if (flavorProfileText != null)
            {
                flavorProfileText.text = !string.IsNullOrEmpty(recipe.flavorProfile)
                    ? $"<b>HƯƠNG VỊ:</b> {recipe.flavorProfile}"
                    : "<b>HƯƠNG VỊ:</b> Đậm đà bản sắc Á Đông";
            }

            if (chefTipText != null)
            {
                chefTipText.text = isUnlocked && !string.IsNullOrEmpty(recipe.chefTip)
                    ? $"<b>BÍ QUYẾT ĐẦU BẾP:</b> {recipe.chefTip}"
                    : "<b>BÍ QUYẾT ĐẦU BẾP:</b> Cần học mở khóa món để xem kỹ thuật chế biến.";
            }

            // Update unlock / mastered controls
            if (masteredBadge != null) masteredBadge.SetActive(isUnlocked);
            if (unlockSection != null)
            {
                unlockSection.SetActive(!isUnlocked);
                if (!isUnlocked && unlockCostText != null)
                {
                    unlockCostText.text = $"HỌC CÔNG THỨC ({recipe.unlockCostCoins} XU)";
                }

                if (unlockButton != null)
                {
                    unlockButton.interactable = CookingSession.Coins >= recipe.unlockCostCoins;
                }
            }
        }

        void HandleUnlockClicked()
        {
            if (currentSelectedRecipe == null) return;
            var progression = CookingServiceLocator.Get<IRecipeProgressionService>();
            if (progression == null) return;

            int currentCoins = CookingSession.Coins;
            if (progression.TryUnlockRecipe(currentSelectedRecipe, ref currentCoins))
            {
                int spent = CookingSession.Coins - currentCoins;
                CookingSession.Coins = currentCoins;
                economyEventChannel?.RaiseCoinsUpdated(CookingSession.Coins, -spent);

                PopulateList();
                SelectRecipe(currentSelectedRecipe);
            }
        }
    }
}
