using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// TIA KHOA (v2 muc 3.5, Man 5 tang 2): quet MOT LAN trong doan G22-G27,5, chi hoat dong o bac 3. Cham tia mat 1 mau.
    /// Muc tieu: co Anh vong => khoa lan cua Anh vong. Khong co => khoa lan cua Khang (bam theo khi Khang doi lan,
    /// nen khong ne duoc). Vi vay o man 5, bong vua do Khang tao ra bang lan tua, vua la "bay nguoc" danh lua NHIP.
    /// (Doan dung o G27,5 thay vi G28 de hai khe thu o G28 nam ngoai tia.)
    /// </summary>
    public class LockBeam : MonoBehaviour
    {
        public NhipBrain brain;
        public TowerFinale finale;
        public float fromTime = 22f, toTime = 27.5f;
        public float nearZ = 0f, farZ = 2.2f, floorY = 0f;
        public Transform strip;        // dai sang tren san (box mong)
        public LineRenderer ray;       // tia tu mat thap xuong diem quet
        public Transform eye;
        public float sweepSpeed = 2.2f; // lan quet / giay thuc
        [Tooltip("Giay thuc truoc khi tia doi sang lan moi cua Khang (khong co bong).")]
        public float followDelay = 0.12f;
        [Tooltip("Bong phai o lan moi lau hon so giay nay thi tia moi doi lan.")]
        public float ghostHysteresis = 1f;

        public bool Active => brain && brain.Tier >= 3 && !(finale && finale.Delivered);
        public int TargetLane { get; private set; } = 0;
        public bool TargetsGhost { get; private set; }
        public int Hits { get; private set; }

        float _pending; int _pendingLane = -1;
        LevelClock _clock;

        void Start()
        {
            if (!brain) brain = NhipBrain.Instance;
            _clock = LevelClock.Instance;
        }

        void Update()
        {
            bool on = Active && _clock;
            if (strip) strip.gameObject.SetActive(on);
            if (ray) ray.enabled = on;
            if (!on) return;

            var g = GhostPlayer.Instance;
            var ph = PlayerHealth.Instance;
            var k = ph ? ph.GetComponent<KhangController>() : null;
            int want; float delay;
            if (g && g.Active) { want = g.Lane; delay = TargetsGhost ? ghostHysteresis : 0f; TargetsGhost = true; }
            else { want = k ? (int)k.CurrentLane : 0; delay = followDelay; TargetsGhost = false; }
            if (want != TargetLane)
            {
                if (_pendingLane != want) { _pendingLane = want; _pending = 0f; }
                _pending += Time.deltaTime;
                if (_pending >= delay) { TargetLane = want; _pendingLane = -1; }
            }
            else _pendingLane = -1;

            float z = TargetLane == 0 ? nearZ : farZ;
            float x0 = _clock.TimeToX(fromTime), x1 = _clock.TimeToX(toTime);
            if (strip)
            {
                strip.position = new Vector3((x0 + x1) * 0.5f, floorY + 0.04f, z);
                strip.localScale = new Vector3(x1 - x0, 0.04f, 1.5f);
            }
            float sx = Mathf.Lerp(x0, x1, 0.5f + 0.5f * Mathf.Sin(Time.time * sweepSpeed));
            if (ray && eye)
            {
                ray.positionCount = 2;
                ray.SetPosition(0, eye.position);
                ray.SetPosition(1, new Vector3(sx, floorY + 0.05f, z));
            }

            if (ph && k && !k.IsSwitchingLane && (int)k.CurrentLane == TargetLane)
            {
                float t = _clock.CurrentTime;
                if (t >= fromTime && t <= toTime && Mathf.Abs(ph.transform.position.y - (floorY + 1.05f)) < 1.5f)
                    if (ph.Damage(1, "tia đỏ")) Hits++;
            }
        }
    }
}
