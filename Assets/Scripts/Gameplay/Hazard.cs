using UnityEngine;

namespace EchoShade
{
    /// <summary>Vat gay sat thuong khi cham Khang (cang may, tam ton dang roi). Kiem tra giao nhau bounds moi khung.</summary>
    [DefaultExecutionOrder(100)] // chay sau LevelClock/ClawRail de dung vi tri moi nhat cua khung
    public class Hazard : MonoBehaviour
    {
        public bool active = true;
        public int damage = 1;
        [Tooltip("Sau khi trung don, day Khang sang lan con lai (doi lan khong doi thoi gian). " +
                 "Tranh bi tru mau lien tuc khi Khang dung yen ben trong vat nguy hiem dang 'dong bang' (thoi gian dung khi Khang dung yen).")]
        public bool knockToOtherLane = false;
        Renderer[] _rs;

        void Awake() { _rs = GetComponentsInChildren<Renderer>(); }

        void LateUpdate()
        {
            if (!active || _rs == null || _rs.Length == 0) return;
            var ph = PlayerHealth.Instance;
            if (!ph) return;
            Bounds b = _rs[0].bounds;
            for (int i = 1; i < _rs.Length; i++) if (_rs[i].enabled) b.Encapsulate(_rs[i].bounds);
            b.Expand(-0.05f);
            if (!b.Intersects(ph.Bounds)) return;
            int deaths = ph.Deaths;
            if (ph.Damage(damage, name) && knockToOtherLane && ph.Deaths == deaths)
            {
                var k = ph.GetComponent<KhangController>();
                if (k)
                {
                    Vector3 p = k.transform.position;
                    p.z = k.CurrentLane == KhangController.Lane.Near ? k.farLaneZ : k.nearLaneZ;
                    k.Teleport(p);
                }
            }
        }
    }
}
