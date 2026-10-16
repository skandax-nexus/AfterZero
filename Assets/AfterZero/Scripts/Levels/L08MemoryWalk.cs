using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Level 08: a playable memory. When <see cref="startFlag"/> is set the player fades into a remembered
    /// place (the Nursery, as a child: lower eye height, PAST light, memory tint). When <see cref="endFlag"/>
    /// is set (Ilse gives the watch) the memory ends after a short delay and the player returns.
    /// </summary>
    public class L08MemoryWalk : MonoBehaviour
    {
        public string startFlag = "";
        public string endFlag = "";
        public string completeFlag = "";
        public Transform memorySpawn;
        public Transform returnSpawn;
        public float childEyeHeight = 1.05f;
        public float endDelay = 6f;
        public List<SubtitleLine> enterLines = new List<SubtitleLine>();

        bool inMemory;
        bool done;
        float savedPivotY = 1.62f;

        void OnEnable() { WorldState.FlagChanged += OnFlag; }
        void OnDisable() { WorldState.FlagChanged -= OnFlag; }

        void OnFlag(string f, bool v)
        {
            if (!v || done) return;
            if (f == startFlag && !inMemory) StartCoroutine(Enter());
            else if (f == endFlag && inMemory) StartCoroutine(Leave());
        }

        IEnumerator Enter()
        {
            inMemory = true;
            var p = PlayerController.Instance;
            if (p == null || memorySpawn == null) yield break;
            GameInput.PushLock();
            yield return ScreenFader.FadeOut(0.9f);
            AudioCues.Play(Cue.Memory);
            TemporalVisuals.SetMemoryMode(true);
            if (TemporalStateManager.Instance != null) TemporalStateManager.Instance.ForceState(TimeState.Past);
            p.Teleport(memorySpawn.position, memorySpawn.rotation);
            if (p.cameraPivot != null)
            {
                var lp = p.cameraPivot.localPosition;
                savedPivotY = lp.y;
                lp.y = childEyeHeight;
                p.cameraPivot.localPosition = lp;
            }
            yield return ScreenFader.FadeIn(0.9f);
            GameInput.PopLock();
            if (enterLines.Count > 0) Subtitles.PlaySequence(enterLines);
            if (WorldState.Get(endFlag)) StartCoroutine(Leave());
        }

        IEnumerator Leave()
        {
            if (done) yield break;
            done = true;
            yield return new WaitForSeconds(endDelay);
            var p = PlayerController.Instance;
            GameInput.PushLock();
            yield return ScreenFader.FadeOut(1.1f);
            if (p != null)
            {
                if (p.cameraPivot != null)
                {
                    var lp = p.cameraPivot.localPosition;
                    lp.y = savedPivotY;
                    p.cameraPivot.localPosition = lp;
                }
                if (returnSpawn != null) p.Teleport(returnSpawn.position, returnSpawn.rotation);
            }
            TemporalVisuals.SetMemoryMode(false);
            if (TemporalStateManager.Instance != null) TemporalStateManager.Instance.ForceState(TimeState.Present);
            inMemory = false;
            yield return ScreenFader.FadeIn(1.1f);
            GameInput.PopLock();
            WorldState.Set(completeFlag);
        }
    }
}
