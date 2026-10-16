using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Makes this object and everything under it exist only in some states.
    /// It toggles components (renderers, colliders, lights, audio, behaviours) rather than
    /// GameObject active flags, so cause-and-effect reactors remain free to SetActive children
    /// (e.g. a Past action enabling new Present dressing) without the two systems fighting.
    /// While absent it can show a holographic echo of itself, revealed by Echo Sense.
    /// </summary>
    [DisallowMultipleComponent]
    public class TemporalPresence : MonoBehaviour
    {
        public TimeMask existsIn = TimeMask.Present;
        [Tooltip("While absent, show a holographic echo of this object (revealed by Echo Sense). Use for gameplay-relevant objects; disable for large dressing groups.")]
        public bool showEcho = true;

        public bool IsPresent { get; private set; } = true;

        static readonly HashSet<TemporalPresence> all = new HashSet<TemporalPresence>();
        static bool echoesRendering;
        static Material echoMaterial;

        readonly List<Component> managed = new List<Component>();
        readonly List<Component> disabledByMe = new List<Component>();
        readonly List<Rigidbody> frozenByMe = new List<Rigidbody>();
        GameObject echo;
        bool applied;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { all.Clear(); echoesRendering = false; echoMaterial = null; }

        /// <summary>Called by TemporalVisuals when Echo Sense starts/stops (avoids drawing invisible echoes).</summary>
        public static void SetEchoRendering(bool on)
        {
            if (echoesRendering == on) return;
            echoesRendering = on;
            foreach (var p in all) if (p != null) p.RefreshEcho();
        }

        void Awake()
        {
            foreach (var c in GetComponentsInChildren<Component>(true))
            {
                if (c == this || c is Transform) continue;
                if (c is Renderer || c is Collider || c is Behaviour) managed.Add(c);
            }
            if (showEcho) BuildEcho();
            all.Add(this);
        }

        void OnDestroy()
        {
            all.Remove(this);
            if (echo != null) Destroy(echo);
        }

        void OnEnable()
        {
            TemporalStateManager.StateChanged += OnStateChanged;
            Apply(TemporalStateManager.CurrentState);
        }

        void OnDisable()
        {
            TemporalStateManager.StateChanged -= OnStateChanged;
        }

        void OnStateChanged(TimeState from, TimeState to) => Apply(to);

        public void Apply(TimeState state)
        {
            bool present = existsIn.Contains(state);
            if (applied && present == IsPresent) return;
            applied = true;
            IsPresent = present;

            if (present)
            {
                foreach (var c in disabledByMe)
                    if (c != null && c.transform.IsChildOf(transform)) SetEnabled(c, true);
                disabledByMe.Clear();
                foreach (var rb in frozenByMe)
                    if (rb != null && rb.transform.IsChildOf(transform)) rb.isKinematic = false;
                frozenByMe.Clear();
            }
            else
            {
                foreach (var c in managed)
                {
                    if (c == null || !c.transform.IsChildOf(transform)) continue; // e.g. an object the player carried away
                    if (!IsEnabled(c)) continue;
                    SetEnabled(c, false);
                    disabledByMe.Add(c);
                }
                // Colliders are off while absent, so freeze physics bodies or they fall through the floor.
                foreach (var rb in GetComponentsInChildren<Rigidbody>(true))
                {
                    if (rb.isKinematic) continue;
                    rb.isKinematic = true;
                    frozenByMe.Add(rb);
                }
            }
            RefreshEcho();
        }

        static bool IsEnabled(Component c)
        {
            if (c is Behaviour b) return b.enabled;
            if (c is Renderer r) return r.enabled;
            if (c is Collider col) return col.enabled;
            return false;
        }

        static void SetEnabled(Component c, bool on)
        {
            if (c is Behaviour b) b.enabled = on;
            else if (c is Renderer r) r.enabled = on;
            else if (c is Collider col) col.enabled = on;
        }

        // ---------- echoes ----------

        void RefreshEcho()
        {
            if (echo != null) echo.SetActive(!IsPresent && echoesRendering);
        }

        void BuildEcho()
        {
            if (echoMaterial == null)
            {
                echoMaterial = Resources.Load<Material>("AfterZero/EchoHologram");
                if (echoMaterial == null)
                {
                    var shader = Shader.Find("AfterZero/Hologram");
                    if (shader == null) return;
                    echoMaterial = new Material(shader);
                    echoMaterial.SetFloat("_UseEchoReveal", 1f);
                }
            }

            echo = new GameObject(name + " [Echo]");
            echo.transform.SetParent(transform.parent, false);
            echo.transform.SetPositionAndRotation(transform.position, transform.rotation);
            echo.transform.localScale = transform.localScale;

            int count = 0;
            foreach (var mf in GetComponentsInChildren<MeshFilter>(true))
            {
                if (mf.sharedMesh == null) continue;
                if (mf.GetComponentInParent<NPCMotor>() != null || mf.GetComponentInParent<Rigidbody>() != null) continue;
                var mr = mf.GetComponent<MeshRenderer>();
                if (mr == null || !mr.enabled) continue;
                var part = new GameObject(mf.name);
                part.transform.SetParent(echo.transform, false);
                part.transform.position = mf.transform.position;
                part.transform.rotation = mf.transform.rotation;
                part.transform.localScale = Vector3.Scale(mf.transform.lossyScale, Inverse(transform.lossyScale));
                part.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
                var r = part.AddComponent<MeshRenderer>();
                r.sharedMaterial = echoMaterial;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
                if (++count >= 300) break;
            }
            if (count == 0) { Destroy(echo); echo = null; return; }
            echo.SetActive(false);
        }

        static Vector3 Inverse(Vector3 v) =>
            new Vector3(v.x != 0f ? 1f / v.x : 0f, v.y != 0f ? 1f / v.y : 0f, v.z != 0f ? 1f / v.z : 0f);
    }
}
