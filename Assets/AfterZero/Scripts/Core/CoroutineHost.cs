using System.Collections;
using UnityEngine;

namespace AfterZero
{
    /// <summary>Persistent helper so static code can run coroutines across scene loads.</summary>
    public class CoroutineHost : MonoBehaviour
    {
        static CoroutineHost instance;

        public static Coroutine Run(IEnumerator routine)
        {
            if (instance == null)
            {
                var go = new GameObject("[AfterZero CoroutineHost]");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<CoroutineHost>();
            }
            return instance.StartCoroutine(routine);
        }
    }
}
