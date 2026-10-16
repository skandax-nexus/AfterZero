using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AfterZero
{
    public static class LevelCatalog
    {
        public const string MainMenu = "AZ_MainMenu";
        public const string Prologue = "AZ_Prologue";
        public const string Epilogue = "AZ_Epilogue";
        /// <summary>levelIndex used by the Prologue / Epilogue LevelDirectors.</summary>
        public const int PrologueIndex = -1;
        public const int EpilogueIndex = 8;

        public static readonly string[] Scenes =
        {
            "AZ_L01_TheBellThatRingsTwice",
            "AZ_L02_UnderTheClockSkin",
            "AZ_L03_TheRainArchive",
            "AZ_L04_TheStreetBeneathTheStreet",
            "AZ_L05_TheDrownedDistrict",
            "AZ_L06_TheNurseryWithNoAddress",
            "AZ_L07_TheBellCatastrophe",
            "AZ_L08_TheFirstZero",
        };

        public static readonly string[] Titles =
        {
            "The Bell That Rings Twice",
            "Under the Clock Skin",
            "The Rain Archive",
            "The Street Beneath the Street",
            "The Drowned District",
            "The Nursery With No Address",
            "The Bell Catastrophe",
            "The First Zero",
        };

        public static readonly string[] Locations =
        {
            "Veyra Arrival Quarter",
            "Founders' Clockworks",
            "Municipal Records Hall",
            "Abandoned Tram Line",
            "Old Veyra Flood Basin",
            "Spatial seam between states",
            "Founders' Square and Clock Tower",
            "Zero Chamber",
        };

        public static int IndexOf(string sceneName) => Array.IndexOf(Scenes, sceneName);
    }

    [Serializable]
    public class SaveData
    {
        public int levelIndex;
        public WorldState.Snapshot world = new WorldState.Snapshot();
    }

    public static class SaveSystem
    {
        static string PathOnDisk => Path.Combine(Application.persistentDataPath, "afterzero_first_cycle.json");

        public static bool HasSave => File.Exists(PathOnDisk);

        public static void Save(int levelIndex)
        {
            var data = new SaveData { levelIndex = levelIndex, world = WorldState.Capture() };
            try { File.WriteAllText(PathOnDisk, JsonUtility.ToJson(data, true)); }
            catch (Exception e) { Debug.LogWarning("[AfterZero] Save failed: " + e.Message); }
        }

        public static SaveData Load()
        {
            if (!HasSave) return null;
            try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(PathOnDisk)); }
            catch (Exception e) { Debug.LogWarning("[AfterZero] Load failed: " + e.Message); return null; }
        }

        public static void Delete()
        {
            if (HasSave) File.Delete(PathOnDisk);
        }
    }

    /// <summary>Scene-to-scene flow: new game, continue, level completion, reload after failure.</summary>
    public static class GameFlow
    {
        static WorldState.Snapshot levelStartSnapshot;
        public static bool IsLoading { get; private set; }

        public static void NewGame()
        {
            SaveSystem.Delete();
            WorldState.Restore(new WorldState.Snapshot());
            LoadScene(Application.CanStreamedLevelBeLoaded(LevelCatalog.Prologue) ? LevelCatalog.Prologue : LevelCatalog.Scenes[0]);
        }

        public static void Continue()
        {
            var data = SaveSystem.Load();
            if (data == null) { NewGame(); return; }
            WorldState.Restore(data.world);
            LoadLevel(Mathf.Clamp(data.levelIndex, 0, LevelCatalog.Scenes.Length - 1));
        }

        public static void LoadLevel(int index)
        {
            if (index < 0 || index >= LevelCatalog.Scenes.Length) { LoadScene(LevelCatalog.MainMenu); return; }
            LoadScene(LevelCatalog.Scenes[index]);
        }

        /// <summary>Called by a LevelDirector when its scene starts.</summary>
        public static void MarkLevelStarted()
        {
            levelStartSnapshot = WorldState.Capture();
        }

        public static void CompleteLevel(int index)
        {
            int next = index + 1;
            SaveSystem.Save(Mathf.Clamp(next, 0, LevelCatalog.Scenes.Length - 1));
            if (index == LevelCatalog.EpilogueIndex) LoadScene(LevelCatalog.MainMenu);
            else if (next >= LevelCatalog.Scenes.Length)
                LoadScene(Application.CanStreamedLevelBeLoaded(LevelCatalog.Epilogue) ? LevelCatalog.Epilogue : LevelCatalog.MainMenu);
            else LoadLevel(next);
        }

        /// <summary>Restart the current level from its starting world state (used when a timed crisis fails).</summary>
        public static void RestartLevel()
        {
            if (levelStartSnapshot != null) WorldState.Restore(levelStartSnapshot);
            LoadScene(SceneManager.GetActiveScene().name);
        }

        public static void LoadScene(string sceneName)
        {
            if (IsLoading) return;
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("[AfterZero] Scene '" + sceneName + "' is not in Build Settings. Run AfterZero ▸ Build All Levels.");
                return;
            }
            CoroutineHost.Run(LoadRoutine(sceneName));
        }

        static IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;
            yield return ScreenFader.FadeOut(0.8f);
            Time.timeScale = 1f;
            AudioListener.pause = false;
            var op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone) yield return null;
            GameInput.ResetLocks();
            IsLoading = false;
            yield return ScreenFader.FadeIn(0.8f);
        }
    }
}
