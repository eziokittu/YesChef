using UnityEngine;

namespace YesChef
{
    /// <summary>Fades optional looping ambience in as the chef approaches an exterior edge.</summary>
    public sealed class AmbientAudioZone : MonoBehaviour
    {
        public Transform listener;
        public AudioSource[] loops;
        public float fullVolumeDistance = 1.5f;
        public float audibleDistance = 7f;
        [Range(0f, 1f)] public float maximumVolume = 0.45f;

        private void Update()
        {
            if (listener == null && GameManager.Instance != null) listener = GameManager.Instance.player.transform;
            if (listener == null) return;
            var distance = Vector3.Distance(listener.position, transform.position);
            var volume = AudioDirector.Instance != null && AudioDirector.Instance.SfxEnabled
                ? maximumVolume * (1f - Mathf.InverseLerp(fullVolumeDistance, audibleDistance, distance)) : 0f;
            foreach (var source in loops)
            {
                if (source == null) continue;
                source.volume = Mathf.MoveTowards(source.volume, volume, Time.unscaledDeltaTime * 0.35f);
                if (source.clip != null && !source.isPlaying) source.Play();
            }
        }
    }
}
