using UnityEngine;
using UnityEngine.EventSystems;

namespace YesChef
{
    /// <summary>Press-and-hold pointer surface for one movement direction or action.</summary>
    public sealed class TouchControlButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public MobileInputController input;
        public MobileDirection direction;
        public bool isAction;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (isAction) input?.PressAction();
            else input?.SetDirection(direction, true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isAction) input?.SetDirection(direction, false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isAction) input?.SetDirection(direction, false);
        }

        private void OnDisable()
        {
            if (!isAction) input?.SetDirection(direction, false);
        }
    }
}
