using UnityEngine;

namespace YesChef
{
    public sealed class WaterSurfaceAnimator : MonoBehaviour
    {
        public float waveHeight = 0.11f;
        public float waveSpeed = 1.2f;
        public float tiltAngle = 1.6f;
        [Range(0f, .25f)] public float colorShift = .08f;
        private Vector3 origin;
        private Vector3 originalScale;
        private Quaternion originalRotation;
        private float phase;
        private Renderer surfaceRenderer;
        private MaterialPropertyBlock propertyBlock;
        private Color baseColor = Color.white;
        private void Awake()
        {
            origin = transform.localPosition;
            originalScale = transform.localScale;
            originalRotation = transform.localRotation;
            phase = origin.x * 0.55f + origin.z * 0.31f;
            surfaceRenderer = GetComponent<Renderer>();
            if (surfaceRenderer != null && surfaceRenderer.sharedMaterial != null)
            {
                propertyBlock = new MaterialPropertyBlock();
                baseColor = surfaceRenderer.sharedMaterial.color;
            }
        }
        private void Update()
        {
            var wave = Mathf.Sin(Time.time * waveSpeed + phase);
            var crossWave = Mathf.Sin(Time.time * waveSpeed * .72f - phase * 1.7f);
            transform.localPosition = origin + Vector3.up * (wave * waveHeight);
            transform.localRotation = originalRotation * Quaternion.Euler(crossWave * tiltAngle, 0f, wave * tiltAngle);
            transform.localScale = new Vector3(originalScale.x, originalScale.y, originalScale.z * (1f + crossWave * .018f));
            if (surfaceRenderer != null && propertyBlock != null)
            {
                var shimmer = 1f + wave * colorShift;
                propertyBlock.SetColor("_Color", new Color(baseColor.r * shimmer, baseColor.g * shimmer, baseColor.b * shimmer, baseColor.a));
                surfaceRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
