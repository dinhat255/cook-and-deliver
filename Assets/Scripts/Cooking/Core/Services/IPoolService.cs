using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Services
{
    // Interface for managing pooled UI visual elements (toasts, sparks, puffs, flying icons)
    public interface IPoolService : IService
    {
        GameObject SpawnFlyArc(Transform parent);
        void ReturnFlyArc(GameObject go);

        GameObject SpawnToast(Transform parent);
        void ReturnToast(GameObject go);

        GameObject SpawnPuff(Transform parent);
        void ReturnPuff(GameObject go);

        GameObject SpawnSpark(Transform parent);
        void ReturnSpark(GameObject go);
    }
}
