using UnityEngine;

namespace CookAndDeliver.Delivery
{
    /// <summary>A reusable, manually stepped impact. Pause freezes particles as well as camera shake.</summary>
    public sealed class DeliveryImpactFeedback : MonoBehaviour
    {
        public ParticleSystem sparks, dust;
        public float shakeDuration = .24f;
        public float shakePixels = 1.5f;
        private float elapsed = 1;
        private float impactDistance;
        private Vector3 origin;
        public Vector2 CameraOffset { get; private set; }
        public float Recoil { get; private set; }
        public float Roll { get; private set; }
        public float Flash => Mathf.Clamp01(1 - elapsed / .09f);
        public int ParticleCount => sparks.particleCount + dust.particleCount;

        public void Trigger(float riderX, float roadDistance)
        {
            Clear();
            elapsed = 0;
            impactDistance = roadDistance;
            origin = new Vector3(riderX / 32, (Core.DeliverySettings.PlayerY + 9) / 32, 0);
            transform.localPosition = origin;
            Emit(sparks, 10, 45, .18f, 5.5f, new Color32(255, 204, 87, 255), new Color32(255, 244, 200, 255));
            Emit(dust, 6, 19, .32f, 9f, new Color32(138, 127, 106, 170), new Color32(187, 178, 154, 150));
        }

        private static void Emit(ParticleSystem system, int count, float speed, float lifetime, float size, Color a, Color b)
        {
            for (var i = 0; i < count; i++)
            {
                var angle = (i * 137.5f + 25) * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
                var emit = new ParticleSystem.EmitParams
                {
                    position = direction * (1.5f / 32),
                    velocity = direction * (speed * (.65f + i % 4 * .15f) / 32),
                    startLifetime = lifetime + i % 3 * .035f,
                    startSize = (size + i % 2) / 32,
                    startColor = i % 2 == 0 ? a : b,
                    rotation = 0
                };
                system.Emit(emit, 1);
            }
            system.Pause(false);
        }

        public void Tick(float delta, float roadDistance)
        {
            elapsed += delta;
            var envelope = Mathf.Clamp01(1 - elapsed / shakeDuration);
            envelope *= envelope;
            CameraOffset = new Vector2(Mathf.Round(Mathf.Sin(elapsed * 100) * shakePixels * envelope),
                Mathf.Round(Mathf.Cos(elapsed * 83) * shakePixels * .65f * envelope));
            Recoil = 2 * envelope;
            Roll = Mathf.Sin(elapsed * 80) * 4 * envelope;
            // The burst stays at its road contact point while the rider keeps moving forward.
            transform.localPosition = origin + Vector3.down * ((roadDistance - impactDistance) / 32);
            if (delta <= 0) return;
            if (sparks.particleCount > 0) sparks.Simulate(delta, false, false, false);
            if (dust.particleCount > 0) dust.Simulate(delta, false, false, false);
        }

        public void Clear()
        {
            sparks.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            dust.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            elapsed = 1;
            CameraOffset = Vector2.zero;
            Recoil = Roll = 0;
            origin = Vector3.zero;
            impactDistance = 0;
            transform.localPosition = Vector3.zero;
        }
    }
}
