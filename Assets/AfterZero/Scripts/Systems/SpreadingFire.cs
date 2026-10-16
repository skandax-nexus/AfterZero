using System;
using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    [Serializable]
    public class FireNode
    {
        public Transform point;
        [Tooltip("This node is put out (and can't reignite) once this flag is set — e.g. a routed water channel.")]
        public string extinguishFlag = "";
        public ParticleSystem flames;
        public Light glow;
        [HideInInspector] public bool burning;
    }

    /// <summary>
    /// Fire that spreads node by node along a path (the Keepers burning the archive). Smoke, crackle and
    /// a growing glow telegraph each node before it ignites. Standing in a burning node hurts. If the fire
    /// reaches the last node (the evidence) the player has failed: respawn at the checkpoint, fire resets.
    /// </summary>
    public class SpreadingFire : MonoBehaviour
    {
        public string startFlag = "";
        public List<FireNode> nodes = new List<FireNode>();
        public float spreadInterval = 9f;
        public float telegraphTime = 3f;
        public float damageRadius = 1.6f;
        public string lostFlag = "";
        [TextArea] public string lostMessage = "The fire reached the records. Route the water faster.";
        public string allOutFlag = "";

        bool running;
        float timer;
        int front;

        void OnEnable() { WorldState.FlagChanged += OnFlag; Checkpoint.Respawned += ResetFire; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; Checkpoint.Respawned -= ResetFire; }

        void Start()
        {
            spreadInterval *= GameSettings.TimerScale;
            foreach (var n in nodes) SetNode(n, false, 0f);
            if (string.IsNullOrEmpty(startFlag) || WorldState.Get(startFlag)) Begin();
        }

        void OnFlag(string f, bool v)
        {
            if (!v) return;
            if (f == startFlag) Begin();
            bool anyBurning = false;
            foreach (var n in nodes)
            {
                if (n.burning && WorldState.Get(n.extinguishFlag)) { SetNode(n, false, 0f); AudioCues.PlayAt(Cue.WaterRush, n.point.position, 0.8f); }
                anyBurning |= n.burning;
            }
            if (running && !anyBurning && AllProtected()) { running = false; WorldState.Set(allOutFlag); AudioCues.Play(Cue.PuzzleSolved); }
        }

        bool AllProtected()
        {
            foreach (var n in nodes) if (!string.IsNullOrEmpty(n.extinguishFlag) && !WorldState.Get(n.extinguishFlag)) return false;
            return true;
        }

        void Begin()
        {
            if (running || nodes.Count == 0) return;
            running = true;
            front = 0;
            timer = 0f;
            Ignite(0);
            HUD.Toast("Fire! The Keepers are burning the records.", 4f);
        }

        void ResetFire()
        {
            if (!running) return;
            foreach (var n in nodes) SetNode(n, false, 0f);
            front = 0;
            timer = 0f;
            Ignite(0);
        }

        void Ignite(int i)
        {
            if (i < 0 || i >= nodes.Count) return;
            var n = nodes[i];
            if (WorldState.Get(n.extinguishFlag)) return;
            SetNode(n, true, 1f);
            AudioCues.PlayAt(Cue.HazardActive, n.point.position, 0.7f);
            if (i == nodes.Count - 1)
            {
                WorldState.Set(lostFlag);
                var p = PlayerController.Instance;
                if (p != null) p.Die(lostMessage);
            }
        }

        void SetNode(FireNode n, bool burning, float intensity)
        {
            n.burning = burning;
            if (n.flames != null) { if (burning) n.flames.Play(); else n.flames.Stop(); }
            if (n.glow != null) { n.glow.enabled = burning || intensity > 0f; n.glow.intensity = intensity * 4f; }
        }

        void Update()
        {
            if (!running) return;
            timer += Time.deltaTime;
            int next = front + 1;
            if (next < nodes.Count)
            {
                var n = nodes[next];
                float until = spreadInterval - timer;
                if (until < telegraphTime && n.glow != null && !WorldState.Get(n.extinguishFlag))
                {
                    n.glow.enabled = true;
                    n.glow.intensity = (1f - until / telegraphTime) * 1.5f * (0.7f + 0.3f * Mathf.Sin(Time.time * 20f));
                }
                if (timer >= spreadInterval)
                {
                    timer = 0f;
                    front = next;
                    Ignite(next);
                }
            }

            var p = PlayerController.Instance;
            if (p == null) return;
            foreach (var node in nodes)
                if (node.burning && Vector3.Distance(p.transform.position, node.point.position) < damageRadius)
                { p.Die("Caught by the fire. Watch the smoke and glow before a shelf ignites."); break; }
        }
    }
}
