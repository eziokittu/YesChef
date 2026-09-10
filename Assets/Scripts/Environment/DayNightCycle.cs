using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Compresses a readable day/night/day lighting story into one three-minute service.
    /// Exterior and interior groups cross-fade independently so each window keeps its own light cone.
    /// </summary>
    public sealed class DayNightCycle : MonoBehaviour
    {
        [Header("Timeline (seconds from service start)")]
        public float sunsetStarts = 50f;
        public float nightStarts = 70f;
        public float sunriseStarts = 110f;
        public float morningReturns = 130f;

        [Header("Light groups")]
        public Light globalMorningLight;
        public Light[] exteriorWindowLights;
        public Light[] interiorWindowSpillLights;
        public Light[] kitchenLights;

        [Header("Intensity")]
        public float globalMorningIntensity = 1.65f;
        public float exteriorWindowIntensity = 2.8f;
        public float interiorWindowSpillIntensity = 1.65f;
        public float kitchenDayIntensity = 0.22f;
        public float kitchenNightIntensity = 2.25f;
        public Color dayAmbient = new(0.58f, 0.62f, 0.56f);
        public Color nightAmbient = new(0.075f, 0.09f, 0.13f);

        public float ElapsedSeconds { get; private set; }
        public float DaylightAmount { get; private set; } = 1f;

        private void Awake() => ResetCycle();

        private void Update()
        {
            if (GameManager.Instance == null || GameManager.Instance.Phase != GamePhase.Playing) return;
            ElapsedSeconds += Time.deltaTime;
            ApplyLighting(ElapsedSeconds);
        }

        public void ResetCycle()
        {
            ElapsedSeconds = 0f;
            ApplyLighting(0f);
        }

        public void ApplyLighting(float elapsedSeconds)
        {
            ElapsedSeconds = Mathf.Max(0f, elapsedSeconds);
            DaylightAmount = CalculateDaylight(ElapsedSeconds);
            var interiorAmount = 1f - DaylightAmount;

            if (globalMorningLight != null)
            {
                globalMorningLight.enabled = DaylightAmount > 0.001f;
                globalMorningLight.intensity = globalMorningIntensity * DaylightAmount;
            }

            if (exteriorWindowLights != null)
            {
                foreach (var windowLight in exteriorWindowLights)
                {
                    if (windowLight == null) continue;
                    windowLight.enabled = DaylightAmount > 0.001f;
                    windowLight.intensity = exteriorWindowIntensity * DaylightAmount;
                }
            }

            // These four cones originate just inside each physical window and
            // point out into the garden/marsh. They are independent from the
            // daylight cones, so night reads as a lit kitchen spilling warmth
            // through four distinct panes instead of one combined floodlight.
            if (interiorWindowSpillLights != null)
            {
                foreach (var windowLight in interiorWindowSpillLights)
                {
                    if (windowLight == null) continue;
                    windowLight.enabled = interiorAmount > 0.001f;
                    windowLight.intensity = interiorWindowSpillIntensity * interiorAmount;
                }
            }

            if (kitchenLights != null)
            {
                for (var index = 0; index < kitchenLights.Length; index++)
                {
                    var kitchenLight = kitchenLights[index];
                    if (kitchenLight == null) continue;
                    var stagger = kitchenLights.Length <= 1 ? 0f : index / (float)(kitchenLights.Length - 1) * 0.72f;
                    var switchedOn = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(stagger, stagger + 0.28f, interiorAmount));
                    var flickerWindow = 1f - Mathf.Abs(switchedOn * 2f - 1f);
                    var flicker = index % 2 == 0
                        ? 1f
                        : Mathf.Lerp(1f, 0.72f + Mathf.PerlinNoise(index * 3.17f, ElapsedSeconds * 8f) * 0.28f, flickerWindow);
                    kitchenLight.enabled = true;
                    kitchenLight.intensity = Mathf.Lerp(kitchenDayIntensity, kitchenNightIntensity, switchedOn) * flicker;
                }
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.Lerp(nightAmbient, dayAmbient, DaylightAmount);
        }

        private float CalculateDaylight(float elapsedSeconds)
        {
            if (elapsedSeconds < sunsetStarts) return 1f;
            if (elapsedSeconds < nightStarts) return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(sunsetStarts, nightStarts, elapsedSeconds));
            if (elapsedSeconds < sunriseStarts) return 0f;
            if (elapsedSeconds < morningReturns) return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(sunriseStarts, morningReturns, elapsedSeconds));
            return 1f;
        }
    }
}
