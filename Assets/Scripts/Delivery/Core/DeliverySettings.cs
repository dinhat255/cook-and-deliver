using System;

namespace CookAndDeliver.Delivery.Core
{
    [Serializable]
    public sealed class DeliverySettings
    {
        public const float WorldWidth = 270f;
        public const float WorldHeight = 360f;
        public const float PlayerY = 72f;
        public const float LaneWidth = 64f;
        public const float PlayerWidth = 12f;
        public const float PlayerHeight = 30f;
        public const float FixedStep = 0.02f;

        public int OrderCount = 3;
        public float LegLength = 4320f;
        public float CountdownSeconds = 3f;
        public float LaneChangeSeconds = 0.12f;
        public int StartingIntegrity = 100;
        public int CollisionDamage = 25;
        public float InvulnerabilitySeconds = 1.25f;
        public float SlowdownSeconds = 0.45f;
        public float SlowdownMultiplier = 0.65f;
        public float Acceleration = 300f;
        public float WarningSeconds = 3f;
        public float ClearAfterStopSeconds = 1f;
        public float InitialClearSeconds = 2f;
        public float[] Speeds = { 252f, 300f, 348f };
        public float[] WaveIntervals = { 0.8f, 0.7f, 0.62f };
        public float[] TrafficChances = { 0.7f, 0.8f, 0.85f };
        public float[] DoubleWaveChances = { 0.55f, 0.7f, 0.8f };
        public bool EnableHazards = true;

        public static float LaneX(int lane)
        {
            if (lane < 0 || lane > 2) throw new ArgumentOutOfRangeException(nameof(lane));
            return 71f + lane * LaneWidth;
        }

        public float SpeedForLeg(int leg) => Speeds[Math.Min(Math.Max(0, leg), Speeds.Length - 1)];
        public float WaveIntervalForLeg(int leg) => WaveIntervals[Math.Min(Math.Max(0, leg), WaveIntervals.Length - 1)];
        public float TrafficChanceForLeg(int leg) => TrafficChances[Math.Min(Math.Max(0, leg), TrafficChances.Length - 1)];
        public float DoubleWaveChanceForLeg(int leg) => DoubleWaveChances[Math.Min(Math.Max(0, leg), DoubleWaveChances.Length - 1)];

        public DeliverySettings Clone()
        {
            var clone = (DeliverySettings)MemberwiseClone();
            clone.Speeds = Speeds == null ? null : (float[])Speeds.Clone();
            clone.WaveIntervals = WaveIntervals == null ? null : (float[])WaveIntervals.Clone();
            clone.TrafficChances = TrafficChances == null ? null : (float[])TrafficChances.Clone();
            clone.DoubleWaveChances = DoubleWaveChances == null ? null : (float[])DoubleWaveChances.Clone();
            return clone;
        }

        public void Validate()
        {
            if (OrderCount < 1) throw new ArgumentException("A delivery run needs at least one order.");
            Positive(LegLength, nameof(LegLength));
            Positive(LaneChangeSeconds, nameof(LaneChangeSeconds));
            NonNegative(CountdownSeconds, nameof(CountdownSeconds));
            Positive(InvulnerabilitySeconds, nameof(InvulnerabilitySeconds));
            NonNegative(SlowdownSeconds, nameof(SlowdownSeconds));
            Positive(Acceleration, nameof(Acceleration));
            Positive(WarningSeconds, nameof(WarningSeconds));
            NonNegative(ClearAfterStopSeconds, nameof(ClearAfterStopSeconds));
            NonNegative(InitialClearSeconds, nameof(InitialClearSeconds));
            if (StartingIntegrity < 1 || StartingIntegrity > 100 || CollisionDamage < 1)
                throw new ArgumentException("Integrity must be 1..100 and collision damage must be positive.");
            if (!Finite(SlowdownMultiplier) || SlowdownMultiplier <= 0f || SlowdownMultiplier > 1f)
                throw new ArgumentException("Slowdown multiplier must be in (0, 1].");
            ValidateArray(Speeds, nameof(Speeds));
            ValidateArray(WaveIntervals, nameof(WaveIntervals));
            ValidateProbabilities(TrafficChances, nameof(TrafficChances));
            ValidateProbabilities(DoubleWaveChances, nameof(DoubleWaveChances));
        }

        private static void ValidateProbabilities(float[] values, string name)
        {
            if (values == null || values.Length == 0) throw new ArgumentException(name + " cannot be empty.");
            foreach (float value in values)
                if (!Finite(value) || value < 0f || value > 1f)
                    throw new ArgumentException(name + " must contain probabilities in [0, 1].");
        }

        private static void ValidateArray(float[] values, string name)
        {
            if (values == null || values.Length == 0) throw new ArgumentException(name + " cannot be empty.");
            foreach (float value in values) Positive(value, name);
        }

        private static void Positive(float value, string name)
        {
            if (!Finite(value) || value <= 0f) throw new ArgumentException(name + " must be finite and positive.");
        }

        private static void NonNegative(float value, string name)
        {
            if (!Finite(value) || value < 0f) throw new ArgumentException(name + " must be finite and nonnegative.");
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
