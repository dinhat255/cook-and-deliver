using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CookAndDeliver.Delivery.Core
{
    public enum DeliveryState { Ready, Countdown, Running, Paused, Results }
    public enum OrderOutcome { Pending, Delivered, Missed, CargoLost }
    public enum EndReason { RouteCompleted, CargoLost }
    public enum HazardKind { Cone, Pothole, Barrier, TrafficGreen, TrafficRed }

    public sealed class DeliveryOrder
    {
        public string Id { get; }
        public string Name { get; }
        public int BaseReward { get; }

        public DeliveryOrder(string id, string name, int baseReward = 100)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Order ID is required.", nameof(id));
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Order name is required.", nameof(name));
            if (baseReward < 0) throw new ArgumentOutOfRangeException(nameof(baseReward));
            Id = id;
            Name = name;
            BaseReward = baseReward;
        }
    }

    public sealed class DeliveryRunRequest
    {
        public int Seed { get; }
        public IReadOnlyList<DeliveryOrder> Orders { get; }

        public DeliveryRunRequest(int seed, IEnumerable<DeliveryOrder> orders)
        {
            if (orders == null) throw new ArgumentNullException(nameof(orders));
            var copy = new List<DeliveryOrder>(orders);
            if (copy.Count == 0) throw new ArgumentException("At least one order is required.", nameof(orders));
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var order in copy)
            {
                if (order == null) throw new ArgumentException("Orders cannot contain null.", nameof(orders));
                if (!ids.Add(order.Id)) throw new ArgumentException("Order IDs must be unique.", nameof(orders));
            }
            Seed = seed;
            Orders = new ReadOnlyCollection<DeliveryOrder>(copy);
        }
    }

    public sealed class OrderResult
    {
        public DeliveryOrder Order { get; }
        public OrderOutcome Outcome { get; }
        public int Integrity { get; }
        public int Reward { get; }

        public OrderResult(DeliveryOrder order, OrderOutcome outcome, int integrity, int reward)
        {
            Order = order;
            Outcome = outcome;
            Integrity = integrity;
            Reward = reward;
        }
    }

    public sealed class DeliveryRunResult
    {
        public EndReason Reason { get; }
        public int Seed { get; }
        public int Score { get; }
        public int CollisionCount { get; }
        public int DeliveredCount { get; }
        public int OrderCount => Orders.Count;
        public IReadOnlyList<OrderResult> Orders { get; }

        internal DeliveryRunResult(EndReason reason, int seed, int score, int collisionCount, IList<OrderResult> orders)
        {
            Reason = reason;
            Seed = seed;
            Score = score;
            CollisionCount = collisionCount;
            Orders = new ReadOnlyCollection<OrderResult>(new List<OrderResult>(orders));
            foreach (var order in orders)
                if (order.Outcome == OrderOutcome.Delivered) DeliveredCount++;
        }
    }

    public sealed class DeliveryHazard
    {
        public int Id { get; }
        public HazardKind Kind { get; }
        public int Lane { get; }
        public float CrossDistance { get; }
        public float Y { get; internal set; }
        public bool Hit { get; internal set; }
        public float Width { get; }
        public float Height { get; }
        public bool IsTraffic => Kind == HazardKind.TrafficGreen || Kind == HazardKind.TrafficRed;

        public DeliveryHazard(int id, HazardKind kind, int lane, float crossDistance)
        {
            if (lane < 0 || lane > 2) throw new ArgumentOutOfRangeException(nameof(lane));
            if (crossDistance < 0f || float.IsNaN(crossDistance) || float.IsInfinity(crossDistance))
                throw new ArgumentOutOfRangeException(nameof(crossDistance));
            Id = id;
            Kind = kind;
            Lane = lane;
            CrossDistance = crossDistance;
            switch (kind)
            {
                // Logical pixels; deliberately inset from the visible sourced sprite.
                case HazardKind.Cone: Width = 54f; Height = 14f; break;
                case HazardKind.Pothole: Width = 20f; Height = 18f; break;
                case HazardKind.Barrier: Width = 54f; Height = 16f; break;
                default: Width = 30f; Height = 62f; break;
            }
        }

        // Cars are parked: all hazards occupy fixed positions on the scrolling road.
        public float HalfOccupancyDistance(float margin = 0f)
            => (Height + DeliverySettings.PlayerHeight) * 0.5f + margin;
    }

    public interface IDeliveryRouteGenerator
    {
        IReadOnlyList<DeliveryHazard> Generate(DeliverySettings settings, DeliveryRunRequest request, IReadOnlyList<int> stopLanes);
    }
}
