using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    public enum Cue
    {
        Interact, Denied, PickUp, Place, Click, Clang, Scrape, Shatter, Footstep,
        RuleChange, ShiftPast, ShiftPresent, ShiftFuture, ShiftBlocked, LoopReset,
        ObjectiveComplete, PuzzleSolved, Checkpoint, LevelExit, Hurt,
        HazardWarn, HazardActive, Alert, WaterRush, Bell, Tick, Memory, Murmur, Revelation
    }

    /// <summary>
    /// One-shot sound playback with a small pooled set of sources. Every rule change has its own
    /// distinct cue (one per state shift, loop reset, rule change, puzzle solved…).
    /// </summary>
    public static class AudioCues
    {
        static readonly Dictionary<Cue, AudioClip> clips = new Dictionary<Cue, AudioClip>();
        static readonly Dictionary<int, AudioClip> tones = new Dictionary<int, AudioClip>();
        static AudioSource[] pool;
        static AudioSource ui;
        static int next;
        static GameObject host;

        public static float MasterVolume = 1f;

        static void EnsureHost()
        {
            if (host != null) return;
            host = new GameObject("[AfterZero Audio]");
            Object.DontDestroyOnLoad(host);
            ui = host.AddComponent<AudioSource>();
            ui.playOnAwake = false;
            ui.spatialBlend = 0f;
            pool = new AudioSource[20];
            for (int i = 0; i < pool.Length; i++)
            {
                var go = new GameObject("src" + i);
                go.transform.SetParent(host.transform);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 2f;
                s.maxDistance = 32f;
                s.dopplerLevel = 0f;
                pool[i] = s;
            }
        }

        public static AudioClip Get(Cue cue)
        {
            if (clips.TryGetValue(cue, out var c) && c != null) return c;
            c = Resources.Load<AudioClip>("AfterZero/Audio/" + cue);
            if (c == null) c = Synthesize(cue);
            clips[cue] = c;
            return c;
        }

        static AudioClip Synthesize(Cue cue)
        {
            string n = "az_" + cue;
            switch (cue)
            {
                case Cue.Interact: return ProceduralAudio.Chime(n, new[] { 880f }, 0.05f, 0.08f);
                case Cue.Denied: return ProceduralAudio.Chime(n, new[] { 220f, 196f }, 0.09f, 0.08f);
                case Cue.PickUp: return ProceduralAudio.Chime(n, new[] { 660f, 990f }, 0.05f, 0.06f);
                case Cue.Place: return ProceduralAudio.Metal(n, 320f, 0.5f);
                case Cue.Click: return ProceduralAudio.NoiseBurst(n, 0.06f, 0.9f, 0.01f);
                case Cue.Clang: return ProceduralAudio.Metal(n, 180f, 1.6f);
                case Cue.Scrape: return ProceduralAudio.NoiseBurst(n, 0.5f, 0.15f, 0.25f);
                case Cue.Shatter: return ProceduralAudio.Shatter(n);
                case Cue.Footstep: return ProceduralAudio.NoiseBurst(n, 0.12f, 0.25f, 0.03f, 90f);
                case Cue.RuleChange: return ProceduralAudio.Chime(n, new[] { 523.25f, 783.99f, 1046.5f, 1567.98f }, 0.07f, 0.5f, 0.6f);
                case Cue.ShiftPast: return ProceduralAudio.Chime(n, new[] { 587.33f, 440f, 349.23f }, 0.08f, 0.6f);
                case Cue.ShiftPresent: return ProceduralAudio.Chime(n, new[] { 392f, 523.25f }, 0.06f, 0.4f, 0.3f);
                case Cue.ShiftFuture: return ProceduralAudio.Sweep(n, 180f, 1400f, 0.55f, 0.15f, 0.6f);
                case Cue.ShiftBlocked: return ProceduralAudio.Tone(n, 110f, 0.35f, true, 0.12f);
                case Cue.LoopReset: return ProceduralAudio.Sweep(n, 900f, 60f, 1.2f, 0.35f);
                case Cue.ObjectiveComplete: return ProceduralAudio.Chime(n, new[] { 659.25f, 830.61f, 987.77f }, 0.1f, 0.7f);
                case Cue.PuzzleSolved: return ProceduralAudio.Chime(n, new[] { 523.25f, 659.25f, 783.99f, 1046.5f }, 0.12f, 0.9f, 0.4f);
                case Cue.Checkpoint: return ProceduralAudio.Chime(n, new[] { 783.99f, 1174.66f }, 0.08f, 0.5f);
                case Cue.LevelExit: return ProceduralAudio.Pad(n, new[] { 261.63f, 392f, 523.25f }, 2.5f);
                case Cue.Hurt: return ProceduralAudio.NoiseBurst(n, 0.6f, 0.08f, 0.2f, 60f);
                case Cue.HazardWarn: return ProceduralAudio.Sweep(n, 300f, 900f, 0.8f, 0.05f);
                case Cue.HazardActive: return ProceduralAudio.NoiseBurst(n, 1.0f, 0.3f, 0.35f, 50f);
                case Cue.Alert: return ProceduralAudio.Chime(n, new[] { 1200f, 900f, 1200f, 900f }, 0.09f, 0.06f, 1.5f);
                case Cue.WaterRush: return ProceduralAudio.NoiseBurst(n, 2.5f, 0.06f, 1.2f);
                case Cue.Bell: return ProceduralAudio.Bell(n, 196f, 6f);
                case Cue.Tick: return ProceduralAudio.NoiseBurst(n, 0.04f, 0.95f, 0.006f);
                case Cue.Memory: return ProceduralAudio.Pad(n, new[] { 220f, 277.18f, 329.63f, 415.3f }, 3.5f);
                case Cue.Murmur: return ProceduralAudio.Murmur(n, 1.6f);
                case Cue.Revelation: return ProceduralAudio.Pad(n, new[] { 146.83f, 220f, 293.66f, 349.23f, 440f }, 4f);
                default: return ProceduralAudio.Tone(n, 440f, 0.2f);
            }
        }

        /// <summary>Non-spatial (UI / global) cue.</summary>
        public static void Play(Cue cue, float volume = 1f)
        {
            EnsureHost();
            ui.PlayOneShot(Get(cue), volume * MasterVolume);
        }

        /// <summary>Positional cue in the world.</summary>
        public static void PlayAt(Cue cue, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            PlayClipAt(Get(cue), position, volume, pitch);
        }

        public static void PlayClipAt(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
        {
            if (clip == null) return;
            EnsureHost();
            var s = pool[next];
            next = (next + 1) % pool.Length;
            s.transform.position = position;
            s.pitch = pitch;
            s.volume = volume * MasterVolume;
            s.clip = clip;
            s.Play();
        }

        /// <summary>Pitched musical note — bell or music-box timbre — for sequence puzzles.</summary>
        public static void PlayTone(float pitchMultiplier, Vector3 position, bool bellLike)
        {
            int key = bellLike ? 1 : 0;
            if (!tones.TryGetValue(key, out var clip) || clip == null)
            {
                clip = bellLike ? ProceduralAudio.Bell("az_note_bell", 261.63f, 3f)
                                : ProceduralAudio.Chime("az_note_box", new[] { 523.25f }, 0.01f, 0.6f, 0.2f);
                tones[key] = clip;
            }
            PlayClipAt(clip, position, 1f, pitchMultiplier);
        }
    }
}
