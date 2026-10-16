using System;
using UnityEngine;

namespace AfterZero
{
    public enum Difficulty { Story = 0, Normal = 1, Hard = 2 }

    /// <summary>
    /// Player options (persisted in PlayerPrefs) and the difficulty scalars every system reads.
    /// See Docs/DESIGN_V2.md §3.
    /// </summary>
    public static class GameSettings
    {
        public static event Action Changed;

        static bool loaded;
        static Difficulty difficulty = Difficulty.Normal;
        static float lookSensitivity = 1f;
        static bool invertY;
        static int quality = -1; // -1 = auto
        static bool leftHanded;
        static bool vibration = true;
        static float subtitleScale = 1f;

        static void Load()
        {
            if (loaded) return;
            loaded = true;
            try
            {
                difficulty = (Difficulty)PlayerPrefs.GetInt("AZ_Difficulty", 1);
                lookSensitivity = PlayerPrefs.GetFloat("AZ_LookSens", 1f);
                invertY = PlayerPrefs.GetInt("AZ_InvertY", 0) == 1;
                quality = PlayerPrefs.GetInt("AZ_Quality", -1);
                leftHanded = PlayerPrefs.GetInt("AZ_LeftHanded", 0) == 1;
                vibration = PlayerPrefs.GetInt("AZ_Vibration", 1) == 1;
                subtitleScale = PlayerPrefs.GetFloat("AZ_SubScale", 1f);
            }
            catch (Exception) { }
        }

        static void Save()
        {
            try
            {
                PlayerPrefs.SetInt("AZ_Difficulty", (int)difficulty);
                PlayerPrefs.SetFloat("AZ_LookSens", lookSensitivity);
                PlayerPrefs.SetInt("AZ_InvertY", invertY ? 1 : 0);
                PlayerPrefs.SetInt("AZ_Quality", quality);
                PlayerPrefs.SetInt("AZ_LeftHanded", leftHanded ? 1 : 0);
                PlayerPrefs.SetInt("AZ_Vibration", vibration ? 1 : 0);
                PlayerPrefs.SetFloat("AZ_SubScale", subtitleScale);
                PlayerPrefs.Save();
            }
            catch (Exception) { }
            Changed?.Invoke();
        }

        public static Difficulty Difficulty { get { Load(); return difficulty; } set { Load(); difficulty = value; Save(); } }
        public static float LookSensitivity { get { Load(); return lookSensitivity; } set { Load(); lookSensitivity = Mathf.Clamp(value, 0.3f, 3f); Save(); } }
        public static bool InvertY { get { Load(); return invertY; } set { Load(); invertY = value; Save(); } }
        /// <summary>0 = Low, 1 = Medium, 2 = High, -1 = automatic.</summary>
        public static int Quality { get { Load(); return quality; } set { Load(); quality = Mathf.Clamp(value, -1, 2); Save(); } }
        public static bool LeftHanded { get { Load(); return leftHanded; } set { Load(); leftHanded = value; Save(); } }
        public static bool Vibration { get { Load(); return vibration; } set { Load(); vibration = value; Save(); } }
        public static float SubtitleScale { get { Load(); return subtitleScale; } set { Load(); subtitleScale = Mathf.Clamp(value, 0.8f, 1.6f); Save(); } }

        // ---------- difficulty scalars ----------

        /// <summary>Seconds before an objective hint appears; infinity on Hard.</summary>
        public static float HintDelay => Difficulty == Difficulty.Story ? 45f : Difficulty == Difficulty.Normal ? 120f : float.PositiveInfinity;
        /// <summary>Multiplier on crisis timer durations.</summary>
        public static float TimerScale => Difficulty == Difficulty.Story ? 1.5f : Difficulty == Difficulty.Normal ? 1f : 0.75f;
        /// <summary>Multiplier on enemy hearing range and sight speed.</summary>
        public static float PerceptionScale => Difficulty == Difficulty.Story ? 0.6f : Difficulty == Difficulty.Normal ? 1f : 1.35f;
        /// <summary>Checkpoints flagged as "climax" are removed on Hard.</summary>
        public static bool ClimaxCheckpoints => Difficulty != Difficulty.Hard;
        /// <summary>Decoys available per NoiseDecoy group (0 = unlimited).</summary>
        public static int DecoyLimit => Difficulty == Difficulty.Story ? 0 : Difficulty == Difficulty.Normal ? 3 : 2;

        public static string DifficultyLabel(Difficulty d) =>
            d == Difficulty.Story ? "STORY - generous timers, early hints" :
            d == Difficulty.Normal ? "NORMAL - the intended challenge" :
            "HARD - no hints, short timers, sharp enemies";
    }

    /// <summary>Short vibration on rule changes, hits and alarms (phones only, respects the setting).</summary>
    public static class AZHaptics
    {
        static float last;

        public static void Pulse()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (!GameSettings.Vibration || Time.unscaledTime - last < 0.25f) return;
            last = Time.unscaledTime;
            Handheld.Vibrate();
#endif
        }
    }
}
