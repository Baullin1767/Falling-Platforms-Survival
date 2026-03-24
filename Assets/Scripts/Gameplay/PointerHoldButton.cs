using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FallingPlatformsSurvival
{
    public sealed class PointerHoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Action pressedAction;
        private Action releasedAction;
        private bool isPressed;

        public void Bind(Action onPressed, Action onReleased = null)
        {
            pressedAction = onPressed;
            releasedAction = onReleased;
            isPressed = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            isPressed = true;
            pressedAction?.Invoke();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Release();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Release();
        }

        private void OnDisable()
        {
            Release();
        }

        private void Release()
        {
            if (!isPressed)
            {
                return;
            }

            isPressed = false;
            releasedAction?.Invoke();
        }
    }
}
