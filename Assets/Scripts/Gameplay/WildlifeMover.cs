using UnityEngine;

namespace YesChef
{
    /// <summary>Moves ambient fish/snakes between two points and optionally hides rare wildlife between appearances.</summary>
    public sealed class WildlifeMover : MonoBehaviour
    {
        public enum MotionStyle { Swim, FishLeap, SnakeVisit }
        public Vector3 pointA;
        public Vector3 pointB;
        public float speed = 0.8f;
        public bool rareAppearance;
        public Vector2 rareDelayRange = new(14f, 28f);
        public MotionStyle motionStyle;
        public float arcHeight = 0.45f;
        public float turnSmoothness = 5f;
        public AudioSource eventAudioSource;
        public AudioClip movementEventClip;

        private Vector3 target;
        private float hiddenSeconds;
        private Vector3 start;
        private float journeyLength;
        private float travelled;
        private float hissSeconds;
        private bool completedRareVisit;

        private void Start()
        {
            target = pointB;
            start = pointA;
            journeyLength = Vector3.Distance(pointA, pointB);
            if (rareAppearance) HideForRandomDelay();
        }

        private void Update()
        {
            if (completedRareVisit) return;
            if (hiddenSeconds > 0f)
            {
                hiddenSeconds -= Time.deltaTime;
                if (hiddenSeconds <= 0f) SetVisible(true);
                return;
            }

            var direction = target - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.002f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), turnSmoothness * Time.deltaTime);

            travelled += speed * Time.deltaTime;
            var normalized = journeyLength <= 0.001f ? 1f : Mathf.Clamp01(travelled / journeyLength);
            var next = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            var ripple = Mathf.Sin(Time.time * 4.5f) * 0.025f;
            next.y = Mathf.Lerp(start.y, target.y, normalized) + ripple;
            if (motionStyle == MotionStyle.FishLeap)
            {
                // One smooth breach in the middle of each route, not jitter.
                var leapWindow = Mathf.InverseLerp(0.30f, 0.70f, normalized);
                next.y += Mathf.Sin(Mathf.Clamp01(leapWindow) * Mathf.PI) * arcHeight;
                if (normalized >= .30f && normalized - speed * Time.deltaTime / Mathf.Max(.01f, journeyLength) < .30f)
                    AudioDirector.Instance?.Play(movementEventClip, eventAudioSource);
            }
            else if (motionStyle == MotionStyle.SnakeVisit && normalized > 0.72f && normalized < 0.86f)
            {
                hissSeconds += Time.deltaTime;
                next.y += Mathf.Sin(hissSeconds * 13f) * 0.04f;
                if (hissSeconds <= Time.deltaTime * 1.5f) AudioDirector.Instance?.Play(movementEventClip, eventAudioSource);
            }
            transform.position = next;
            if (Vector3.Distance(transform.position, target) > 0.05f) return;

            if (rareAppearance)
            {
                completedRareVisit = true;
                SetVisible(false);
            }
            else
            {
                target = target == pointA ? pointB : pointA;
                ResetJourney(transform.position);
            }
        }

        private void ResetJourney(Vector3 from)
        {
            start = from;
            travelled = 0f;
            journeyLength = Vector3.Distance(from, target);
            hissSeconds = 0f;
        }

        private void HideForRandomDelay()
        {
            if (completedRareVisit) return;
            hiddenSeconds = Random.Range(rareDelayRange.x, rareDelayRange.y);
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = visible;
        }
    }
}
