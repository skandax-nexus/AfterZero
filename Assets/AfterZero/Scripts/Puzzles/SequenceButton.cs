using UnityEngine;

namespace AfterZero
{
    public class SequenceButton : Interactable
    {
        public SequencePuzzle puzzle;
        public int noteIndex;
        public bool bellLike = false;
        public Transform pressVisual;

        float pressT;
        Vector3 basePos;

        void Awake()
        {
            if (pressVisual != null) basePos = pressVisual.localPosition;
            if (prompt == "Inspect") prompt = "Strike";
        }

        protected override void OnInteract(PlayerInteractor who)
        {
            if (puzzle != null) puzzle.Press(this);
            pressT = 1f;
        }

        void Update()
        {
            if (pressVisual == null || pressT <= 0f) return;
            pressT = Mathf.MoveTowards(pressT, 0f, Time.deltaTime * 4f);
            pressVisual.localPosition = basePos + Vector3.down * 0.04f * pressT;
        }
    }
}
