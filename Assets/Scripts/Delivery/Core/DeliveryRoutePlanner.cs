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
        private const float ReactionMarginSeconds = 0.25f;

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
                int safeLane = entryLane;
                int previousOpenLanes = 1 << entryLane;
                int waveIndex = 0;
                float previousCross = start;
                float previousReserve = 0f;
                for (float cross = clearStart + speed; cross < clearEnd; cross += waveGap)
                {
                    for (int attempt = 0; attempt < 12; attempt++)
                    {
                        // Two introductory singles, then one single every fourth wave.
                        bool breathingWave = waveIndex < 2 || waveIndex % 4 == 3;
                        int count = !breathingWave && random.NextDouble() < settings.DoubleWaveChanceForLeg(leg) ? 2 : 1;
                        int nextSafeLane = waveIndex == 0 ? entryLane : PickSafeLane(random, previousOpenLanes, safeLane);
                        int firstLane = (nextSafeLane + 1 + random.Next(2)) % 3;
                        int secondLane = 3 - nextSafeLane - firstLane;
                        var candidate = new List<DeliveryHazard>(count);
                        float reserve = 0f;
                        for (int i = 0; i < count; i++)
                        {
                            var kind = PickKind(random, settings.TrafficChanceForLeg(leg));
                            var hazard = new DeliveryHazard(nextId + candidate.Count, kind, i == 0 ? firstLane : secondLane, cross);
                            reserve = Math.Max(reserve, hazard.HalfOccupancyDistance(SafetyMargin));
                            candidate.Add(hazard);
                        }

                        float placedCross = Math.Max(cross, clearStart + reserve);
                        if (local.Count > 0 && previousOpenLanes != (1 << nextSafeLane))
                        {
                            // Every previous open lane reaches this corridor in at most one swipe.
                            // Count the full car occupancy, reaction time and discretization margins.
                            float transfer = settings.LaneChangeSeconds + ReactionMarginSeconds + 2f * SearchStep;
                            placedCross = Math.Max(placedCross, previousCross + previousReserve + reserve + speed * transfer);
                        }
                        if (placedCross + reserve > clearEnd) continue;
                        int openLanes = 7;
                        for (int i = 0; i < candidate.Count; i++)
                        {
                            var hazard = candidate[i];
                            candidate[i] = new DeliveryHazard(hazard.Id, hazard.Kind, hazard.Lane, placedCross);
                            openLanes &= ~(1 << hazard.Lane);
                        }
                        local.AddRange(candidate);
                        if (HasReachablePath(settings, local, leg, entryLane, stopLanes[leg]))
                        {
                            nextId += candidate.Count;
                            safeLane = nextSafeLane;
                            previousOpenLanes = openLanes;
                            previousCross = placedCross;
                            previousReserve = reserve;
                            cross = placedCross;
                            waveIndex++;
                            break;
                        }
                        local.RemoveRange(local.Count - candidate.Count, candidate.Count);
                    }
                }
                route.AddRange(local);
            }
            return route.AsReadOnly();
        }

        private static int PickSafeLane(Random random, int openLanes, int previousSafeLane)
        {
            var choices = new List<int>(3);
            for (int lane = 0; lane < 3; lane++)
            {
                bool reachable = true;
                for (int from = 0; from < 3; from++)
                    if ((openLanes & (1 << from)) != 0 && Math.Abs(lane - from) > 1) reachable = false;
                if (reachable) choices.Add(lane);
            }
            // Prefer a change, but allow short convoys sharing the same open lane.
            if (choices.Count > 1 && random.NextDouble() < 0.7) choices.Remove(previousSafeLane);
            return choices[random.Next(choices.Count)];
        }

        private static HazardKind PickKind(Random random, float trafficChance)
        {
            if (random.NextDouble() < trafficChance)
                return random.Next(2) == 0 ? HazardKind.TrafficGreen : HazardKind.TrafficRed;
            int roll = random.Next(100);
            return roll < 45 ? HazardKind.Cone : roll < 70 ? HazardKind.Pothole : HazardKind.Barrier;
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
