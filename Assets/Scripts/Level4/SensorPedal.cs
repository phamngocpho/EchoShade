using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// BAN DAP CAM BIEN (v2 3.2, Man 4): chi nhan NGUOI hoac BONG (Anh vong), khong nhan thung.
    /// Nhan khi Khang dang dung tren ban dap (cung lan) HOAC Anh vong dang o tren ban dap - ke ca khi bong bi
    /// thung neo chan dung lai dung cho do. Khong co buoc an nao (khong can "dung im luc ghi").
    /// </summary>
    public class SensorPedal : MonoBehaviour
    {
        public float fromX, toX, laneZ = 0f;
        public Transform plate;
        public Renderer lamp;
        public float pressDepth = 0.07f;

        public bool KhangOn { get; private set; }
        public bool GhostOn { get; private set; }
        public bool Pressed => KhangOn || GhostOn;
        Vector3 _plateUp;
        Material _lampMat;
        bool _was;

        void Awake()
        {
            if (plate) _plateUp = plate.localPosition;
            if (lamp) _lampMat = lamp.material;
        }

        bool On(Vector3 p, float feetY) => p.x >= fromX && p.x <= toX && Mathf.Abs(p.z - laneZ) < 0.7f && feetY < 0.45f;

        void Update()
        {
            var ph = PlayerHealth.Instance;
            KhangOn = ph && On(ph.transform.position, ph.transform.position.y - 1f);
            var g = GhostPlayer.Instance;
            GhostOn = g && g.Active && On(g.Position, g.Position.y);
            if (Pressed != _was)
            {
                _was = Pressed;
                Debug.Log($"[Ban dap cam bien] {(Pressed ? "NHAN" : "nha")} (Khang={KhangOn}, bong={GhostOn})");
            }
            if (plate) plate.localPosition = Vector3.Lerp(plate.localPosition, _plateUp + (Pressed ? Vector3.down * pressDepth : Vector3.zero), 1f - Mathf.Exp(-12f * Time.deltaTime));
            if (_lampMat)
            {
                Color c = Pressed ? new Color(0.3f, 1f, 0.5f) : new Color(0.35f, 0.6f, 1f);
                _lampMat.SetColor("_BaseColor", c); _lampMat.EnableKeyword("_EMISSION");
                _lampMat.SetColor("_EmissionColor", c * (Pressed ? 3f : 1.2f));
            }
        }
    }
}
