using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// BAN DAP TAI (v2 3.2, Man 3): nhan vat nang nhu thung go (vat neo nam tren ban dap). Nguoi va bong KHONG lam no nhan.
    /// Con tai => IsLoaded = true (cau nang ha va giu nguyen). Nhac thung len => het tai.
    /// Vat neo khong phu thuoc thoi diem nen "dat thung o day cho tuong lai" la cach giai Man 3.
    /// </summary>
    public class LoadPedal : MonoBehaviour
    {
        public float centerX, laneZ = 2.2f, halfWidth = 0.9f;
        public Transform plate;
        public Renderer lamp;
        public float pressDepth = 0.08f;

        public bool IsLoaded { get; private set; }
        public HoldableItem Load { get; private set; }
        Vector3 _plateUp;
        Material _lampMat;

        void Awake()
        {
            if (plate) _plateUp = plate.localPosition;
            if (lamp) _lampMat = lamp.material;
        }

        void Update()
        {
            HoldableItem found = null;
            foreach (var it in HoldableItem.All)
            {
                if (!it || it.State != HoldableItem.ItemState.Anchored || it.IsReturning) continue;
                var p = it.transform.position;
                if (Mathf.Abs(p.x - centerX) <= halfWidth && Mathf.Abs(p.z - laneZ) < 0.8f) { found = it; break; }
            }
            if ((found != null) != IsLoaded)
            {
                Debug.Log(found ? $"[Ban dap tai] Co tai: {found.name}" : "[Ban dap tai] Het tai");
                if (found && HUD.Instance) HUD.Instance.ShowSubtitle("Bàn đạp tải đã nhận thùng. Cầu nâng đang hạ.", 3f, false);
            }
            IsLoaded = found; Load = found;
            if (plate) plate.localPosition = Vector3.Lerp(plate.localPosition, _plateUp + (IsLoaded ? Vector3.down * pressDepth : Vector3.zero), 1f - Mathf.Exp(-12f * Time.deltaTime));
            if (_lampMat)
            {
                Color c = IsLoaded ? new Color(0.3f, 1f, 0.45f) : new Color(1f, 0.75f, 0.2f);
                _lampMat.SetColor("_BaseColor", c); _lampMat.EnableKeyword("_EMISSION");
                _lampMat.SetColor("_EmissionColor", c * (IsLoaded ? 3f : 1.2f + Mathf.Abs(Mathf.Sin(Time.time * 3f))));
            }
        }
    }
}
