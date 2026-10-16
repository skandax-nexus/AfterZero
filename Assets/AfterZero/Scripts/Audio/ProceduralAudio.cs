using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Synthesises every sound the prototype needs, so the game is fully audible with zero imported
    /// assets. Drop real clips into Resources/AfterZero/Audio/&lt;CueName&gt; to override any of them.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int Rate = 44100;

        static System.Random rng = new System.Random(1337);
        static float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        static AudioClip Make(string name, float[] data)
        {
            // Normalise to avoid clipping.
            float peak = 0.0001f;
            for (int i = 0; i < data.Length; i++) peak = Mathf.Max(peak, Mathf.Abs(data[i]));
            float g = 0.9f / peak;
            for (int i = 0; i < data.Length; i++) data[i] *= g;
            var clip = AudioClip.Create(name, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float[] Buffer(float seconds) => new float[Mathf.Max(1, Mathf.CeilToInt(seconds * Rate))];

        static float Env(float t, float attack, float decay) =>
            t < attack ? t / attack : Mathf.Exp(-(t - attack) / Mathf.Max(0.0001f, decay));

        // ---------- tonal ----------

        public static AudioClip Bell(string name, float baseHz, float seconds = 4.5f)
        {
            var d = Buffer(seconds);
            float[] partials = { 0.5f, 1f, 1.183f, 1.506f, 2f, 2.514f, 2.662f, 3.011f, 4.166f };
            float[] amps = { 0.6f, 1f, 0.6f, 0.5f, 0.45f, 0.3f, 0.25f, 0.2f, 0.12f };
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate, s = 0f;
                for (int p = 0; p < partials.Length; p++)
                    s += amps[p] * Mathf.Sin(2f * Mathf.PI * baseHz * partials[p] * t) * Mathf.Exp(-t * (0.6f + p * 0.45f));
                d[i] = s * Env(t, 0.002f, 10f);
            }
            return Make(name, d);
        }

        public static AudioClip Chime(string name, float[] notesHz, float spacing, float decay, float fm = 0f)
        {
            float seconds = spacing * notesHz.Length + decay * 3f;
            var d = Buffer(seconds);
            for (int n = 0; n < notesHz.Length; n++)
            {
                int start = Mathf.FloorToInt(n * spacing * Rate);
                for (int i = start; i < d.Length; i++)
                {
                    float t = (i - start) / (float)Rate;
                    float mod = fm > 0f ? Mathf.Sin(2f * Mathf.PI * notesHz[n] * 2f * t) * fm * Mathf.Exp(-t * 3f) : 0f;
                    d[i] += Mathf.Sin(2f * Mathf.PI * notesHz[n] * t + mod) * Env(t, 0.004f, decay) * 0.6f
                          + Mathf.Sin(2f * Mathf.PI * notesHz[n] * 2.01f * t) * Env(t, 0.002f, decay * 0.4f) * 0.2f;
                }
            }
            return Make(name, d);
        }

        public static AudioClip Sweep(string name, float fromHz, float toHz, float seconds, float noiseMix, float glitch = 0f)
        {
            var d = Buffer(seconds);
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate, k = t / seconds;
                float hz = Mathf.Lerp(fromHz, toHz, k * k);
                phase += 2f * Mathf.PI * hz / Rate;
                float s = Mathf.Sin(phase) + 0.35f * Mathf.Sin(phase * 1.5f);
                if (glitch > 0f && (i / 1100) % 5 == 0) s = Mathf.Sign(s) * glitch;
                s = s * (1f - noiseMix) + Noise() * noiseMix;
                d[i] = s * Mathf.Sin(Mathf.PI * k);
            }
            return Make(name, d);
        }

        public static AudioClip Tone(string name, float hz, float seconds, bool square = false, float decay = 0.2f)
        {
            var d = Buffer(seconds);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float s = Mathf.Sin(2f * Mathf.PI * hz * t);
                if (square) s = Mathf.Sign(s) * 0.6f;
                d[i] = s * Env(t, 0.003f, decay);
            }
            return Make(name, d);
        }

        public static AudioClip Pad(string name, float[] chordHz, float seconds)
        {
            var d = Buffer(seconds);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate, k = t / seconds, s = 0f;
                foreach (var hz in chordHz)
                    s += Mathf.Sin(2f * Mathf.PI * hz * t + 0.3f * Mathf.Sin(2f * Mathf.PI * 0.25f * t)) * 0.4f
                       + Mathf.Sin(2f * Mathf.PI * hz * 1.003f * t) * 0.3f;
                d[i] = s * Mathf.Sin(Mathf.PI * k);
            }
            return Make(name, d);
        }

        // ---------- noise based ----------

        public static AudioClip NoiseBurst(string name, float seconds, float lowpass, float decay, float thumpHz = 0f)
        {
            var d = Buffer(seconds);
            float y = 0f, a = Mathf.Clamp01(lowpass);
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                y += a * (Noise() - y);
                float s = y;
                if (thumpHz > 0f) s += Mathf.Sin(2f * Mathf.PI * thumpHz * t * (1f - t)) * 1.5f * Mathf.Exp(-t * 18f);
                d[i] = s * Env(t, 0.002f, decay);
            }
            return Make(name, d);
        }

        public static AudioClip Shatter(string name)
        {
            var d = Buffer(1.2f);
            for (int burst = 0; burst < 9; burst++)
            {
                int start = Mathf.FloorToInt((burst == 0 ? 0f : 0.02f + (float)rng.NextDouble() * 0.5f) * Rate);
                float hz = 2500f + (float)rng.NextDouble() * 4500f;
                for (int i = start; i < d.Length; i++)
                {
                    float t = (i - start) / (float)Rate;
                    d[i] += (Noise() * 0.5f + Mathf.Sin(2f * Mathf.PI * hz * t)) * Env(t, 0.001f, burst == 0 ? 0.08f : 0.05f) * (burst == 0 ? 1f : 0.5f);
                }
            }
            return Make(name, d);
        }

        public static AudioClip Metal(string name, float baseHz, float seconds)
        {
            var d = Buffer(seconds);
            float[] partials = { 1f, 2.32f, 4.25f, 6.63f, 9.38f };
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate, s = 0f;
                for (int p = 0; p < partials.Length; p++)
                    s += Mathf.Sin(2f * Mathf.PI * baseHz * partials[p] * t) * Mathf.Exp(-t * (3f + p * 2f)) / (p + 1);
                s += Noise() * Mathf.Exp(-t * 60f) * 0.6f;
                d[i] = s;
            }
            return Make(name, d);
        }

        public static AudioClip Murmur(string name, float seconds)
        {
            var d = Buffer(seconds);
            float phase = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate;
                float syll = 0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 4.3f * t + Mathf.Sin(t * 1.7f) * 2f);
                float pitch = 140f + 30f * Mathf.Sin(t * 2.1f) + 15f * Mathf.Sin(t * 5.3f);
                phase += 2f * Mathf.PI * pitch / Rate;
                float voice = Mathf.Sin(phase) + 0.5f * Mathf.Sin(phase * 2f) + 0.25f * Mathf.Sin(phase * 3f);
                d[i] = voice * syll * syll * 0.5f;
            }
            return Make(name, d);
        }

        // ---------- ambient loops (seamless-ish: length is a whole number of LFO cycles) ----------

        public static AudioClip AmbientLoop(string name, AmbientKind kind, float seconds = 8f)
        {
            var d = Buffer(seconds);
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < d.Length; i++)
            {
                float t = i / (float)Rate, k = t / seconds, n = Noise(), s = 0f;
                switch (kind)
                {
                    case AmbientKind.Rain:
                        lp += 0.35f * (n - lp);
                        s = lp * 0.6f + (rng.NextDouble() < 0.0009 ? Noise() * 2.5f : 0f);
                        break;
                    case AmbientKind.Crowd:
                        lp += 0.05f * (n - lp);
                        s = lp * 1.6f * (0.7f + 0.3f * Mathf.Sin(2f * Mathf.PI * 3f * k));
                        s += Mathf.Sin(2f * Mathf.PI * (180f + 40f * Mathf.Sin(2f * Mathf.PI * 7f * k)) * t) * 0.05f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 11f * k));
                        break;
                    case AmbientKind.Machine:
                        s = Mathf.Sin(2f * Mathf.PI * 55f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.25f
                          + Mathf.Sin(2f * Mathf.PI * 165f * t) * 0.1f;
                        s *= 0.8f + 0.2f * Mathf.Sin(2f * Mathf.PI * 4f * k);
                        lp += 0.02f * (n - lp);
                        s += lp * 0.8f;
                        if (((int)(t * 2f)) % 2 == 0 && (t * 2f) % 1f < 0.02f) s += n * 1.2f; // ratchet tick
                        break;
                    case AmbientKind.Water:
                        lp += 0.08f * (n - lp);
                        lp2 += 0.01f * (lp - lp2);
                        s = lp2 * 6f * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 2f * k)) + (rng.NextDouble() < 0.0003 ? Mathf.Sin(t * 9000f) : 0f);
                        break;
                    case AmbientKind.Wind:
                        lp += (0.01f + 0.02f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 2f * k))) * (n - lp);
                        s = lp * 5f;
                        break;
                    case AmbientKind.FutureDrone:
                        s = Mathf.Sin(2f * Mathf.PI * 41.2f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 61.7f * t + Mathf.Sin(2f * Mathf.PI * k) * 3f) * 0.3f
                          + Mathf.Sin(2f * Mathf.PI * 329.6f * t) * 0.04f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 4f * k));
                        lp += 0.03f * (n - lp);
                        s += lp * 0.3f;
                        break;
                    case AmbientKind.Nursery:
                        s = Mathf.Sin(2f * Mathf.PI * 523.25f * t) * 0.05f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 2f * k))
                          + Mathf.Sin(2f * Mathf.PI * 659.25f * t) * 0.04f * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * 3f * k))
                          + Mathf.Sin(2f * Mathf.PI * 130.8f * t) * 0.1f;
                        break;
                }
                d[i] = s;
            }
            // Cross-fade the ends so the loop point does not click.
            int fade = Rate / 10;
            for (int i = 0; i < fade; i++)
            {
                float w = i / (float)fade;
                d[i] = d[i] * w + d[d.Length - fade + i] * (1f - w);
            }
            System.Array.Resize(ref d, d.Length - fade);
            return Make(name, d);
        }
    }

    public enum AmbientKind { Rain, Crowd, Machine, Water, Wind, FutureDrone, Nursery }
}
