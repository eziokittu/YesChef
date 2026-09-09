using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Adds a small breathing bob to a model child. Route and movement rotation
    /// remain on the parent, so this animation cannot tilt the character over.
    /// </summary>
    public sealed class CharacterIdleMotion : MonoBehaviour
    {
        public PlayerController player;
        public float bobHeight = 0.035f;
        public float bobSpeed = 2.2f;
        public float breatheAmount = 0.018f;
        public float phaseOffset;

        private Vector3 basePosition;
        private Vector3 baseScale;

        private void Awake()
        {
            basePosition = transform.localPosition;
            baseScale = transform.localScale;
            if (phaseOffset <= 0f) phaseOffset = Random.Range(0f, Mathf.PI * 2f);
        }

        private void LateUpdate()
        {
            var amount = player != null && player.IsMoving ? 0.25f : 1f;
            var wave = Mathf.Sin(Time.time * bobSpeed + phaseOffset);
            transform.localPosition = basePosition + Vector3.up * (wave * bobHeight * amount);
            transform.localScale = baseScale + new Vector3(-wave, wave, -wave) * (breatheAmount * amount);
        }
    }
}
