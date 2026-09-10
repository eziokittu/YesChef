using UnityEngine;

namespace YesChef
{
    public sealed class WaterSurfaceAnimator : MonoBehaviour
    {
        public float waveHeight = 0.085f;
        public float waveSpeed = 1.2f;
        public float tiltAngle = 1.15f;
        private Vector3 origin;
        private Vector3 originalScale;
        private Quaternion originalRotation;
        private float phase;
        private void Awake()
        {
            origin = transform.localPosition;
            originalScale = transform.localScale;
            originalRotation = transform.localRotation;
            phase = origin.x * 0.55f + origin.z * 0.31f;
        }
        private void Update()
        {
            var wave = Mathf.Sin(Time.time * waveSpeed + phase);
            var crossWave = Mathf.Sin(Time.time * waveSpeed * .72f - phase * 1.7f);
            transform.localPosition = origin + Vector3.up * (wave * waveHeight);
            transform.localRotation = originalRotation * Quaternion.Euler(crossWave * tiltAngle, 0f, wave * tiltAngle);
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z * (1f + crossWave * .018f));
        }
    }
}
