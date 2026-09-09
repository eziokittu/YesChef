using UnityEngine;

namespace YesChef
{
    /// <summary>Moves ambient fish/snakes between two points and optionally hides rare wildlife between appearances.</summary>
    public sealed class WildlifeMover : MonoBehaviour
    {
        public Vector3 pointA;
        public Vector3 pointB;
        public float speed = 0.8f;
        public bool rareAppearance;
        public Vector2 rareDelayRange = new(14f, 28f);

        private Vector3 target;
        private float hiddenSeconds;

        private void Start()
        {
            target = pointB;
            if (rareAppearance) HideForRandomDelay();
        }

        private void Update()
        {
            if (hiddenSeconds > 0f)
            {
                hiddenSeconds -= Time.deltaTime;
                if (hiddenSeconds <= 0f) SetVisible(true);
                return;
            }

            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, target) > 0.05f) return;

            if (rareAppearance)
            {
                transform.position = pointA;
                target = pointB;
                HideForRandomDelay();
            }
            else
            {
                target = target == pointA ? pointB : pointA;
                transform.rotation *= Quaternion.Euler(0f, 180f, 0f);
            }
        }

        private void HideForRandomDelay()
        {
            hiddenSeconds = Random.Range(rareDelayRange.x, rareDelayRange.y);
            SetVisible(false);
        }

        private void SetVisible(bool visible)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>()) renderer.enabled = visible;
        }
    }
}
