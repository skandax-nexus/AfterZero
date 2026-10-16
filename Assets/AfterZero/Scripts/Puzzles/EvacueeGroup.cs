using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// A cluster of residents who follow Arin once spoken to, and are saved on reaching the safe zone.
    /// They hesitate at telegraphed hazards (they will not walk into an active hazard zone).
    /// </summary>
    public class EvacueeGroup : Interactable
    {
        public string groupName = "Residents";
        public List<Transform> members = new List<Transform>();
        public Transform safeZone;
        public float safeRadius = 4f;
        public float followDistance = 2.5f;
        public float speed = 3.4f;
        public string savedFlag = "";
        public List<SubtitleLine> onFollow = new List<SubtitleLine>();
        [TextArea] public string savedToast = "";

        public bool Following { get; private set; }
        public bool Saved { get; private set; }

        readonly List<Vector3> offsets = new List<Vector3>();

        void Awake()
        {
            if (prompt == "Inspect") prompt = "Lead " + groupName + " to safety";
            for (int i = 0; i < members.Count; i++)
                offsets.Add(new Vector3((i % 3 - 1) * 0.9f, 0f, -(i / 3) * 0.9f));
        }

        public override bool IsUsableNow => base.IsUsableNow && !Following && !Saved;

        protected override void OnInteract(PlayerInteractor who)
        {
            Following = true;
            AudioCues.PlayAt(Cue.Interact, transform.position);
            if (onFollow.Count > 0) Subtitles.PlaySequence(onFollow);
        }

        void Update()
        {
            if (Saved || !Following) return;
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector3 lead = player.transform.position - player.transform.forward * followDistance;
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];
                if (m == null) continue;
                Vector3 target = lead + player.transform.rotation * offsets[i];
                target.y = m.position.y;
                Vector3 to = target - m.position;
                if (to.magnitude > 0.4f)
                {
                    float s = speed * Mathf.Clamp(to.magnitude / 3f, 0.4f, 1.6f);
                    m.position = Vector3.MoveTowards(m.position, target, s * Time.deltaTime);
                    m.rotation = Quaternion.Slerp(m.rotation, Quaternion.LookRotation(new Vector3(to.x, 0f, to.z).normalized), Time.deltaTime * 6f);
                }
            }

            if (safeZone != null && members.Count > 0)
            {
                int inside = 0;
                foreach (var m in members)
                    if (m != null && Vector3.Distance(Flat(m.position), Flat(safeZone.position)) < safeRadius) inside++;
                if (inside >= Mathf.CeilToInt(members.Count * 0.6f)) Save();
            }
        }

        void Save()
        {
            Saved = true;
            Following = false;
            WorldState.Set(savedFlag);
            AudioCues.Play(Cue.ObjectiveComplete);
            if (!string.IsNullOrEmpty(savedToast)) HUD.Toast(savedToast, 3f);
            for (int i = 0; i < members.Count; i++)
                if (members[i] != null)
                    members[i].position = safeZone.position + new Vector3((i % 3 - 1) * 1.1f, members[i].position.y - safeZone.position.y, (i / 3) * 1.1f);
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
