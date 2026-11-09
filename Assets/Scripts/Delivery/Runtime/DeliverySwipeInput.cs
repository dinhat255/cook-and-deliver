using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CookAndDeliver.Delivery
{
    public sealed class DeliverySwipeInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IInitializePotentialDragHandler
    {
        public float widthFraction = .06f;
        public float horizontalRatio = 1.25f;
        public event Action<int> Swiped;
        private int? pointer;
        private Vector2 start;
        private bool consumed;
        public bool Enabled { get; set; }

        public void OnInitializePotentialDrag(PointerEventData data) { data.useDragThreshold = false; }
        public void OnPointerDown(PointerEventData data)
        {
            if (!Enabled || pointer.HasValue || data.button != PointerEventData.InputButton.Left) return;
            pointer = data.pointerId;
            start = data.position;
            consumed = false;
        }
        public void OnDrag(PointerEventData data)
        {
            if (!Enabled || pointer != data.pointerId || consumed) return;
            var delta = data.position - start;
            var corners = new Vector3[4];
            ((RectTransform)transform).GetWorldCorners(corners);
            var canvas = GetComponentInParent<Canvas>();
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var width = Vector2.Distance(RectTransformUtility.WorldToScreenPoint(cam, corners[0]), RectTransformUtility.WorldToScreenPoint(cam, corners[3]));
            if (Mathf.Abs(delta.x) < width * widthFraction || Mathf.Abs(delta.x) < Mathf.Abs(delta.y) * horizontalRatio) return;
            consumed = true;
            Swiped?.Invoke(delta.x < 0 ? -1 : 1);
        }
        public void OnPointerUp(PointerEventData data) { if (pointer == data.pointerId) Cancel(); }
        public void Cancel() { pointer = null; consumed = false; }
        private void OnDisable() { Cancel(); }
        private void OnApplicationFocus(bool focus) { if (!focus) Cancel(); }
    }
}
