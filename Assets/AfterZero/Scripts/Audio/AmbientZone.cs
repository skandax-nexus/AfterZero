using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Looping local ambience (rain on awnings, crowd, clockwork, tram hum, flood water, future drone).
    /// Optionally only audible in certain states, cross-fading on shift.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AmbientZone : MonoBehaviour
    {
        public AmbientKind kind = AmbientKind.Crowd;
        [Range(0f, 1f)] public float volume = 0.35f;
        public TimeMask audibleIn = TimeMask.All;
        public float minDistance = 4f;
        public float maxDistance = 30f;
        [Tooltip("0 = everywhere (2D), 1 = fully positional.")]
        [Range(0f, 1f)] public float spatial = 1f;
        public float pitch = 1f;

        AudioSource src;
        float target;

        static readonly System.Collections.Generic.Dictionary<AmbientKind, AudioClip> cache =
            new System.Collections.Generic.Dictionary<AmbientKind, AudioClip>();

        void Awake()
        {
            src = GetComponent<AudioSource>();
            if (!cache.TryGetValue(kind, out var clip) || clip == null)
            {
                clip = Resources.Load<AudioClip>("AfterZero/Audio/Ambient_" + kind);
                if (clip == null) clip = ProceduralAudio.AmbientLoop("az_amb_" + kind, kind);
                cache[kind] = clip;
            }
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = spatial;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.rolloffMode = AudioRolloffMode.Linear;
            src.dopplerLevel = 0f;
            src.pitch = pitch;
            src.volume = 0f;
            src.time = Random.Range(0f, clip.length * 0.9f);
            src.Play();
        }

        void OnEnable()
        {
            // Re-enabled after being hidden by a TemporalPresence: resume the loop.
            if (src != null && src.clip != null && !src.isPlaying) src.Play();
        }

        void Update()
        {
            target = audibleIn.Contains(TemporalStateManager.CurrentState) ? volume * AudioCues.MasterVolume : 0f;
            src.volume = Mathf.MoveTowards(src.volume, target, Time.deltaTime * 0.8f);
        }
    }
}
