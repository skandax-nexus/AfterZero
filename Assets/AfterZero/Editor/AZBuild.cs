using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// Menu: AfterZero ▸ Build All Levels generates every scene of The First Cycle from code,
    /// registers them in Build Settings and opens Level 01. Re-running rebuilds (overwrites) scenes.
    /// </summary>
    public static class AZBuild
    {
        public const string SceneFolder = "Assets/AfterZero/Scenes";

        static readonly Action<Transform>[] builders =
        {
            Level01Builder.Build,
            Level02Builder.Build,
            Level03Builder.Build,
            Level04Builder.Build,
            Level05Builder.Build,
            Level06Builder.Build,
            Level07Builder.Build,
            Level08Builder.Build,
        };

        [MenuItem("AfterZero/Build All Levels", priority = 1)]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                PrepareAssets();
                BuildMainMenu();
                BuildStoryScenes();
                for (int i = 0; i < builders.Length; i++)
                {
                    EditorUtility.DisplayProgressBar("AfterZero", "Building " + LevelCatalog.Titles[i], (i + 1f) / (builders.Length + 1f));
                    BuildLevelInternal(i);
                }
                RegisterBuildScenes();
                AssetDatabase.SaveAssets();
            }
            finally { EditorUtility.ClearProgressBar(); }
            EditorSceneManager.OpenScene(ScenePath(LevelCatalog.Scenes[0]));
            Debug.Log("[AfterZero] Built main menu + 8 levels. Press Play, or open AZ_MainMenu to start from the title screen.");
        }

        [MenuItem("AfterZero/Build Level/01 The Bell That Rings Twice", priority = 20)] static void B1() => BuildOne(0);
        [MenuItem("AfterZero/Build Level/02 Under the Clock Skin", priority = 21)] static void B2() => BuildOne(1);
        [MenuItem("AfterZero/Build Level/03 The Rain Archive", priority = 22)] static void B3() => BuildOne(2);
        [MenuItem("AfterZero/Build Level/04 The Street Beneath the Street", priority = 23)] static void B4() => BuildOne(3);
        [MenuItem("AfterZero/Build Level/05 The Drowned District", priority = 24)] static void B5() => BuildOne(4);
        [MenuItem("AfterZero/Build Level/06 The Nursery With No Address", priority = 25)] static void B6() => BuildOne(5);
        [MenuItem("AfterZero/Build Level/07 The Bell Catastrophe", priority = 26)] static void B7() => BuildOne(6);
        [MenuItem("AfterZero/Build Level/08 The First Zero", priority = 27)] static void B8() => BuildOne(7);

        [MenuItem("AfterZero/Build Main Menu", priority = 40)]
        static void BuildMenuOnly()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareAssets();
            BuildMainMenu();
            RegisterBuildScenes();
        }

        [MenuItem("AfterZero/Delete Save File", priority = 60)]
        static void DeleteSave()
        {
            SaveSystem.Delete();
            Debug.Log("[AfterZero] Save deleted.");
        }

        /// <summary>Entry point for batch mode / Unity MCP: Unity -batchmode -executeMethod AfterZero.EditorTools.AZBuild.BuildAllBatch</summary>
        public static void BuildAllBatch()
        {
            PrepareAssets();
            BuildMainMenu();
            BuildStoryScenes();
            for (int i = 0; i < builders.Length; i++) BuildLevelInternal(i);
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
        }

        public static void BuildOne(int index)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareAssets();
            BuildLevelInternal(index);
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(ScenePath(LevelCatalog.Scenes[index]));
        }

        static void PrepareAssets()
        {
            AZMaterials.ClearCache();
            AZMaterials.EnsureFolder(SceneFolder);
            AZMaterials.EnsureFolder(AZMaterials.MatFolder);
            AZMaterials.EchoMaterial();
            foreach (var key in AZMaterials.Keys) AZMaterials.Get(key);
            AZMaterials.Hologram("hologram");
            AZMaterials.Hologram("hologram_amber");
            AZMaterials.Hologram("hologram_magenta");
            AssetDatabase.SaveAssets();
        }

        static string ScenePath(string sceneName) => SceneFolder + "/" + sceneName + ".unity";

        static void BuildLevelInternal(int index)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Level " + (index + 1).ToString("00") + " — " + LevelCatalog.Titles[index]).transform;
            builders[index](root);
            FixMovingStatics(root);
            Lightmapping.lightingSettings = LightingFor();
            EditorSceneManager.SaveScene(scene, ScenePath(LevelCatalog.Scenes[index]));
        }

        /// <summary>
        /// Static batching freezes geometry in place, so anything gameplay moves (doors, water, gears,
        /// NPCs, pickups, hazards, lifts) must not be static. Clears the flags on every such hierarchy.
        /// </summary>
        public static void FixMovingStatics(Transform root)
        {
            var moving = new HashSet<Transform>();
            void Add(Transform t) { if (t != null) moving.Add(t); }
            foreach (var r in root.GetComponentsInChildren<FlagReactor>(true)) Add(r.moveTarget);
            foreach (var f in root.GetComponentsInChildren<FlagInteractable>(true)) Add(f.animateTarget);
            foreach (var w in root.GetComponentsInChildren<WaterBody>(true)) { Add(w.surface); Add(w.transform); }
            foreach (var h in root.GetComponentsInChildren<TelegraphedHazard>(true)) Add(h.shakeTarget);
            foreach (var b in root.GetComponentsInChildren<SequenceButton>(true)) Add(b.pressVisual);
            foreach (var e in root.GetComponentsInChildren<EvacueeGroup>(true)) foreach (var m in e.members) Add(m);
            foreach (var r in root.GetComponentsInChildren<RotatableElement>(true)) { Add(r.pivot); Add(r.transform); }
            foreach (var c in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (c == null) continue;
                if (c is Spinner || c is FloatBob || c is MovingHazard || c is VibrationSentinel || c is NPCMotor ||
                    c is Grabbable || c is Pushable || c is ObservedExit || c is NeonFlicker ||
                    c is KeeperPatrol || c is Pursuer || c is MovingPlatform ||
                    (c.GetType().Namespace == "AfterZero" && c.GetType().Name.StartsWith("L0", StringComparison.Ordinal)))
                    Add(c.transform);
            }
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true)) Add(rb.transform);
            int n = 0;
            foreach (var t in moving)
                foreach (var child in t.GetComponentsInChildren<Transform>(true))
                {
                    GameObjectUtility.SetStaticEditorFlags(child.gameObject, 0);
                    n++;
                }
            if (n > 0) Debug.Log("[AfterZero] Cleared static flags on " + n + " moving objects under " + root.name + ".");
        }

        static LightingSettings lighting;
        static LightingSettings LightingFor()
        {
            // Realtime GI off, baked off: every state change must relight instantly.
            if (lighting != null) return lighting;
            string path = AZMaterials.Root + "/AZ_Lighting.lighting";
            lighting = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (lighting == null)
            {
                lighting = new LightingSettings { name = "AZ_Lighting", bakedGI = false, realtimeGI = false };
                AssetDatabase.CreateAsset(lighting, path);
            }
            return lighting;
        }

        [MenuItem("AfterZero/Build Prologue + Epilogue", priority = 41)]
        static void BuildStoryOnly()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PrepareAssets();
            BuildStoryScenes();
            RegisterBuildScenes();
        }

        static void BuildStoryScenes()
        {
            BuildScene(LevelCatalog.Prologue, "Prologue — The Last Train", AZStoryScenes.BuildPrologue);
            BuildScene(LevelCatalog.Epilogue, "Epilogue — After Zero", AZStoryScenes.BuildEpilogue);
        }

        static void BuildScene(string sceneName, string rootName, Action<Transform> build)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject(rootName).transform;
            build(root);
            FixMovingStatics(root);
            Lightmapping.lightingSettings = LightingFor();
            EditorSceneManager.SaveScene(scene, ScenePath(sceneName));
        }

        static void BuildMainMenu()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("Main Menu").transform;

            // A slice of Veyra seen from above the square, at the Future/Present seam.
            var tsmGo = new GameObject("_Systems");
            tsmGo.transform.SetParent(root);
            var tsm = tsmGo.AddComponent<TemporalStateManager>();
            tsm.startState = TimeState.Future;
            tsm.allowedStates = TimeMask.Future;
            var sun = AZKit.Sun(root, new Vector3(20f, 30f, 0f), Color.white, 0.5f);
            var vis = tsmGo.AddComponent<TemporalVisuals>();
            vis.sun = sun;
            RenderSettings.skybox = AZMaterials.Skybox();
            tsmGo.AddComponent<MainMenu>();

            var city = AZKit.Group("City", root);
            AZKit.Floor("Ground", city, Vector3.zero, new Vector2(400, 400), "obsidian");
            var rng = new System.Random(7);
            for (int i = 0; i < 70; i++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float dist = 30f + (float)rng.NextDouble() * 120f;
                var p = new Vector3(Mathf.Cos(ang) * dist, 0f, Mathf.Sin(ang) * dist);
                var size = new Vector3(8f + (float)rng.NextDouble() * 10f, 10f + (float)rng.NextDouble() * 45f, 8f + (float)rng.NextDouble() * 10f);
                AZKit.Building("Tower" + i, city, p, size, "ruin_dark", i % 3 == 0 ? "neon_magenta" : "neon_cyan", "obsidian", i);
            }
            // The clock tower in the middle, split open.
            var tower = AZKit.Group("ClockTower", city);
            AZKit.Box("TowerBase", tower, new Vector3(0, 20, 0), new Vector3(10, 40, 10), "ruin_dark");
            AZKit.Box("TowerTopL", tower, new Vector3(-3, 48, 0), new Vector3(4, 16, 10), "ruin_dark", true, -8f);
            AZKit.Box("TowerTopR", tower, new Vector3(3.5f, 47, 0), new Vector3(4, 14, 10), "ruin_dark", true, 10f);
            var face = AZKit.Hologram("ClockFace", tower, new Vector3(0, 34, -5.3f), new Vector3(7, 7, 0.1f), PrimitiveType.Cylinder, "hologram", false, true);
            face.transform.rotation = Quaternion.Euler(90, 0, 0);
            AZKit.PointLight("CoreGlow", tower, new Vector3(0, 44, 0), new Color(0.5f, 0.9f, 1f), 8f, 40f);
            AZKit.Particles("Motes", root, new Vector3(0, 30, 0), new Vector3(120, 40, 120), "motes");

            var camGo = new GameObject("MenuCamera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(root);
            camGo.transform.position = new Vector3(0, 28, -70);
            camGo.transform.rotation = Quaternion.Euler(8, 0, 0);
            var cam = camGo.AddComponent<Camera>();
            cam.farClipPlane = 800f;
            cam.fieldOfView = 55f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraFXSetup>();
            AZKit.Ambient("Drone", root, Vector3.zero, AmbientKind.FutureDrone, 0.4f, TimeMask.All, 0f);

            Lightmapping.lightingSettings = LightingFor();
            EditorSceneManager.SaveScene(scene, ScenePath(LevelCatalog.MainMenu));
        }

        static void RegisterBuildScenes()
        {
            var list = new List<EditorBuildSettingsScene>();
            list.Add(new EditorBuildSettingsScene(ScenePath(LevelCatalog.MainMenu), true));
            var all = new List<string> { LevelCatalog.Prologue };
            all.AddRange(LevelCatalog.Scenes);
            all.Add(LevelCatalog.Epilogue);
            foreach (var s in all)
            {
                string p = ScenePath(s);
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(p) != null) list.Add(new EditorBuildSettingsScene(p, true));
            }
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
