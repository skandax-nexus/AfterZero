using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// AfterZero ▸ Mobile: configure player settings for Android / iOS and produce builds.
    /// Android: needs the "Android Build Support" module (with OpenJDK + SDK/NDK) in Unity Hub.
    /// iOS: needs the "iOS Build Support" module; the exported Xcode project is compiled on a Mac.
    /// </summary>
    public static class AZMobileBuild
    {
        public const string Company = "Skandax";
        public const string Product = "AfterZero: The First Cycle";
        public const string BundleId = "com.skandax.afterzero.firstcycle";
        public const string Version = "1.0.0";
        public const int BuildNumber = 1;

        [MenuItem("AfterZero/Mobile/Simulate Touch Controls in Editor", priority = 100)]
        static void ToggleTouch()
        {
            AZPlatform.ForceTouchInEditor = !AZPlatform.ForceTouchInEditor;
            Debug.Log("[AfterZero] Touch controls in the editor: " + (AZPlatform.ForceTouchInEditor ? "ON" : "OFF") + " (applies next time you press Play).");
        }

        [MenuItem("AfterZero/Mobile/Simulate Touch Controls in Editor", true)]
        static bool ToggleTouchValidate()
        {
            Menu.SetChecked("AfterZero/Mobile/Simulate Touch Controls in Editor", AZPlatform.ForceTouchInEditor);
            return true;
        }

        static void Common()
        {
            PlayerSettings.companyName = Company;
            PlayerSettings.productName = Product;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            AZProjectSetup.Apply(false);
        }

        [MenuItem("AfterZero/Mobile/Configure for Android", priority = 110)]
        public static void ConfigureAndroid()
        {
            Common();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.bundleVersionCode = BuildNumber;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            Debug.Log("[AfterZero] Android configured: " + BundleId + ", IL2CPP ARM64, API 26+, landscape.");
        }

        [MenuItem("AfterZero/Mobile/Configure for iOS", priority = 111)]
        public static void ConfigureIOS()
        {
            Common();
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.iOS.targetOSVersionString = "15.0";
            PlayerSettings.iOS.buildNumber = BuildNumber.ToString();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.iOS, BuildTarget.iOS);
            Debug.Log("[AfterZero] iOS configured: " + BundleId + ", iOS 15+, landscape.");
        }

        [MenuItem("AfterZero/Mobile/Build Android APK (install on a phone)", priority = 130)]
        public static void BuildApk() => BuildAndroid(false);

        [MenuItem("AfterZero/Mobile/Build Android App Bundle (Google Play)", priority = 131)]
        public static void BuildAab() => BuildAndroid(true);

        static void BuildAndroid(bool bundle)
        {
            ConfigureAndroid();
            EditorUserBuildSettings.buildAppBundle = bundle;
            string path = "Builds/Android/AfterZero_FirstCycle." + (bundle ? "aab" : "apk");
            Build(BuildTarget.Android, BuildTargetGroup.Android, path);
        }

        [MenuItem("AfterZero/Mobile/Export iOS Xcode Project", priority = 132)]
        public static void BuildIOS()
        {
            ConfigureIOS();
            Build(BuildTarget.iOS, BuildTargetGroup.iOS, "Builds/iOS");
        }

        static void Build(BuildTarget target, BuildTargetGroup group, string path)
        {
            var scenes = new List<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) scenes.Add(s.path);
            if (scenes.Count == 0)
            {
                Debug.LogError("[AfterZero] No scenes in Build Settings. Run AfterZero ▸ Build All Levels first.");
                return;
            }
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes.ToArray(),
                locationPathName = path,
                target = target,
                targetGroup = group,
                options = BuildOptions.None,
            });
            if (report != null && report.summary.result == BuildResult.Succeeded)
                Debug.Log("[AfterZero] " + target + " build succeeded: " + path + " (" + (report.summary.totalSize / (1024 * 1024)) + " MB).");
            else
                Debug.LogError("[AfterZero] " + target + " build failed. See the Console for details.");
        }

        /// <summary>Batch mode: Unity -batchmode -quit -executeMethod AfterZero.EditorTools.AZMobileBuild.CIBuildAndroid</summary>
        public static void CIBuildAndroid()
        {
            AZBuild.BuildAllBatch();
            BuildAndroid(false);
        }
    }
}
