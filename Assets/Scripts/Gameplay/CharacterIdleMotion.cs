using System.Linq;
using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Procedural animation shared by the chef and customers. This component
    /// lives below the facing transform, keeping locomotion, axis correction
    /// and visible posing independent.
    /// </summary>
    public sealed class CharacterIdleMotion : MonoBehaviour
    {
        public PlayerController player;
        public float idleBobHeight = 0.06f;
        public float idleSpeed = 2.2f;
        public float idleSwayAngle = 3.5f;
        public float breatheAmount = 0.025f;
        public float walkBobHeight = 0.12f;
        public float walkSpeed = 8.5f;
        public float walkLimbAngle = 40f;
        public float walkLeanAngle = 7f;
        public float reachLeanAngle = 32f;
        public float reachArmAngle = 68f;
        public float phaseOffset;

        public bool IsMoving { get; private set; }
        public bool IsPerformingAction => reachRemaining > 0f;
        public int ArmPivotCount => arms?.Length ?? 0;
        public int LegPivotCount => legs?.Length ?? 0;

        private Vector3 basePosition;
        private Vector3 baseScale;
        private Quaternion baseRotation;
        private Transform[] arms;
        private Transform[] legs;
        private Quaternion[] armRotations;
        private Quaternion[] legRotations;
        private Vector3 previousCarrierPosition;
        private float reachRemaining;
        private float reachDuration;

        private void Awake()
        {
            basePosition = transform.localPosition;
            baseScale = transform.localScale;
            baseRotation = transform.localRotation;
            if (phaseOffset <= 0f) phaseOffset = Random.Range(0f, Mathf.PI * 2f);

            arms = FindParts("ArmPivot_");
            legs = FindParts("LegPivot_");
            armRotations = arms.Select(part => part.localRotation).ToArray();
            legRotations = legs.Select(part => part.localRotation).ToArray();
            previousCarrierPosition = CarrierPosition;
        }

        private void LateUpdate()
        {
            var carrierPosition = CarrierPosition;
            var movedByRoute = (carrierPosition - previousCarrierPosition).sqrMagnitude > 0.000004f;
            previousCarrierPosition = carrierPosition;
            IsMoving = !IsPerformingAction && (player != null ? player.IsMoving : movedByRoute);

            if (IsPerformingAction)
            {
                reachRemaining = Mathf.Max(0f, reachRemaining - Time.deltaTime);
                var normalized = 1f - reachRemaining / Mathf.Max(0.01f, reachDuration);
                var reach = Mathf.Sin(normalized * Mathf.PI);
                transform.localPosition = basePosition + Vector3.forward * (0.22f * reach) + Vector3.down * (0.08f * reach);
                transform.localScale = baseScale;
                transform.localRotation = baseRotation * Quaternion.Euler(reachLeanAngle * reach, 0f, 0f);
                AnimatePairs(arms, armRotations, reachArmAngle * reach);
                AnimatePairs(legs, legRotations, -7f * reach);
                return;
            }

            var speed = IsMoving ? walkSpeed : idleSpeed;
            var wave = Mathf.Sin(Time.time * speed + phaseOffset);
            var bob = IsMoving ? Mathf.Abs(wave) * walkBobHeight : wave * idleBobHeight;
            var sway = IsMoving ? 0f : Mathf.Sin(Time.time * idleSpeed * 0.55f + phaseOffset) * idleSwayAngle;
            transform.localPosition = basePosition + Vector3.up * bob;
            transform.localScale = IsMoving
                ? baseScale
                : baseScale + new Vector3(-wave, wave, -wave) * breatheAmount;
            transform.localRotation = baseRotation * Quaternion.Euler(IsMoving ? walkLeanAngle : 0f, 0f, sway);

            var limbSwing = wave * (IsMoving ? walkLimbAngle : 5f);
            AnimatePairs(arms, armRotations, limbSwing);
            AnimatePairs(legs, legRotations, -limbSwing);
        }

        public void PlayReach(float duration)
        {
            reachDuration = Mathf.Max(0.2f, duration);
            reachRemaining = reachDuration;
        }

        private Vector3 CarrierPosition => transform.parent != null ? transform.parent.position : transform.position;

        private Transform[] FindParts(string prefix) => GetComponentsInChildren<Transform>(true)
            .Where(part => part != transform && part.name.StartsWith(prefix)).ToArray();

        private static void AnimatePairs(Transform[] parts, Quaternion[] bases, float angle)
        {
            for (var index = 0; index < parts.Length; index++)
                parts[index].localRotation = bases[index] * Quaternion.Euler(index % 2 == 0 ? angle : -angle, 0f, 0f);
        }
    }
}
