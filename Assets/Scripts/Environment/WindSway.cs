using UnityEngine;

namespace YesChef
{
    public sealed class WindSway : MonoBehaviour
    {
        public float angle = 2.5f;
        public float speed = 1.15f;
        public float phase;
        private Quaternion rest;
        private void Awake() { rest = transform.localRotation; if (phase == 0f) phase = Random.Range(0f, 9f); }
        private void LateUpdate()
        {
            var gust = Mathf.Sin(Time.time * speed + phase) + Mathf.Sin(Time.time * speed * 0.37f + phase) * 0.35f;
            transform.localRotation = rest * Quaternion.Euler(angle * gust, 0f, angle * 0.45f * gust);
        }
    }
}
