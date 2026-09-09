using Cinemachine;
using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Keeps the Cinemachine camera wide while the chef moves and gradually
    /// narrows the perspective field of view after the chef becomes idle.
    /// </summary>
    public sealed class AdaptiveCinemachineCamera : MonoBehaviour
    {
        public CinemachineVirtualCamera virtualCamera;
        public PlayerController player;

        [Header("Perspective zoom")]
        [Range(20f, 80f)] public float movingFieldOfView = 48f;
        [Range(20f, 80f)] public float idleFieldOfView = 36f;
        [Min(0f)] public float idleDelay = 1.25f;
        [Min(0.05f)] public float zoomSmoothTime = 0.8f;

        private float idleSeconds;
        private float zoomVelocity;

        private void Update()
        {
            if (virtualCamera == null || player == null) return;

            idleSeconds = player.IsMoving ? 0f : idleSeconds + Time.unscaledDeltaTime;
            var targetFieldOfView = idleSeconds >= idleDelay ? idleFieldOfView : movingFieldOfView;

            var lens = virtualCamera.m_Lens;
            lens.FieldOfView = Mathf.SmoothDamp(
                lens.FieldOfView,
                targetFieldOfView,
                ref zoomVelocity,
                zoomSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            virtualCamera.m_Lens = lens;
        }

        public void ResetZoom()
        {
            idleSeconds = 0f;
            zoomVelocity = 0f;
            if (virtualCamera == null) return;
            var lens = virtualCamera.m_Lens;
            lens.FieldOfView = movingFieldOfView;
            virtualCamera.m_Lens = lens;
        }
    }
}
