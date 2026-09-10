using UnityEngine;

namespace YesChef
{
    /// <summary>
    /// Persistent two-track music playlist and central SFX routing point. All
    /// clips are intentionally optional so final audio can be dropped into the
    /// Inspector without changing gameplay code.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        private const string MusicEnabledKey = "YesChef.MusicEnabled";
        private const string SfxEnabledKey = "YesChef.SfxEnabled";
        public static AudioDirector Instance { get; private set; }

        [Header("Music playlist")]
        public AudioSource musicSource;
        public AudioClip[] musicTracks = new AudioClip[2];
        [Range(0f, 1f)] public float musicVolume = 0.72f;

        [Header("Kitchen sound effects")]
        public AudioSource sfxSource;
        [Range(0f, 1f)] public float sfxVolume = 0.22f;
        public AudioClip chopping;
        public AudioClip cooking;
        public AudioClip fridgeOpen;
        public AudioClip fridgeClose;
        public AudioClip foodPrepared;
        public AudioClip newOrder;
        public AudioClip orderItemReceived;
        public AudioClip customerArrival;
        public AudioClip customerHappy;
        public AudioClip trash;

        public bool MusicEnabled { get; private set; } = true;
        public bool SfxEnabled { get; private set; } = true;

        private int trackIndex;
        private float musicDisabledAt = -1f;
        private AudioListener activeListener;
        private bool listenerReadyLogged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioListener();
            MusicEnabled = PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
            SfxEnabled = PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
            if (musicSource != null)
            {
                musicSource.loop = false;
                musicSource.playOnAwake = false;
                musicSource.ignoreListenerPause = true;
                musicSource.spatialBlend = 0f;
                musicSource.mute = false;
                musicSource.priority = 0;
                musicSource.volume = musicVolume;
            }
            if (sfxSource != null)
            {
                sfxSource.ignoreListenerPause = true;
                sfxSource.spatialBlend = 0f;
                sfxSource.volume = sfxVolume;
            }
        }

        private void Start() => EnsureMusicPlaying();

        private void Update()
        {
            EnsureAudioListener();
            if (!MusicEnabled || musicSource == null || musicSource.isPlaying) return;
            if (HasAnyMusic())
            {
                trackIndex = NextValidTrack(trackIndex + 1);
                PlayCurrentTrack(false);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) return;
            EnsureAudioListener();
            EnsureMusicPlaying();
        }

        private void EnsureAudioListener()
        {
            if (activeListener != null && activeListener.enabled && activeListener.gameObject.activeInHierarchy) return;

            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var mainCamera = Camera.main;
            AudioListener listener = null;
            if (mainCamera != null)
            {
                listener = mainCamera.GetComponent<AudioListener>();
                if (listener == null) listener = mainCamera.gameObject.AddComponent<AudioListener>();
            }
            else
            {
                foreach (var candidate in listeners)
                {
                    if (candidate != null && candidate.gameObject.activeInHierarchy)
                    {
                        listener = candidate;
                        break;
                    }
                }
                if (listener == null) listener = gameObject.AddComponent<AudioListener>();
            }

            listener.enabled = true;
            activeListener = listener;
            foreach (var candidate in listeners)
            {
                if (candidate != null && candidate != activeListener) candidate.enabled = false;
            }

            if (listenerReadyLogged) return;
            listenerReadyLogged = true;
            Debug.Log($"YES_CHEF_AUDIO_LISTENER_READY: host={activeListener.gameObject.name}, active={activeListener.enabled && activeListener.gameObject.activeInHierarchy}");
        }

        public void EnsureMusicPlaying()
        {
            if (!MusicEnabled || musicSource == null || musicSource.isPlaying || !HasAnyMusic()) return;
            musicSource.enabled = true;
            musicSource.mute = false;
            musicSource.volume = musicVolume;
            PlayCurrentTrack(false);
        }

        public void SetMusicEnabled(bool enabled)
        {
            if (MusicEnabled == enabled) return;
            MusicEnabled = enabled;
            PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
            if (!enabled)
            {
                musicDisabledAt = Time.realtimeSinceStartup;
                musicSource?.Pause();
                return;
            }

            var resume = musicDisabledAt >= 0f && Time.realtimeSinceStartup - musicDisabledAt <= 3f;
            if (resume && musicSource != null && musicSource.clip != null) musicSource.UnPause();
            else PlayCurrentTrack(true);
        }

        public void SetSfxEnabled(bool enabled)
        {
            SfxEnabled = enabled;
            PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void PlayChop() => Play(chopping);
        public void PlayCooking() => Play(cooking);
        public void PlayFridgeOpen() => Play(fridgeOpen);
        public void PlayFridgeClose() => Play(fridgeClose);
        public void PlayPrepared() => Play(foodPrepared);
        public void PlayNewOrder() => Play(newOrder);
        public void PlayOrderItemReceived() => Play(orderItemReceived);
        public void PlayCustomerArrival() => Play(customerArrival);
        public void PlayCustomerHappy() => Play(customerHappy);
        public void PlayTrash() => Play(trash);

        public void Play(AudioClip clip, AudioSource source = null)
        {
            if (!SfxEnabled || clip == null) return;
            (source != null ? source : sfxSource)?.PlayOneShot(clip);
        }

        private bool HasAnyMusic()
        {
            if (musicTracks == null) return false;
            foreach (var track in musicTracks) if (track != null) return true;
            return false;
        }

        private int NextValidTrack(int start)
        {
            if (musicTracks == null || musicTracks.Length == 0) return 0;
            for (var offset = 0; offset < musicTracks.Length; offset++)
            {
                var index = (start + offset) % musicTracks.Length;
                if (musicTracks[index] != null) return index;
            }
            return 0;
        }

        private void PlayCurrentTrack(bool restart)
        {
            if (!MusicEnabled || musicSource == null || !HasAnyMusic()) return;
            trackIndex = NextValidTrack(trackIndex);
            musicSource.clip = musicTracks[trackIndex];
            if (restart) musicSource.time = 0f;
            musicSource.Play();
        }
    }
}
