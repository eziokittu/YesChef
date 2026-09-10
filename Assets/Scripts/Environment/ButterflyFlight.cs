using UnityEngine;

namespace YesChef
{
    public sealed class ButterflyFlight : MonoBehaviour
    {
        public Vector3 centre;
        public Vector3 radii = new(2f, 0.35f, 1.2f);
        public float speed = 0.55f;
        public Transform leftWing;
        public Transform rightWing;
        private float phase;
        private void Start() => phase = Random.Range(0f, Mathf.PI * 2f);
        private void Update()
        {
            var t = Time.time * speed + phase;
            var next = centre + new Vector3(Mathf.Sin(t) * radii.x,
                0.65f + Mathf.Sin(t * 2.1f) * radii.y, Mathf.Sin(t * 1.7f + 0.8f) * radii.z);
            var velocity = next - transform.position;
            transform.position = Vector3.Lerp(transform.position, next, Time.deltaTime * 2.4f);
            if (velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(velocity), Time.deltaTime * 5f);
            var flap = Mathf.Sin(Time.time * 18f) * 55f;
            if (leftWing != null) leftWing.localRotation = Quaternion.Euler(0f, flap, 0f);
            if (rightWing != null) rightWing.localRotation = Quaternion.Euler(0f, -flap, 0f);
        }
    }
}
