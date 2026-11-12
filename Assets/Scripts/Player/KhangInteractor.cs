using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Phim E: nhat vat gan nhat (cung lan) hoac dat vat dang cam xuong truoc mat Khang.
    /// </summary>
    [RequireComponent(typeof(KhangController))]
    public class KhangInteractor : MonoBehaviour
    {
        public float pickRadius = 1.3f;
        public Vector3 holdOffset = new Vector3(0f, 1.4f, 0f);
        public float placeDistance = 1.0f;

        /// <summary>
        /// Cac he thong khac (vd: CableCrossing) co the "an" phim E truoc. Tra ve true = da xu ly.
        /// </summary>
        public static event System.Func<bool> InteractOverride;

        public HoldableItem Held { get; private set; }
        KhangController _khang;

        void Awake() { _khang = GetComponent<KhangController>(); }

        void LateUpdate()
        {
            // Vat dang cam nam ben hong / tay Khang, doi ben theo huong mat.
            if (Held && Held.transform.parent == transform)
                Held.transform.localPosition = new Vector3(holdOffset.x * _khang.Facing, holdOffset.y, holdOffset.z);
        }

        void Update()
        {
            if (Time.timeScale == 0f || !KhangInput.Interact) return;
            if (InteractOverride != null)
                foreach (System.Func<bool> f in InteractOverride.GetInvocationList())
                    if (f()) return;
            if (Held) TryPlace();
            else TryPick();
        }

        public bool TryPick()
        {
            HoldableItem best = null;
            float bestD = float.MaxValue;
            foreach (var item in HoldableItem.All)
            {
                if (!item || !item.pickable || item.IsReturning || item.State == HoldableItem.ItemState.Held) continue;
                Vector3 d = item.transform.position - transform.position;
                if (Mathf.Abs(d.z) > 0.8f) continue; // chi nhat vat cung lan
                if (Mathf.Abs(d.y) > 2f) continue; // vat treo tren cao (vd: trong cang may) khong voi toi
                d.y = 0f;
                float dist = d.magnitude;
                if (dist <= pickRadius && dist < bestD) { best = item; bestD = dist; }
            }
            if (best)
            {
                best.PickUp(transform, holdOffset);
                Held = best;
                return true;
            }
            return false;
        }

        public void TryPlace()
        {
            Vector3 p = transform.position + new Vector3(_khang.Facing * placeDistance, 0f, 0f);
            if (_khang.clock) p.x = Mathf.Clamp(p.x, _khang.clock.levelStartX, _khang.clock.levelEndX);
            if (!Held) return;
            Held.Place(p);
            Held = null;
        }
    }
}
