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
        [Range(20f, 80f)] public float movingFieldOfView = 52f;
        [Range(20f, 80f)] public float idleFieldOfView = 42f;
        [Min(0f)] public float idleDelay = 3f;
        [Min(0.05f)] public float zoomSmoothTime = 1.8f;
        [Header("Idle edge reveals")]
        public float marshRevealScreenX = 0.35f;
        public float customerRevealScreenX = 0.62f;
        public float gardenRevealScreenY = 0.61f;
        public float horizontalEdgeThreshold = 3.8f;
        public float gardenEdgeThreshold = -3.25f;
        public float idleRevealFieldOfView = 58f;

        private float idleSeconds;
        private float zoomVelocity;

        private void Update()
        {
            if (virtualCamera == null || player == null) return;

            idleSeconds = player.IsMoving ? 0f : idleSeconds + Time.unscaledDeltaTime;
            var targetFieldOfView = idleSeconds >= idleDelay ? idleFieldOfView : movingFieldOfView;
            var revealReady = idleSeconds >= idleDelay;
            var revealMarsh = revealReady && player.transform.position.x >= horizontalEdgeThreshold;
            var revealCustomers = revealReady && player.transform.position.x <= -horizontalEdgeThreshold;
            var revealGarden = revealReady && player.transform.position.z <= gardenEdgeThreshold;
            if (revealMarsh || revealCustomers || revealGarden) targetFieldOfView = idleRevealFieldOfView;

            var lens = virtualCamera.m_Lens;
            lens.FieldOfView = Mathf.SmoothDamp(
                lens.FieldOfView,
                targetFieldOfView,
                ref zoomVelocity,
                zoomSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime);
            virtualCamera.m_Lens = lens;

            var framing = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (framing != null)
            {
                var targetScreenX = revealMarsh ? marshRevealScreenX : revealCustomers ? customerRevealScreenX : 0.5f;
                var targetScreenY = revealGarden ? gardenRevealScreenY : 0.48f;
                framing.m_ScreenX = Mathf.MoveTowards(framing.m_ScreenX, targetScreenX, Time.unscaledDeltaTime * 0.12f);
                framing.m_ScreenY = Mathf.MoveTowards(framing.m_ScreenY, targetScreenY, Time.unscaledDeltaTime * 0.12f);
            }
        }

        public void ResetZoom()
        {
            idleSeconds = 0f;
            zoomVelocity = 0f;
            if (virtualCamera == null) return;
            var lens = virtualCamera.m_Lens;
            lens.FieldOfView = movingFieldOfView;
            virtualCamera.m_Lens = lens;
            var framing = virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
            if (framing != null)
            {
                framing.m_ScreenX = 0.5f;
                framing.m_ScreenY = 0.48f;
            }
        }
    }
}
