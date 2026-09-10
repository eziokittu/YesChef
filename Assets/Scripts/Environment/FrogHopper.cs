using UnityEngine;

namespace YesChef
{
    public sealed class FrogHopper : MonoBehaviour
    {
        public Vector3[] landingPoints;
        public float hopDuration = 1.15f;
        public Vector2 restRange = new(1.8f, 4.2f);
        public float hopHeight = 0.8f;
        private int targetIndex;
        private float rest;
        private float progress = 1f;
        private Vector3 start;
        private void Start() => rest = Random.Range(restRange.x, restRange.y);
        private void Update()
        {
            if (landingPoints == null || landingPoints.Length < 2) return;
            if (progress >= 1f)
            {
                rest -= Time.deltaTime;
                if (rest > 0f) return;
                start = transform.position;
                targetIndex = (targetIndex + 1) % landingPoints.Length;
                progress = 0f;
            }
            progress = Mathf.Min(1f, progress + Time.deltaTime / hopDuration);
            var target = landingPoints[targetIndex];
            transform.position = Vector3.Lerp(start, target, progress) + Vector3.up * (Mathf.Sin(progress * Mathf.PI) * hopHeight);
            var direction = target - start; direction.y = 0f;
            if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction);
            if (progress >= 1f) rest = Random.Range(restRange.x, restRange.y);
        }
    }
}
