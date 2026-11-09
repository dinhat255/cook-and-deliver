using System;
using System.Collections.Generic;

namespace CookAndDeliver.Delivery.Core
{
    /// <summary>
    /// Plans fixed road positions for props and parked cars. All scroll with the ground,
    /// so slowing the run never moves a collision into a reserved stop.
    /// Candidate waves must preserve a complete, conservatively timed route to the next stop.
    /// </summary>
    public sealed class DeliveryRoutePlanner : IDeliveryRouteGenerator
    {
        private const float SafetyMargin = 8f;
        private const float SearchStep = 0.05f;
        private const float ReactionMarginSeconds = 0.12f;

        public IReadOnlyList<DeliveryHazard> Generate(DeliverySettings settings, DeliveryRunRequest request, IReadOnlyList<int> stopLanes)
        {
            var route = new List<DeliveryHazard>();
            if (!settings.EnableHazards) return route.AsReadOnly();
            var random = new Random(request.Seed);
            int nextId = 1;
            for (int leg = 0; leg < request.Orders.Count; leg++)
            {
                float start = leg * settings.LegLength;
                float end = start + settings.LegLength;
                float speed = settings.SpeedForLeg(leg);
                float clearStart = start + speed * (leg == 0 ? settings.InitialClearSeconds : settings.ClearAfterStopSeconds);
                float clearEnd = end - speed * settings.WarningSeconds;
                float waveGap = speed * settings.WaveIntervalForLeg(leg);
                int entryLane = leg == 0 ? 1 : stopLanes[leg - 1];
                var local = new List<DeliveryHazard>();
                for (float cross = clearStart + speed; cross < clearEnd; cross += waveGap)
                {
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        int count = leg > 0 && random.NextDouble() < 0.35 ? 2 : 1;
                        int firstLane = random.Next(3);
                        int secondLane = (firstLane + 1 + random.Next(2)) % 3;
                        var candidate = new List<DeliveryHazard>(count);
                        for (int i = 0; i < count; i++)
                        {
                            var kind = PickKind(random, leg);
                            var hazard = new DeliveryHazard(nextId + candidate.Count, kind, i == 0 ? firstLane : secondLane, cross);
                            float reserve = hazard.HalfOccupancyDistance(SafetyMargin);
                            if (cross - reserve < clearStart || cross + reserve > clearEnd) continue;
                            candidate.Add(hazard);
                        }
                        if (candidate.Count == 0) continue;
                        local.AddRange(candidate);
                        if (HasReachablePath(settings, local, leg, entryLane, stopLanes[leg]))
                        {
                            nextId += candidate.Count;
                            break;
                        }
                        local.RemoveRange(local.Count - candidate.Count, candidate.Count);
                    }
                }
                route.AddRange(local);
            }
            return route.AsReadOnly();
        }

        private static HazardKind PickKind(Random random, int leg)
        {
            int roll = random.Next(100);
            if (roll < 35) return HazardKind.Cone;
            if (roll < 60) return HazardKind.Pothole;
            if (roll < 80 || leg == 0) return HazardKind.Barrier;
            return roll < 90 ? HazardKind.TrafficGreen : HazardKind.TrafficRed;
        }

        /// <summary>
        /// Time-expanded lane search. Lane changes reserve both lanes for the full transfer;
        /// waiting and moving are checked over every crossed time slice, not only wave centers.
        /// The final state must be at the actual receiving lane.
        /// </summary>
        public static bool HasReachablePath(DeliverySettings settings, IReadOnlyList<DeliveryHazard> hazards,
            int leg, int entryLane, int receivingLane)
        {
            float speed = settings.SpeedForLeg(leg);
            float duration = settings.LegLength / speed;
            int steps = (int)Math.Ceiling(duration / SearchStep);
            var blocked = new bool[steps + 1, 3];
            float start = leg * settings.LegLength;
            foreach (var hazard in hazards)
            {
                float reserve = hazard.HalfOccupancyDistance(SafetyMargin);
                int from = (int)Math.Floor((hazard.CrossDistance - reserve - start) / speed / SearchStep);
                int to = (int)Math.Ceiling((hazard.CrossDistance + reserve - start) / speed / SearchStep);
                for (int tick = Math.Max(0, from); tick <= Math.Min(steps, to); tick++)
                    blocked[tick, hazard.Lane] = true;
            }

            var reachable = new bool[steps + 1, 3];
            reachable[0, entryLane] = !blocked[0, entryLane];
            int transferSteps = Math.Max(1, (int)Math.Ceiling((settings.LaneChangeSeconds + ReactionMarginSeconds) / SearchStep));
            for (int tick = 0; tick < steps; tick++)
            {
                for (int lane = 0; lane < 3; lane++)
                {
                    if (!reachable[tick, lane]) continue;
                    if (!blocked[tick + 1, lane]) reachable[tick + 1, lane] = true;
                    int arrival = tick + transferSteps;
                    if (arrival > steps) continue;
                    for (int direction = -1; direction <= 1; direction += 2)
                    {
                        int target = lane + direction;
                        if (target < 0 || target > 2) continue;
                        bool clear = true;
                        for (int crossing = tick; crossing <= arrival; crossing++)
                        {
                            if (!blocked[crossing, lane] && !blocked[crossing, target]) continue;
                            clear = false;
                            break;
                        }
                        if (clear) reachable[arrival, target] = true;
                    }
                }
            }
            return reachable[steps, receivingLane];
        }
    }
}
