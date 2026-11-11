using UnityEngine;

namespace EchoShade
{
    /// <summary>Vung roi (hem, mep mai): Khang vao day = -1 HP va ve hom thu.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public class KillZone : MonoBehaviour
    {
        BoxCollider _box;
        void Awake() { _box = GetComponent<BoxCollider>(); _box.isTrigger = true; }

        void Update()
        {
            var ph = PlayerHealth.Instance;
            if (!ph) return;
            if (_box.bounds.Contains(ph.transform.position)) ph.Damage(1, "roi: " + name, true);
        }
    }
}
