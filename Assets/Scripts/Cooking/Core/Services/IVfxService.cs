using System;
using UnityEngine;

namespace CookAndDeliver.Cooking.Core.Services
{
    // Interface for visual effects service animating flying ingredients, toasts, puffs, and sparkles
    public interface IVfxService : IService
    {
        void FlyArc(Sprite sprite, Color tint, Vector3 fromWorld, Vector3 toWorld, float duration, Action onArrive);
        void FlyBounce(Sprite sprite, Color tint, Vector3 fromWorld, Vector3 toWorld, float duration);
        void ShowToast(string message, Vector3 worldPos, Color color);
        void EmitSparkle(Vector3 worldPos, Color color, int count, float distance);
        void EmitPuff(Vector3 worldPos, Color color);
    }
}
