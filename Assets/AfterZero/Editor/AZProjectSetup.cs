using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AfterZero.EditorTools
{
    /// <summary>
    /// Makes sure the project renders with URP in linear HDR and applies the high-fidelity settings
    /// the AfterZero look relies on (HDR bloom, MSAA, long soft shadows, depth/opaque textures).
    /// Runs automatically once when the editor loads; also available from the AfterZero menu.
    /// </summary>
    [InitializeOnLoad]
    public static class AZProjectSetup
    {
        const string PipelinePath = "Assets/AfterZero/Generated/AZ_URP_HighFidelity.asset";
        const string SessionKey = "AfterZero.SetupChecked";

        static AZProjectSetup()
        {
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += () => Apply(false);
        }

        [MenuItem("AfterZero/Setup/Apply High-Fidelity Graphics", priority = 80)]
        public static void ApplyFromMenu() => Apply(true);

        public static void Apply(bool verbose)
        {
            var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null)
            {
                urp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
                if (urp == null)
                {
                    AZMaterials.EnsureFolder("Assets/AfterZero/Generated");
                    urp = UniversalRenderPipelineAsset.Create();
                    AssetDatabase.CreateAsset(urp, PipelinePath);
                    urp.LoadBuiltinRendererData();
                    Debug.Log("[AfterZero] No URP asset was assigned; created " + PipelinePath + ". (Tip: the Universal 3D template ships a renderer with SSAO already configured.)");
                }
                GraphicsSettings.defaultRenderPipeline = urp;
                QualitySettings.renderPipeline = urp;
            }

            urp.supportsHDR = true;
            urp.msaaSampleCount = 4;
            urp.shadowDistance = Mathf.Max(urp.shadowDistance, 90f);
            urp.supportsCameraDepthTexture = true;
            urp.supportsCameraOpaqueTexture = true;
            EditorUtility.SetDirty(urp);

            if (PlayerSettings.colorSpace != ColorSpace.Linear) PlayerSettings.colorSpace = ColorSpace.Linear;
            EnsureInputSystemBackend();
            AssetDatabase.SaveAssets();
            if (verbose) Debug.Log("[AfterZero] High-fidelity graphics applied to " + urp.name + ".");
        }

        /// <summary>
        /// The game reads input through the Input System package. Make sure the player uses it
        /// (Both = 2, so legacy-only plugins keep working too). Takes effect after an editor restart
        /// and immediately for builds, including CI.
        /// </summary>
        static void EnsureInputSystemBackend()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null || prop.intValue == 1 || prop.intValue == 2) return;
            prop.intValue = 2;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log("[AfterZero] Enabled the Input System backend (Both). Restart the editor if touch/keyboard input does nothing in Play mode.");
        }
    }
}