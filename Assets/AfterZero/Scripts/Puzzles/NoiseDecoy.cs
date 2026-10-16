using UnityEngine;

namespace AfterZero
{
    /// <summary>Creates a false vibration somewhere else (drop a pipe on the rails, start a pump).</summary>
    public class NoiseDecoy : Interactable
    {
        public Transform noisePoint;
        public float delay = 1.2f;
        public float loudness = 1.6f;
        public float cooldown = 8f;
        public string usedFlag = "";
        [Tooltip("Decoys sharing a group draw from one difficulty-scaled supply (Story: unlimited, Normal: 3, Hard: 2).")]
        public string supplyGroup = "";

        static readonly System.Collections.Generic.Dictionary<string, int> used = new System.Collections.Generic.Dictionary<string, int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            used.Clear();
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += (sc, m) => used.Clear();
        }

        public static int Remaining(string group)
        {
            int limit = GameSettings.DecoyLimit;
            if (limit <= 0 || string.IsNullOrEmpty(group)) return int.MaxValue;
            used.TryGetValue(group, out int n);
            return Mathf.Max(0, limit - n);
        }

        float lastUse = -999f;

        void Awake() { if (prompt == "Inspect") prompt = "Create a false vibration"; }

        public override bool IsUsableNow => base.IsUsableNow && Time.time - lastUse > cooldown;

        protected override void OnInteract(PlayerInteractor who)
        {
            if (Remaining(supplyGroup) <= 0) { HUD.Toast("No decoys left. Find another way past.", 3f); AudioCues.Play(Cue.Denied); return; }
            if (!string.IsNullOrEmpty(supplyGroup)) { used.TryGetValue(supplyGroup, out int n); used[supplyGroup] = n + 1; }
            int left = Remaining(supplyGroup);
            if (left != int.MaxValue) HUD.Toast("Decoys left: " + left, 2.5f);
            lastUse = Time.time;
            StartCoroutine(Fire());
        }

        System.Collections.IEnumerator Fire()
        {
            AudioCues.PlayAt(Cue.Click, transform.position);
            yield return new WaitForSeconds(delay);
            Vector3 p = noisePoint != null ? noisePoint.position : transform.position;
            AudioCues.PlayAt(Cue.Clang, p, 1f);
            NoiseSystem.Emit(p, loudness, gameObject, true);
            WorldState.Set(usedFlag);
        }
    }
}
