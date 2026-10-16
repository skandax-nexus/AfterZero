using UnityEngine;

namespace AfterZero
{
    /// <summary>
    /// Marks a moving/rotating surface (gear teeth, lifts, rafts) the player rides. The player controller
    /// asks for the surface's motion since last frame and moves with it. Runs late so it records its pose
    /// after the player has used last frame's delta.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class MovingPlatform : MonoBehaviour
    {
        Matrix4x4 last;
        bool has;

        void OnEnable() { last = transform.localToWorldMatrix; has = true; }

        void LateUpdate()
        {
            last = transform.localToWorldMatrix;
            has = true;
        }

        /// <summary>World-space displacement of a point riding this platform since the last frame.</summary>
        public Vector3 DeltaFor(Vector3 worldPoint)
        {
            if (!has) return Vector3.zero;
            Vector3 local = last.inverse.MultiplyPoint3x4(worldPoint);
            return transform.localToWorldMatrix.MultiplyPoint3x4(local) - worldPoint;
        }

        /// <summary>Yaw the platform turned since last frame (to rotate the rider's view).</summary>
        public float YawDelta()
        {
            if (!has) return 0f;
            Vector3 a = last.MultiplyVector(Vector3.forward), b = transform.forward;
            a.y = 0f; b.y = 0f;
            return Vector3.SignedAngle(a, b, Vector3.up);
        }
    }
}
