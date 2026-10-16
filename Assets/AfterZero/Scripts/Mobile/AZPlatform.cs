using UnityEngine;

namespace AfterZero
{
    /// <summary>Platform switches: touch UI and the mobile graphics profile.</summary>
    public static class AZPlatform
    {
        /// <summary>Set to true in the editor (AfterZero ▸ Mobile ▸ Simulate Touch Controls) to test the phone UI.</summary>
        public static bool ForceTouchInEditor
        {
            get { try { return PlayerPrefs.GetInt("AZ_ForceTouch", 0) == 1; } catch { return false; } }
            set { PlayerPrefs.SetInt("AZ_ForceTouch", value ? 1 : 0); }
        }

        public static bool IsMobile => Application.isMobilePlatform;

        public static bool UseTouchControls => IsMobile || (Application.isEditor && ForceTouchInEditor);
    }
}
