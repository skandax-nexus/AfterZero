using UnityEngine;

namespace AfterZero
{
    /// <summary>Constant rotation (clock gears, fans, hologram rings). Can stop on a flag (tower stopped).</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 degreesPerSecond = new Vector3(0f, 30f, 0f);
        public string stopWhenFlag = "";
        public string startWhenFlag = "";

        void Update()
        {
            if (!string.IsNullOrEmpty(stopWhenFlag) && WorldState.Get(stopWhenFlag)) return;
            if (!string.IsNullOrEmpty(startWhenFlag) && !WorldState.Get(startWhenFlag)) return;
            transform.Rotate(degreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
