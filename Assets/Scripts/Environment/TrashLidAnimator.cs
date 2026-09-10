using UnityEngine;

namespace YesChef
{
    public sealed class TrashLidAnimator : MonoBehaviour
    {
        public Transform lid;
        public float openAngle = 72f;
        public float openSeconds = 0.55f;
        public float speed = 7f;
        private Quaternion closedRotation;
        private float remaining;

        private void Awake() { if (lid != null) closedRotation = lid.localRotation; }
        private void Update()
        {
            remaining = Mathf.Max(0f, remaining - Time.deltaTime);
            if (lid != null) lid.localRotation = Quaternion.Slerp(lid.localRotation,
                closedRotation * Quaternion.Euler(openAngle * (remaining > 0f ? 1f : 0f), 0f, 0f), speed * Time.deltaTime);
        }
        public void OpenOnce() => remaining = openSeconds;
    }
}
