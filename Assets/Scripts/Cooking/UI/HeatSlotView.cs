using System;
using CookAndDeliver.Cooking.Core;
using CookAndDeliver.Cooking.Data;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace CookAndDeliver.Cooking.UI
{
    // View controlling an individual heating station slot (grill, oven, or sauce pan)
    public class HeatSlotView : MonoBehaviour
    {
        [Header("UI References")]
        public Image iconImage;
        public Image progressBarBackground;
        public Image progressBarFill;
        public Button clickButton;

        [Header("Default Timers (Fallback if IngredientSO doesn't specify)")]
        public float defaultCookDuration = 3f;
        public float defaultWarnDuration = 6f;
        public float defaultBurnDuration = 8f;
        public bool allowBurn = true;

        public HeatSlotState CurrentState { get; private set; } = HeatSlotState.Idle;
        public bool IsReserved { get; set; }
        public IngredientSO CurrentIngredient { get; private set; }

        public Action<Vector3, Color> OnEmitPuff;
        public Action<HeatSlotView> OnCookPerfect;
        public Action<HeatSlotView, HeatSlotState> OnStateChanged;

        float timer;
        float puffTimer;
        float cookDuration;
        float warnDuration;
        float burnDuration;
        bool externalBurnDisabled;

        static readonly Color ProgressGreen = new Color(0.3f, 0.85f, 0.35f);
        static readonly Color ProgressGold = new Color(1f, 0.82f, 0.2f);
        static readonly Color ProgressRed = new Color(1f, 0.25f, 0.2f);

        public Sprite IconSprite => iconImage != null ? iconImage.sprite : null;
        public Color IconColor => iconImage != null ? iconImage.color : Color.white;
        public Vector3 IconWorldPos => iconImage != null ? iconImage.transform.position : transform.position;
        public bool IsFree => CurrentState == HeatSlotState.Idle && !IsReserved;

        void Awake()
        {
            ClearSlot();
        }

        public void PlaceIngredient(IngredientSO ingredient)
        {
            CurrentIngredient = ingredient;
            IsReserved = false;
            CurrentState = HeatSlotState.Cooking;
            timer = 0f;
            puffTimer = 0.2f;

            cookDuration = ingredient.cookDuration > 0 ? ingredient.cookDuration : defaultCookDuration;
            warnDuration = ingredient.warnDuration > 0 ? ingredient.warnDuration : defaultWarnDuration;
            burnDuration = ingredient.burnDuration > 0 ? ingredient.burnDuration : defaultBurnDuration;

            if (iconImage != null)
            {
                iconImage.enabled = true;
                iconImage.sprite = ingredient.rawSprite;
                iconImage.color = ingredient.GetDisplayColor(false);
                iconImage.transform.localScale = Vector3.one;
                iconImage.transform.DOKill();
                iconImage.transform.DOPunchScale(Vector3.one * 0.25f, 0.2f, 6, 0.6f);
            }

            if (progressBarBackground != null) progressBarBackground.gameObject.SetActive(true);
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = 0f;
                progressBarFill.color = ProgressGreen;
            }
        }

        public void ClearSlot()
        {
            CurrentState = HeatSlotState.Idle;
            IsReserved = false;
            CurrentIngredient = null;
            if (iconImage != null)
            {
                iconImage.transform.DOKill();
                iconImage.transform.localScale = Vector3.one;
                iconImage.enabled = false;
            }
            if (progressBarBackground != null) progressBarBackground.gameObject.SetActive(false);
        }

        public void SetExternalBurnDisabled(bool disabled)
        {
            externalBurnDisabled = disabled;
        }

        public void Tick(float deltaTime)
        {
            if (CurrentState == HeatSlotState.Idle || CurrentIngredient == null) return;

            timer += deltaTime;
            var prevState = CurrentState;

            bool canBurn = allowBurn && !externalBurnDisabled && CurrentIngredient.canBurn;
            float activeWarn = canBurn ? warnDuration : float.PositiveInfinity;
            float activeBurn = canBurn ? burnDuration : float.PositiveInfinity;

            if (timer >= activeBurn) CurrentState = HeatSlotState.Burnt;
            else if (timer >= activeWarn) CurrentState = HeatSlotState.Warning;
            else if (timer >= cookDuration) CurrentState = HeatSlotState.Perfect;
            else CurrentState = HeatSlotState.Cooking;

            bool stateChanged = prevState != CurrentState;
            UpdateVisuals(stateChanged, activeWarn, activeBurn);
            if (stateChanged)
            {
                OnStateChanged?.Invoke(this, CurrentState);
            }

            // Emit smoke puff particles
            puffTimer -= deltaTime;
            if (puffTimer <= 0f && OnEmitPuff != null && iconImage != null)
            {
                bool burnt = CurrentState == HeatSlotState.Burnt;
                puffTimer = burnt ? 0.22f : 0.5f;
                Color puffColor = burnt
                    ? new Color(0.1f, 0.1f, 0.1f, 0.8f)
                    : CurrentState == HeatSlotState.Warning
                        ? new Color(0.55f, 0.55f, 0.55f, 0.7f)
                        : new Color(1f, 1f, 1f, 0.55f);

                OnEmitPuff(iconImage.transform.position + new Vector3(UnityEngine.Random.Range(-25f, 25f), 30f, 0f), puffColor);
            }
        }

        void UpdateVisuals(bool stateChanged, float activeWarn, float activeBurn)
        {
            if (iconImage == null || progressBarFill == null) return;

            Color rawTint = CurrentIngredient.GetDisplayColor(false);
            Color cookedTint = CurrentIngredient.GetDisplayColor(true);
            bool blink = Mathf.Sin(Time.time * 22f) > 0f;

            switch (CurrentState)
            {
                case HeatSlotState.Cooking:
                    iconImage.sprite = CurrentIngredient.rawSprite;
                    iconImage.color = Color.Lerp(rawTint, cookedTint, timer / cookDuration);
                    progressBarFill.fillAmount = timer / cookDuration;
                    progressBarFill.color = ProgressGreen;
                    break;

                case HeatSlotState.Perfect:
                    iconImage.sprite = CurrentIngredient.GetDisplaySprite(true);
                    iconImage.color = cookedTint;
                    progressBarFill.fillAmount = float.IsInfinity(activeWarn)
                        ? 1f
                        : 1f - (timer - cookDuration) / (activeWarn - cookDuration);
                    progressBarFill.color = ProgressGold;

                    if (stateChanged)
                    {
                        iconImage.transform.DOKill();
                        iconImage.transform.localScale = Vector3.one;
                        iconImage.transform.DOPunchScale(Vector3.one * 0.35f, 0.3f, 6, 0.6f);
                        OnCookPerfect?.Invoke(this);
                    }
                    break;

                case HeatSlotState.Warning:
                    iconImage.sprite = CurrentIngredient.GetDisplaySprite(true);
                    iconImage.color = blink ? Color.Lerp(cookedTint, ProgressRed, 0.55f) : cookedTint;
                    progressBarFill.fillAmount = 1f - (timer - activeWarn) / (activeBurn - activeWarn);
                    progressBarFill.color = blink ? ProgressRed : new Color(0.55f, 0.1f, 0.1f);
                    break;

                case HeatSlotState.Burnt:
                    iconImage.sprite = CurrentIngredient.GetDisplaySprite(true);
                    iconImage.color = new Color(0.14f, 0.13f, 0.13f);
                    progressBarFill.fillAmount = 1f;
                    progressBarFill.color = new Color(0.2f, 0.2f, 0.2f);

                    if (stateChanged)
                    {
                        iconImage.transform.DOKill();
                        iconImage.transform.localScale = Vector3.one;
                        iconImage.transform.DOShakePosition(0.3f, 10f, 30);
                    }
                    break;
            }
        }
    }
}
