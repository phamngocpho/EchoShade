using System.Collections;
using UnityEngine;

namespace EchoShade
{
    /// <summary>Man 3 - cong chan ca hai lan sau khu khe; ha xuong khi thu da duoc giao.</summary>
    public class L3EndGate : MonoBehaviour
    {
        public NhipTowerMind mind;
        public float lowerBy = 3.3f;
        public float duration = 1.2f;
        bool _opened;

        void Start() { if (!mind) mind = NhipTowerMind.Instance; if (mind) mind.OnDelivered += Open; }
        void OnDestroy() { if (mind) mind.OnDelivered -= Open; }

        public bool IsOpen => _opened;

        void Open()
        {
            if (_opened) return;
            _opened = true;
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            StartCoroutine(Lower());
        }

        IEnumerator Lower()
        {
            Vector3 a = transform.position, b = a + Vector3.down * lowerBy;
            for (float u = 0f; u < 1f; u += Time.deltaTime / duration)
            {
                transform.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, u));
                yield return null;
            }
            transform.position = b;
        }
    }
}
