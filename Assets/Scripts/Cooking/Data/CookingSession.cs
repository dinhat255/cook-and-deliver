using UnityEngine;

namespace CookAndDeliver.Cooking.Data
{
    // Du lieu phien choi duoc luu giu khi chuyen giua Cooking va Delivery scene
    public static class CookingSession
    {
        public static int Coins { get; set; }
        public static int Combo { get; set; }
        public static int CompletedOrdersCount { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession()
        {
            Coins = 0;
            Combo = 0;
            CompletedOrdersCount = 0;
        }
    }
}
