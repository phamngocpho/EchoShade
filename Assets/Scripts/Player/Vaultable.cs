using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>Danh dau mot vat thap ma Khang nhay qua duoc (vd: bien hieu da roi nam tren san o Man 2).</summary>
    public class Vaultable : MonoBehaviour
    {
        public static readonly List<Vaultable> All = new List<Vaultable>();
        public Collider solid;
        public bool CanVault => !solid || solid.enabled;
        public Bounds Bounds
        {
            get
            {
                if (solid) return solid.bounds;
                var r = GetComponentInChildren<Renderer>();
                return r ? r.bounds : new Bounds(transform.position, Vector3.one * 0.5f);
            }
        }
        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }
    }
}
