using UnityEngine;

namespace YesChef
{
    /// <summary>Plays a short, readable overshoot when dialogue first appears.</summary>
    public sealed class DialogueBubbleAnimator : MonoBehaviour
    {
        [Min(.1f)] public float duration = .38f;
        [Range(.2f, 1f)] public float startingScale = .72f;

        private Vector3 restingScale;
        private float elapsed = float.PositiveInfinity;

        private void Awake()
        {
            restingScale = transform.localScale;
        }

        public void PlayBounce()
        {
            if (restingScale == Vector3.zero) restingScale = transform.localScale;
            elapsed = 0f;
            transform.localScale = restingScale * startingScale;
        }

        private void Update()
        {
            if (elapsed >= duration) return;
            elapsed += Time.unscaledDeltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            const float overshoot = 1.70158f;
            var shifted = t - 1f;
            var eased = 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
            transform.localScale = restingScale * Mathf.LerpUnclamped(startingScale, 1f, eased);
            if (t >= 1f) transform.localScale = restingScale;
        }
    }
}
