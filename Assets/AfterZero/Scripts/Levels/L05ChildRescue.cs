using System.Collections.Generic;
using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Level 05: keeps the trapped child's routine from fighting the EvacueeGroup while she follows
    /// Arin, then (once saved at the foot of the Weir Stairs) walks her up the steps to the terrace,
    /// where her changed routine takes over. NPCMotor and EvacueeGroup only move on the flat, so the
    /// climb is handled here.
    /// </summary>
    public class L05ChildRescue : MonoBehaviour
    {
        public EvacueeGroup group;
        public NPCMotor motor;
        public List<Transform> climbPath = new List<Transform>();
        public float climbSpeed = 1.8f;

        int index = -1;
        bool done;

        void Update()
        {
            if (done || group == null || motor == null) return;
            if (group.Following) { motor.Paused = true; return; }
            if (!group.Saved) return;

            if (index < 0) { index = 0; motor.Paused = true; }
            if (index >= climbPath.Count) { done = true; motor.Paused = false; return; }

            var t = motor.transform;
            Vector3 target = climbPath[index].position;
            Vector3 to = target - t.position;
            Vector3 flat = new Vector3(to.x, 0f, to.z);
            if (flat.sqrMagnitude > 0.0001f)
                t.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(flat.normalized), Time.deltaTime * 6f);
            t.position = Vector3.MoveTowards(t.position, target, climbSpeed * Time.deltaTime);
            if (to.magnitude < 0.05f) index++;
        }
    }
}
