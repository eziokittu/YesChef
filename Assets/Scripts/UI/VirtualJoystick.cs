using UnityEngine;
using UnityEngine.EventSystems;

namespace YesChef
{
    /// <summary>A floating-handle joystick that follows one continuous pointer drag.</summary>
    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public MobileInputController input;
        public RectTransform handle;
        [Min(1f)] public float movementRadius = 82f;
        [Range(0f, .9f)] public float deadZone = .12f;

        private RectTransform pad;
        private int activePointerId = int.MinValue;

        private void Awake()
        {
            pad = transform as RectTransform;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != int.MinValue) return;
            activePointerId = eventData.pointerId;
            ApplyPointer(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId == activePointerId) ApplyPointer(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId) return;
            activePointerId = int.MinValue;
            ResetJoystick();
        }

        private void OnDisable()
        {
            activePointerId = int.MinValue;
            ResetJoystick();
        }

        private void ApplyPointer(PointerEventData eventData)
        {
            if (pad == null) pad = transform as RectTransform;
            if (pad == null || input == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    pad, eventData.position, eventData.pressEventCamera, out var localPoint)) return;

            localPoint = Vector2.ClampMagnitude(localPoint, movementRadius);
            if (handle != null) handle.anchoredPosition = localPoint;

            var normalized = localPoint / movementRadius;
            var magnitude = normalized.magnitude;
            if (magnitude <= deadZone)
            {
                input.SetMovement(Vector2.zero);
                return;
            }

            var remappedMagnitude = Mathf.InverseLerp(deadZone, 1f, magnitude);
            input.SetMovement(normalized.normalized * remappedMagnitude);
        }

        private void ResetJoystick()
        {
            if (handle != null) handle.anchoredPosition = Vector2.zero;
            input?.SetMovement(Vector2.zero);
        }
    }
}
