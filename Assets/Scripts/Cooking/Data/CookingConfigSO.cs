using UnityEngine;

namespace CookAndDeliver.Cooking.Data
{
    // Tap trung toan bo thong so can bang gameplay vao mot ScriptableObject
    [CreateAssetMenu(fileName = "CookingConfig", menuName = "Cooking/Clean/Cooking Config", order = 0)]
    public class CookingConfigSO : ScriptableObject
    {
        [Header("Don hang")]
        public int maxActiveOrders = 3;
        public float orderPatienceDuration = 60f;
        public float orderSpawnIntervalMin = 7f;
        public float orderSpawnIntervalMax = 11f;
        [Range(0f, 1f)] public float shipOrderProbability = 0.4f;

        [Header("Giao ship")]
        public int shipGoal = 3;

        [Header("Tien te va phat")]
        public int burnPenaltyCoins = 15;
        public int trashFeeCoins = 5;
        public float tipThresholdRatio = 0.5f; // Neu phuc vu khi con > 50% thoi gian thi thuong tip
        public float tipPercent = 0.2f;        // 20% gia mon
        public float comboMultiplierPerStep = 0.1f; // +10% moi combo (toi da 10)

        [Header("Thoi gian hoat hoa (giay)")]
        public float smartTapFlyDuration = 0.18f;
        public float serveFlyDuration = 0.4f;
    }
}
