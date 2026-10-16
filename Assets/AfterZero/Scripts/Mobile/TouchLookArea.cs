using UnityEngine;
using UnityEngine.EventSystems;

namespace AfterZero
{
    /// <summary>Right-hand screen area: drag to look around. Accumulates pixel deltas for the player to consume.</summary>
    public class TouchLookArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        static Vector2 pending;
        static int activePointer = int.MinValue;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { pending = Vector2.zero; activePointer = int.MinValue; }

        /// <summary>Look delta in pixels since the last call.</summary>
        public static Vector2 Consume()
        {
            var d = pending;
            pending = Vector2.zero;
            return d;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (activePointer == int.MinValue) activePointer = e.pointerId;
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == activePointer) pending += e.delta;
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId == activePointer) activePointer = int.MinValue;
        }
    }
}
