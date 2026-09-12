using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Smoothly swings the Blender-authored refrigerator door and fades its
    /// interior light with the catalogue state.
    /// </summary>
    public sealed class RefrigeratorAnimator : MonoBehaviour
    {
        public Transform doorHinge;
        public Light interiorLight;
        public float openAngle = 105f;
        public float animationSpeed = 4.8f;

        private Quaternion closedRotation;
        private float openness;
        private bool shouldOpen;
        private bool lastRequestedOpen;

        public float OpenAmount => openness;
        public bool IsPickupReady => shouldOpen && openness >= 0.78f;

        private void Awake()
        {
            if (doorHinge != null) closedRotation = doorHinge.localRotation;
            if (interiorLight != null) interiorLight.intensity = 0f;
        }

        private void Update()
        {
            openness = Mathf.MoveTowards(openness, shouldOpen ? 1f : 0f, animationSpeed * Time.unscaledDeltaTime);
            var eased = Mathf.SmoothStep(0f, 1f, openness);
            if (doorHinge != null)
                doorHinge.localRotation = closedRotation * Quaternion.AngleAxis(openAngle * eased, Vector3.forward);
            if (interiorLight != null)
            {
                interiorLight.enabled = openness > 0.001f;
                interiorLight.intensity = eased;
            }
        }

        public void SetOpen(bool value)
        {
            if (value != lastRequestedOpen)
            {
                if (value) AudioDirector.Instance?.PlayFridgeOpen();
                else AudioDirector.Instance?.PlayFridgeClose();
                lastRequestedOpen = value;
            }
            shouldOpen = value;
        }
    }
}
