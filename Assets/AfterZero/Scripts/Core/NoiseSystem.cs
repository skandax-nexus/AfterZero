using System;
using UnityEngine;

namespace AfterZero
{
    public struct NoiseEvent
    {
        public Vector3 position;
        /// <summary>0..~1.5. Walking ≈ 0.45, sprinting 1, crouching 0.1, decoys 1.5.</summary>
        public float loudness;
        public GameObject source;
        public bool isDecoy;
    }

    /// <summary>Vibration / sound propagation used by stealth listeners (the tram maintenance machine).</summary>
    public static class NoiseSystem
    {
        public static event Action<NoiseEvent> Emitted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Emitted = null; }

        public static void Emit(Vector3 position, float loudness, GameObject source, bool isDecoy = false)
        {
            Emitted?.Invoke(new NoiseEvent { position = position, loudness = loudness, source = source, isDecoy = isDecoy });
        }
    }
}
