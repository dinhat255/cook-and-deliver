using System.Collections.Generic;
using CookAndDeliver.Delivery.Core;
using UnityEngine;

namespace CookAndDeliver.Delivery
{
    public sealed class DeliveryWorldView : MonoBehaviour
    {
        public Transform[] roadTiles;
        public Transform[] scenery;
        public Transform player;
        public SpriteRenderer rider;
        public SpriteRenderer bike;
        public SpriteRenderer bikeShadow;
        public Sprite idlePose, ridingPose, trashedPose;
        public Transform worldCamera;
        public DeliveryImpactFeedback impact;
        public DeliveryHazardVisual hazardPrefab;
        public Transform hazardRoot;
        public Sprite[] hazardSprites;
        public SpriteRenderer deliveryMarker, customer, laneHighlight;
        public int warmPoolSize = 48;
        public float warningSeconds = 3f;
        private readonly Queue<DeliveryHazardVisual> pool = new Queue<DeliveryHazardVisual>();
        private readonly Dictionary<int, DeliveryHazardVisual> active = new Dictionary<int, DeliveryHazardVisual>();
        private readonly HashSet<int> seen = new HashSet<int>();
        private readonly List<int> released = new List<int>();
        private float[] sceneryY;
        private float[] sceneryClearance;
        private int[] sceneryCycle;
        private bool[] sceneryHiddenForStop;
        private Vector3 cameraHome;
        private float steerRoll;
        private float previousX = 135;
        private int movementSign;
        private float poseTime;
        private float animationTime;
        private float warningTime;
        private int warningOrder = -1;
        public float DeliveryPulse { get; private set; } = 1f;
        public int ActiveHazardViews => active.Count;
        public int PooledHazardViews => pool.Count;

        private void Awake()
        {
            cameraHome = worldCamera.localPosition;
            sceneryY = new float[scenery.Length];
            sceneryClearance = new float[scenery.Length];
            sceneryCycle = new int[scenery.Length];
            sceneryHiddenForStop = new bool[scenery.Length];
            for (var i = 0; i < scenery.Length; i++)
            {
                sceneryY[i] = scenery[i].localPosition.y * 32;
                sceneryCycle[i] = int.MinValue;
                float treeRadius = 0;
                foreach (var renderer in scenery[i].GetComponentsInChildren<SpriteRenderer>(true))
                {
                    treeRadius = Mathf.Max(treeRadius, Mathf.Abs(renderer.bounds.min.y - scenery[i].position.y),
                        Mathf.Abs(renderer.bounds.max.y - scenery[i].position.y));
                }
                // Include canopy, shadow, receiver height and an eight-pixel pavement gap.
                sceneryClearance[i] = (treeRadius + customer.bounds.extents.y) * 32 + 8;
            }
            for (var i = 0; i < warmPoolSize; i++)
            {
                var item = Instantiate(hazardPrefab, hazardRoot);
                item.gameObject.SetActive(false);
                pool.Enqueue(item);
            }
        }

        public void Hit(float riderX, float distance) { impact.Trigger(riderX, distance); }
        public void ResetView()
        {
            foreach (var item in active.Values) { item.gameObject.SetActive(false); pool.Enqueue(item); }
            active.Clear();
            impact.Clear();
            worldCamera.localPosition = cameraHome;
            steerRoll = 0;
            player.localRotation = Quaternion.identity;
            rider.color = bike.color = Color.white;
            bikeShadow.color = new Color(0, 0, 0, .42f);
            previousX = 135;
            animationTime = 0;
            movementSign = 0;
            poseTime = 0;
            warningTime = 0;
            warningOrder = -1;
            DeliveryPulse = 1;
            for (var i = 0; i < scenery.Length; i++)
            {
                sceneryHiddenForStop[i] = false;
                sceneryCycle[i] = int.MinValue;
                scenery[i].gameObject.SetActive(true);
            }
        }

        public void Render(DeliverySimulation simulation, float delta, float interpolationAlpha = 1f)
        {
            if (simulation.State == DeliveryState.Paused) return;
            var distance = Mathf.Lerp(simulation.PreviousDistance, simulation.Distance, interpolationAlpha);
            var riderX = Mathf.Lerp(simulation.PreviousRiderX, simulation.RiderX, interpolationAlpha);
            var scrollOffset = simulation.Distance - distance;
            var customerY = simulation.NextStopY + scrollOffset + 10;
            var hasNextStop = simulation.Request != null && simulation.CurrentOrderIndex < simulation.Request.Orders.Count;
            for (var i = 0; i < roadTiles.Length; i++)
                roadTiles[i].localPosition = new Vector3(135f / 32, (i * 128 + 64 - distance % 128) / 32, 0);
            for (var i = 0; i < scenery.Length; i++)
            {
                var p = scenery[i].localPosition;
                // Trees stand on the pavement: keep the same ground speed and tile phase.
                p.y = (Mathf.Repeat(sceneryY[i] - distance + 64, 480) - 64) / 32;
                var cycle = Mathf.FloorToInt((sceneryY[i] - distance + 64) / 480);
                if (cycle != sceneryCycle[i])
                {
                    sceneryCycle[i] = cycle;
                    sceneryHiddenForStop[i] = false;
                }
                var samePavement = (p.x * 32 < DeliverySettings.WorldWidth / 2) == (simulation.NextStopLane == 0);
                if (hasNextStop && samePavement && Mathf.Abs(p.y * 32 - customerY) < sceneryClearance[i])
                    sceneryHiddenForStop[i] = true;
                // Reserve the gap before it enters view. Keep it empty after handoff until
                // this tree wraps offscreen, so it cannot pop back over the departing receiver.
                var visible = !sceneryHiddenForStop[i];
                if (scenery[i].gameObject.activeSelf != visible) scenery[i].gameObject.SetActive(visible);
                scenery[i].localPosition = p;
            }
            var moving = simulation.State == DeliveryState.Running;
            if (moving)
            {
                animationTime += delta;
                poseTime = Mathf.Max(0, poseTime - delta);
                var change = riderX - previousX;
                if (Mathf.Abs(change) > .1f) { movementSign = change < 0 ? -1 : 1; poseTime = .055f; }
            }
            previousX = riderX;
            impact.Tick(delta, distance);
            worldCamera.localPosition = cameraHome + new Vector3(impact.CameraOffset.x / 32, impact.CameraOffset.y / 32, 0);
            player.localPosition = new Vector3(riderX / 32, (DeliverySettings.PlayerY - impact.Recoil) / 32, 0);
            steerRoll = Mathf.MoveTowards(steerRoll, moving && poseTime > 0 ? -movementSign * 9 : 0, delta * 230);
            player.localRotation = Quaternion.Euler(0, 0, steerRoll + impact.Roll);
            var damaged = simulation.State == DeliveryState.Results && simulation.Integrity <= 0;
            // StepLeft/StepRight are walking sprites, never motorcycle steering poses.
            rider.sprite = damaged ? trashedPose : moving ? ridingPose : idlePose;
            var alpha = simulation.IsInvulnerable && moving ? (Mathf.Sin(animationTime * 35) > 0 ? .4f : 1) : 1;
            var tint = Color.Lerp(Color.white, new Color(1, .58f, .4f), impact.Flash);
            tint.a = alpha;
            rider.color = bike.color = tint;
            bikeShadow.color = new Color(0, 0, 0, .42f * alpha);

            seen.Clear();
            foreach (var hazard in simulation.Hazards)
            {
                seen.Add(hazard.Id);
                if (!active.TryGetValue(hazard.Id, out var view))
                {
                    view = pool.Count > 0 ? pool.Dequeue() : Instantiate(hazardPrefab, hazardRoot);
                    view.gameObject.SetActive(true);
                    active.Add(hazard.Id, view);
                }
                view.Render(hazard, hazardSprites);
                view.transform.localScale = Vector3.one;
                // Both source cars face up. Reset pooled state so traffic stays parallel to the road.
                view.transform.localRotation = Quaternion.identity;
                view.transform.localPosition = new Vector3(DeliverySettings.LaneX(hazard.Lane) / 32, (hazard.Y + scrollOffset) / 32, 0);
            }
            released.Clear();
            foreach (var pair in active) if (!seen.Contains(pair.Key)) released.Add(pair.Key);
            foreach (var id in released)
            {
                var item = active[id];
                active.Remove(id);
                item.gameObject.SetActive(false);
                pool.Enqueue(item);
            }

            var stopY = simulation.NextStopY + scrollOffset;
            var showStop = simulation.State != DeliveryState.Ready && simulation.State != DeliveryState.Results && stopY < 410;
            deliveryMarker.gameObject.SetActive(showStop);
            customer.gameObject.SetActive(showStop);
            // At higher speed the stop can still be above the screen when its warning starts.
            var warning = moving && simulation.DistanceToNextStop <= simulation.BaseSpeed * warningSeconds;
            laneHighlight.gameObject.SetActive(warning);
            if (warning)
            {
                if (warningOrder != simulation.CurrentOrderIndex) { warningOrder = simulation.CurrentOrderIndex; warningTime = 0; }
                else warningTime += delta;
                DeliveryPulse = .5f + .5f * Mathf.Cos(warningTime * Mathf.PI * 6); // Three pulses per second.
                laneHighlight.transform.localPosition = new Vector3(DeliverySettings.LaneX(simulation.NextStopLane) / 32, 180f / 32, 0);
                laneHighlight.color = new Color(1, .76f, .27f, .09f + .18f * DeliveryPulse);
            }
            else { warningOrder = -1; warningTime = 0; DeliveryPulse = 1; }
            if (showStop)
            {
                deliveryMarker.transform.localPosition = new Vector3(135f / 32, stopY / 32, 0);
                customer.transform.localPosition = new Vector3((simulation.NextStopLane == 0 ? 25 : 245) / 32f, customerY / 32, 0);
            }
        }
    }
}
