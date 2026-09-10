using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Keeps a world label subtle when the chef approaches and lets a station
    /// hide it completely while its surface is in use.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class WorldLabelFader : MonoBehaviour
    {
        public Transform player;
        public float fadeDistance = 2.4f;
        [Range(0f, 1f)] public float distantAlpha = 1f;
        [Range(0f, 1f)] public float nearbyAlpha = 0.16f;
        public float fadeSpeed = 5f;

        private CanvasGroup canvasGroup;
        private bool suppressed;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        private void Update()
        {
            if (player == null && GameManager.Instance != null && GameManager.Instance.player != null)
            {
                player = GameManager.Instance.player.transform;
            }

            var targetAlpha = distantAlpha;
            if (suppressed) targetAlpha = 0f;
            else if (player != null && Vector3.Distance(transform.position, player.position) <= fadeDistance)
                targetAlpha = nearbyAlpha;

            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, fadeSpeed * Time.unscaledDeltaTime);
        }

        public void SetSuppressed(bool value)
        {
            suppressed = value;
        }
    }
}
