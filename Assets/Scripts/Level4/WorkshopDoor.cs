using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// CUA XUONG (Man 4, lan gan G12): mo khi ban dap cam bien dang duoc nhan, roi ban dap thi dong.
    /// An toan: neu Khang dang dung duoi cua thi cua cho Khang di qua roi moi dong.
    /// </summary>
    public class WorkshopDoor : MonoBehaviour
    {
        public SensorPedal pedal;
        public Transform panel;
        public Collider blocker;
        public float openHeight = 3f, speed = 6f, halfWidth = 0.9f;
        public Renderer lamp;

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
            bool khangUnder = ph && panel && Mathf.Abs(ph.transform.position.x - _closed.x) < halfWidth && Mathf.Abs(ph.transform.position.z - _closed.z) < 0.9f;
            bool wantOpen = (pedal && pedal.Pressed) || (IsOpen && khangUnder);
            if (wantOpen != IsOpen) Debug.Log("[Cua xuong] " + (wantOpen ? "MO" : "dong"));
            IsOpen = wantOpen;
            if (panel) panel.position = Vector3.MoveTowards(panel.position, _closed + (IsOpen ? Vector3.up * openHeight : Vector3.zero), speed * Time.deltaTime);
            if (blocker) blocker.enabled = !IsOpen || (panel && panel.position.y < _closed.y + openHeight * 0.6f && !khangUnder);
            if (_lampMat)
            {
                Color c = IsOpen ? new Color(0.3f, 1f, 0.5f) : new Color(1f, 0.25f, 0.15f);
                _lampMat.SetColor("_BaseColor", c); _lampMat.EnableKeyword("_EMISSION"); _lampMat.SetColor("_EmissionColor", c * 2.5f);
            }
        }
    }
}
