using CookAndDeliver.Delivery.Core;
using UnityEngine;

namespace CookAndDeliver.Delivery
{
    /// <summary>One logical hazard, with a lane-wide cluster for cones and barriers.</summary>
    public sealed class DeliveryHazardVisual : MonoBehaviour
    {
        public SpriteRenderer single;
        public SpriteRenderer[] cones;
        public SpriteRenderer[] barriers;
        private HazardKind visibleKind = (HazardKind)(-1);

        public void Render(DeliveryHazard hazard, Sprite[] sprites)
        {
            if (visibleKind != hazard.Kind)
            {
                var isCone = hazard.Kind == HazardKind.Cone;
                var isBarrier = hazard.Kind == HazardKind.Barrier;
                single.gameObject.SetActive(!isCone && !isBarrier);
                foreach (var piece in cones) piece.gameObject.SetActive(isCone);
                foreach (var piece in barriers) piece.gameObject.SetActive(isBarrier);
                visibleKind = hazard.Kind;
            }

            var color = hazard.Hit ? new Color(.7f, .7f, .7f, .7f) : Color.white;
            if (hazard.Kind == HazardKind.Cone)
            {
                foreach (var piece in cones) piece.color = color;
            }
            else if (hazard.Kind == HazardKind.Barrier)
            {
                foreach (var piece in barriers) piece.color = color;
            }
            else
            {
                single.sprite = sprites[(int)hazard.Kind];
                single.transform.localScale = Vector3.one * (hazard.IsTraffic ? 1.5f : 3f);
                single.color = color;
            }
        }
    }
}
