using CookAndDeliver.Cooking.Core.Events;
using CookAndDeliver.Cooking.UI;
using DG.Tweening;
using UnityEngine;

namespace CookAndDeliver.Cooking.Presenters
{
    // Presenter in MVP managing top HUD data and presentation via Event Channel
    public class HudPresenter : MonoBehaviour
    {
        [Header("Views")]
        public HudView hudView;

        [Header("Event Channels")]
        public EconomyEventChannelSO economyEventChannel;

        void OnEnable()
        {
            if (economyEventChannel != null)
            {
                economyEventChannel.OnCoinsUpdated += HandleCoinsUpdated;
                economyEventChannel.OnComboUpdated += HandleComboUpdated;
                economyEventChannel.OnShipCountUpdated += HandleShipCountUpdated;
            }
        }

        void OnDisable()
        {
            if (economyEventChannel != null)
            {
                economyEventChannel.OnCoinsUpdated -= HandleCoinsUpdated;
                economyEventChannel.OnComboUpdated -= HandleComboUpdated;
                economyEventChannel.OnShipCountUpdated -= HandleShipCountUpdated;
            }
        }

        void HandleCoinsUpdated(int totalCoins, int delta)
        {
            if (hudView != null)
            {
                hudView.UpdateCoins(totalCoins);
                if (delta > 0 && hudView.coinsText != null)
                {
                    hudView.coinsText.transform.DOKill();
                    hudView.coinsText.transform.localScale = Vector3.one;
                    hudView.coinsText.transform.DOPunchScale(Vector3.one * 0.3f, 0.25f, 6, 0.6f);
                }
            }
        }

        void HandleComboUpdated(int currentCombo)
        {
            if (hudView != null)
            {
                hudView.UpdateCombo(currentCombo);
            }
        }

        void HandleShipCountUpdated(int currentShip, int goal)
        {
            if (hudView != null)
            {
                hudView.UpdateShipCount(currentShip, goal);
            }
        }
    }
}
