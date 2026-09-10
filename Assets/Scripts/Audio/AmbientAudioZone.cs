using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Fades a quiet environmental bed in near an exterior edge, then schedules
    /// wildlife details independently so birds, insects and pond life do not
    /// repeat as an obvious synchronized loop.
    /// </summary>
    public sealed class AmbientAudioZone : MonoBehaviour
    {
        public Transform listener;
        [Header("Continuous beds")]
        public AudioSource[] continuousLoops = System.Array.Empty<AudioSource>();
        public float[] continuousRelativeVolumes = System.Array.Empty<float>();
        [Header("Randomized details")]
        public AudioSource[] randomOneShots = System.Array.Empty<AudioSource>();
        public float[] randomRelativeVolumes = System.Array.Empty<float>();
        public float[] minimumIntervals = System.Array.Empty<float>();
        public float[] maximumIntervals = System.Array.Empty<float>();
        public float fullVolumeDistance = 1.5f;
        public float audibleDistance = 7f;
        [Range(0f, 1f)] public float maximumVolume = 0.45f;

        private float[] nextPlayTimes;

        private void Awake() => ResetSchedules();

        private void OnEnable() => ResetSchedules();

        private void Update()
        {
            if (listener == null && GameManager.Instance != null) listener = GameManager.Instance.player.transform;
            if (listener == null) return;
            var distance = Vector3.Distance(listener.position, transform.position);
            var volume = AudioDirector.Instance != null && AudioDirector.Instance.SfxEnabled
                ? maximumVolume * (1f - Mathf.InverseLerp(fullVolumeDistance, audibleDistance, distance)) : 0f;
            for (var index = 0; index < continuousLoops.Length; index++)
            {
                var source = continuousLoops[index];
                if (source == null) continue;
                var mix = ValueAt(continuousRelativeVolumes, index, 1f);
                source.volume = Mathf.MoveTowards(source.volume, volume * mix, Time.unscaledDeltaTime * 0.35f);
                if (source.clip != null && !source.isPlaying) source.Play();
            }

            EnsureSchedules();
            for (var index = 0; index < randomOneShots.Length; index++)
            {
                var source = randomOneShots[index];
                if (source == null) continue;
                source.volume = Mathf.MoveTowards(source.volume,
                    volume * ValueAt(randomRelativeVolumes, index, 1f), Time.unscaledDeltaTime * 0.5f);
                if (volume <= 0.005f || source.clip == null || Time.unscaledTime < nextPlayTimes[index]) continue;

                source.pitch = Random.Range(0.94f, 1.06f);
                source.panStereo = Random.Range(-0.28f, 0.28f);
                source.Play();
                ScheduleNext(index);
            }
        }

        private void ResetSchedules()
        {
            nextPlayTimes = new float[randomOneShots?.Length ?? 0];
            for (var index = 0; index < nextPlayTimes.Length; index++) ScheduleNext(index, true);
        }

        private void EnsureSchedules()
        {
            if (nextPlayTimes == null || nextPlayTimes.Length != (randomOneShots?.Length ?? 0)) ResetSchedules();
        }

        private void ScheduleNext(int index, bool initial = false)
        {
            var minimum = Mathf.Max(0.5f, ValueAt(minimumIntervals, index, 5f));
            var maximum = Mathf.Max(minimum, ValueAt(maximumIntervals, index, minimum + 6f));
            var delay = Random.Range(minimum, maximum);
            if (initial) delay *= Random.Range(0.35f, 1f);
            nextPlayTimes[index] = Time.unscaledTime + delay;
        }

        private static float ValueAt(float[] values, int index, float fallback) =>
            values != null && index < values.Length ? values[index] : fallback;
    }
}
