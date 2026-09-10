using System.Linq;
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
        private Transform[] arms;
        private Transform[] legs;
        private Quaternion[] armRotations;
        private Quaternion[] legRotations;

        private void Awake()
        {
            basePosition = transform.localPosition;
            baseScale = transform.localScale;
            if (phaseOffset <= 0f) phaseOffset = Random.Range(0f, Mathf.PI * 2f);
            arms = FindParts("Arm_");
            legs = FindParts("Leg_").Concat(FindParts("PantsLeg_")).ToArray();
            armRotations = arms.Select(part => part.localRotation).ToArray();
            legRotations = legs.Select(part => part.localRotation).ToArray();
        }

        private void LateUpdate()
        {
            var amount = player != null && player.IsMoving ? 0.25f : 1f;
            var wave = Mathf.Sin(Time.time * bobSpeed + phaseOffset);
            transform.localPosition = basePosition + Vector3.up * (wave * bobHeight * amount);
            transform.localScale = baseScale + new Vector3(-wave, wave, -wave) * (breatheAmount * amount);

            var walking = player != null && player.IsMoving;
            var swing = Mathf.Sin(Time.time * (walking ? 9f : 1.7f) + phaseOffset) * (walking ? 22f : 4f);
            AnimatePairs(arms, armRotations, swing);
            AnimatePairs(legs, legRotations, -swing);
        }

        private Transform[] FindParts(string prefix) => GetComponentsInChildren<Transform>(true)
            .Where(part => part != transform && part.name.Contains(prefix)).ToArray();

        private static void AnimatePairs(Transform[] parts, Quaternion[] bases, float angle)
        {
            for (var index = 0; index < parts.Length; index++)
                parts[index].localRotation = bases[index] * Quaternion.Euler(0f, index % 2 == 0 ? angle : -angle, 0f);
        }
    }
}
