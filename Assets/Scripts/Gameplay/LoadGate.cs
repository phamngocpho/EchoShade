using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// CONG TAI (v3.1, phan doan mo rong): cong chan ca hai lan, mo khi ban dap tai (LoadPedal) dang co vat neo.
    /// Nhac vat ra thi cong dong lai (tru khi Khang dang dung duoi cong).
    /// </summary>
    public class LoadGate : MonoBehaviour
    {
        public LoadPedal pedal;
        public Transform panel;
        public Collider blocker;
        public Renderer lamp;
        public float openHeight = 3.2f, speed = 5f, halfWidth = 0.8f;

        public bool IsOpen { get; private set; }
        Vector3 _closed;
        Material _lampMat;

        void Awake()
        {
            if (panel) _closed = panel.position;
            if (lamp) _lampMat = lamp.material;
        }

        void Update()
        {
            var ph = PlayerHealth.Instance;
            bool under = ph && panel && Mathf.Abs(ph.transform.position.x - _closed.x) < halfWidth;
            bool want = (pedal && pedal.IsLoaded) || (IsOpen && under);
            if (want != IsOpen) GameLog.Info("[Cong tai] " + (want ? "MO" : "dong"));
            IsOpen = want;
            if (panel) panel.position = Vector3.MoveTowards(panel.position, _closed + (IsOpen ? Vector3.up * openHeight : Vector3.zero), speed * Time.deltaTime);
            if (blocker) blocker.enabled = !(IsOpen && panel && panel.position.y > _closed.y + openHeight * 0.6f) && !under;
            if (_lampMat)
            {
                Color c = IsOpen ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.6f, 0.15f);
                _lampMat.SetColor("_BaseColor", c); _lampMat.EnableKeyword("_EMISSION"); _lampMat.SetColor("_EmissionColor", c * 2.5f);
            }
        }
    }
}
