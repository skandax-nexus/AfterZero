using UnityEngine;

namespace AfterZero
{
    /// <summary>Rotates in fixed steps when used (water channel segments, hanging shapes, dials).</summary>
    public class RotatableElement : Interactable
    {
        public Transform pivot;
        public Vector3 axis = Vector3.up;
        [Range(2, 12)] public int steps = 4;
        public int startIndex = 0;
        public int correctIndex = 0;
        [Tooltip("Text glyph drawn on the element so alignment never depends on colour alone.")]
        public string glyph = "";

        public int Index { get; private set; }
        public bool IsCorrect => Index == correctIndex;
        public event System.Action<RotatableElement> Rotated;

        Quaternion baseRot;
        float visualAngle;

        void Awake()
        {
            if (pivot == null) pivot = transform;
            baseRot = pivot.localRotation;
            Index = ((startIndex % steps) + steps) % steps;
            visualAngle = Index * 360f / steps;
            pivot.localRotation = baseRot * Quaternion.AngleAxis(visualAngle, axis);
            if (prompt == "Inspect") prompt = "Rotate";
        }

        protected override void OnInteract(PlayerInteractor who)
        {
            Index = (Index + 1) % steps;
            AudioCues.PlayAt(Cue.Click, transform.position);
            Rotated?.Invoke(this);
        }

        void Update()
        {
            float target = Index * 360f / steps;
            if (target < visualAngle - 1f) target += 360f;
            visualAngle = Mathf.MoveTowards(visualAngle, target, Time.deltaTime * 360f);
            if (visualAngle >= 360f) visualAngle -= 360f;
            pivot.localRotation = baseRot * Quaternion.AngleAxis(visualAngle, axis);
        }
    }
}
