using System;
using System.Collections.Generic;

namespace CookAndDeliver.Delivery.Core
{
    /// <summary>Scene-independent, deterministic 50 Hz delivery simulation.</summary>
    public sealed class DeliverySimulation
    {
        private readonly DeliverySettings settings;
        private readonly IDeliveryRouteGenerator routeGenerator;
        private readonly List<DeliveryHazard> visibleHazards = new List<DeliveryHazard>();
        private readonly IReadOnlyList<DeliveryHazard> visibleView;
        private readonly List<OrderResult> orderResults = new List<OrderResult>();
        private IReadOnlyList<DeliveryHazard> route = Array.Empty<DeliveryHazard>();
        private int[] stopLanes = Array.Empty<int>();
        private DeliveryState stateBeforePause;
        private float accumulator;
        private float invulnerabilityRemaining;
        private float slowdownRemaining;
        private float laneElapsed;
        private float laneStartX;
        private int queuedDirection;
        private bool changingLane;

        public DeliveryState State { get; private set; } = DeliveryState.Ready;
        public float RiderX { get; private set; } = DeliverySettings.LaneX(1);
        public int Lane { get; private set; } = 1;
        public float Distance { get; private set; }
        public int Integrity { get; private set; }
        public int Score { get; private set; }
        public int DeliveredCount { get; private set; }
        public int CollisionCount { get; private set; }
        public int CurrentOrderIndex { get; private set; }
        public float CountdownRemaining { get; private set; }
        public bool IsInvulnerable => invulnerabilityRemaining > 0f;
        public bool HasQueuedInput => queuedDirection != 0;
        public bool IsChangingLane => changingLane;
        public bool CanSteer => State == DeliveryState.Running;
        public IReadOnlyList<DeliveryHazard> Hazards => visibleView;
        public DeliveryRunRequest Request { get; private set; }
        public DeliveryRunResult Result { get; private set; }
        public float Speed => State == DeliveryState.Running
            ? BaseSpeed * (slowdownRemaining > 0f ? settings.SlowdownMultiplier : 1f) : 0f;
        public float BaseSpeed => settings.SpeedForLeg(CurrentOrderIndex);
        public float CurrentOrderProgress => Request == null ? 0f : CurrentOrderIndex >= Request.Orders.Count
            ? 1f : Clamp01((Distance - CurrentOrderIndex * settings.LegLength) / settings.LegLength);
        public float DistanceToNextStop => Request == null || CurrentOrderIndex >= Request.Orders.Count
            ? 0f : Math.Max(0f, (CurrentOrderIndex + 1) * settings.LegLength - Distance);
        public int NextStopLane => stopLanes.Length == 0 ? 0 : stopLanes[Math.Min(CurrentOrderIndex, stopLanes.Length - 1)];
        public float NextStopY => DeliverySettings.PlayerY + DistanceToNextStop;

        public event Action StateChanged;
        /// <summary>Reports remaining integrity, not the amount of damage.</summary>
        public event Action<int> Damaged;
        public event Action<OrderResult> OrderResolved;
        public event Action<DeliveryRunResult> RunCompleted;

        public DeliverySimulation(DeliverySettings settings, IDeliveryRouteGenerator routeGenerator = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            settings.Validate();
            this.settings = settings.Clone();
            this.routeGenerator = routeGenerator ?? new DeliveryRoutePlanner();
            Integrity = this.settings.StartingIntegrity;
            visibleView = visibleHazards.AsReadOnly();
        }

        public void StartRun(DeliveryRunRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            Reset();
            Request = request;
            stopLanes = new int[request.Orders.Count];
            var random = new Random(unchecked(request.Seed ^ 0x61B48A2D));
            for (int i = 0; i < stopLanes.Length; i++)
                stopLanes[i] = i == 0 ? 0 : i == 1 ? 2 : random.Next(2) * 2;
            // Copy route objects: a generator may cache templates between runs.
            var generated = routeGenerator.Generate(settings, request, Array.AsReadOnly(stopLanes));
            var copy = new List<DeliveryHazard>(generated.Count);
            var ids = new HashSet<int>();
            foreach (var hazard in generated)
            {
                if (hazard == null || !ids.Add(hazard.Id)) throw new ArgumentException("Route hazards must be non-null with unique IDs.");
                copy.Add(new DeliveryHazard(hazard.Id, hazard.Kind, hazard.Lane, hazard.CrossDistance));
            }
            route = copy.AsReadOnly();
            CountdownRemaining = settings.CountdownSeconds;
            SetState(CountdownRemaining > 0f ? DeliveryState.Countdown : DeliveryState.Running);
            UpdateHazardViews();
        }

        public void RequestLaneChange(int direction)
        {
            if (!CanSteer || direction == 0) return;
            direction = Math.Sign(direction);
            if (changingLane)
            {
                if (queuedDirection == 0 && Lane + direction >= 0 && Lane + direction <= 2)
                    queuedDirection = direction;
                return;
            }
            BeginLaneChange(direction);
        }

        public void Pause()
        {
            if (State != DeliveryState.Running && State != DeliveryState.Countdown) return;
            stateBeforePause = State;
            queuedDirection = 0;
            SetState(DeliveryState.Paused);
        }

        public void Resume()
        {
            if (State == DeliveryState.Paused) SetState(stateBeforePause);
        }

        public void AbortRun()
        {
            Reset();
            SetState(DeliveryState.Ready);
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (State != DeliveryState.Running && State != DeliveryState.Countdown) return;
            accumulator += deltaTime;
            while (accumulator + 0.000001f >= DeliverySettings.FixedStep)
            {
                accumulator -= DeliverySettings.FixedStep;
                Step(DeliverySettings.FixedStep);
                if (State != DeliveryState.Running && State != DeliveryState.Countdown)
                {
                    accumulator = 0f;
                    break;
                }
            }
        }

        private void Step(float dt)
        {
            if (State == DeliveryState.Countdown)
            {
                CountdownRemaining = Math.Max(0f, CountdownRemaining - dt);
                if (CountdownRemaining <= 0.00001f)
                {
                    CountdownRemaining = 0f;
                    SetState(DeliveryState.Running);
                }
                return;
            }

            float previousX = RiderX;
            float previousDistance = Distance;
            float nextDistance = Distance + Speed * dt;
            float oldSlowdown = slowdownRemaining;
            invulnerabilityRemaining = Math.Max(0f, invulnerabilityRemaining - dt);
            slowdownRemaining = Math.Max(0f, oldSlowdown - dt);
            UpdateLane(dt);
            Distance = nextDistance;
            CheckCollisions(previousX, previousDistance);
            if (State != DeliveryState.Running) { UpdateHazardViews(); return; }

            while (CurrentOrderIndex < Request.Orders.Count && Distance >= (CurrentOrderIndex + 1) * settings.LegLength)
            {
                float stopDistance = (CurrentOrderIndex + 1) * settings.LegLength;
                float fraction = Distance <= previousDistance ? 1f : Clamp01((stopDistance - previousDistance) / (Distance - previousDistance));
                float crossingX = previousX + (RiderX - previousX) * fraction;
                ResolveOrder(Math.Abs(crossingX - DeliverySettings.LaneX(NextStopLane)) <= DeliverySettings.LaneWidth * 0.2f);
                if (CurrentOrderIndex == Request.Orders.Count)
                {
                    Distance = Request.Orders.Count * settings.LegLength;
                    Complete(EndReason.RouteCompleted);
                    break;
                }
            }
            UpdateHazardViews();
        }

        private void CheckCollisions(float previousX, float previousDistance)
        {
            foreach (var hazard in route)
            {
                if (hazard.Hit) continue;
                float previousY = DeliverySettings.PlayerY + hazard.CrossDistance - previousDistance;
                float nextY = DeliverySettings.PlayerY + hazard.CrossDistance - Distance;
                if (!SweptOverlap(previousX, RiderX, previousY, nextY, hazard)) continue;
                // Even during immunity, the contact is consumed once for this object.
                hazard.Hit = true;
                if (IsInvulnerable) continue;
                CollisionCount++;
                Integrity = Math.Max(0, Integrity - settings.CollisionDamage);
                invulnerabilityRemaining = settings.InvulnerabilitySeconds;
                slowdownRemaining = settings.SlowdownSeconds;
                Damaged?.Invoke(Integrity);
                if (Integrity != 0) continue;
                while (CurrentOrderIndex < Request.Orders.Count)
                {
                    var lost = new OrderResult(Request.Orders[CurrentOrderIndex++], OrderOutcome.CargoLost, 0, 0);
                    orderResults.Add(lost);
                    OrderResolved?.Invoke(lost);
                }
                Complete(EndReason.CargoLost);
                break;
            }
        }

        private static bool SweptOverlap(float x0, float x1, float y0, float y1, DeliveryHazard hazard)
        {
            float enter = 0f;
            float exit = 1f;
            float halfX = (DeliverySettings.PlayerWidth + hazard.Width) * 0.5f;
            float halfY = (DeliverySettings.PlayerHeight + hazard.Height) * 0.5f;
            float centerX = DeliverySettings.LaneX(hazard.Lane);
            return ClipAxis(x0, x1, centerX - halfX, centerX + halfX, ref enter, ref exit)
                && ClipAxis(y0, y1, DeliverySettings.PlayerY - halfY, DeliverySettings.PlayerY + halfY, ref enter, ref exit);
        }

        private static bool ClipAxis(float start, float end, float minimum, float maximum, ref float enter, ref float exit)
        {
            float delta = end - start;
            if (Math.Abs(delta) < 0.00001f) return start >= minimum && start <= maximum;
            float first = (minimum - start) / delta;
            float last = (maximum - start) / delta;
            if (first > last) { float swap = first; first = last; last = swap; }
            enter = Math.Max(enter, first);
            exit = Math.Min(exit, last);
            return enter <= exit;
        }

        private void BeginLaneChange(int direction)
        {
            int nextLane = Lane + direction;
            if (nextLane < 0 || nextLane > 2) return;
            laneStartX = RiderX;
            Lane = nextLane;
            laneElapsed = 0f;
            changingLane = true;
        }

        private void UpdateLane(float dt)
        {
            if (!changingLane) return;
            laneElapsed += dt;
            float t = Clamp01(laneElapsed / settings.LaneChangeSeconds);
            float eased = 1f - (1f - t) * (1f - t) * (1f - t);
            RiderX = laneStartX + (DeliverySettings.LaneX(Lane) - laneStartX) * eased;
            if (t < 1f - 0.00001f) return;
            RiderX = DeliverySettings.LaneX(Lane);
            changingLane = false;
            if (queuedDirection == 0) return;
            int next = queuedDirection;
            queuedDirection = 0;
            BeginLaneChange(next);
        }

        private void ResolveOrder(bool delivered)
        {
            var order = Request.Orders[CurrentOrderIndex++];
            int reward = delivered ? (int)Math.Round(order.BaseReward * (double)Integrity / 100d, MidpointRounding.AwayFromZero) : 0;
            var result = new OrderResult(order, delivered ? OrderOutcome.Delivered : OrderOutcome.Missed, Integrity, reward);
            orderResults.Add(result);
            if (delivered) DeliveredCount++;
            Score += reward;
            OrderResolved?.Invoke(result);
        }

        private void Complete(EndReason reason)
        {
            if (Result != null) return;
            queuedDirection = 0;
            changingLane = false;
            Result = new DeliveryRunResult(reason, Request.Seed, Score, CollisionCount, orderResults);
            SetState(DeliveryState.Results);
            RunCompleted?.Invoke(Result);
        }

        private void UpdateHazardViews()
        {
            visibleHazards.Clear();
            foreach (var hazard in route)
            {
                hazard.Y = DeliverySettings.PlayerY + hazard.CrossDistance - Distance;
                if (hazard.Y >= -64f && hazard.Y <= DeliverySettings.WorldHeight + 64f)
                    visibleHazards.Add(hazard);
            }
        }

        private void Reset()
        {
            Request = null;
            Result = null;
            Distance = 0f;
            Score = 0;
            DeliveredCount = 0;
            CollisionCount = 0;
            CurrentOrderIndex = 0;
            Integrity = settings.StartingIntegrity;
            Lane = 1;
            RiderX = DeliverySettings.LaneX(1);
            queuedDirection = 0;
            changingLane = false;
            laneElapsed = 0f;
            invulnerabilityRemaining = 0f;
            slowdownRemaining = 0f;
            accumulator = 0f;
            CountdownRemaining = 0f;
            orderResults.Clear();
            route = Array.Empty<DeliveryHazard>();
            stopLanes = Array.Empty<int>();
            visibleHazards.Clear();
        }

        private void SetState(DeliveryState state)
        {
            State = state;
            StateChanged?.Invoke();
        }

        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }
}
