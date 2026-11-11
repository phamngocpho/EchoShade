using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 3 - THANH CHAN + TIA QUET truoc hai khe.
    /// Ca hai nam o lan NHIP dang canh (NhipTowerMind.GuardLane). Luc dau canh lan xa (khoa khe that).
    /// Tia quet la Hazard (tru 1 HP, day sang lan kia) => di lan xa truoc se bi danh va la cach de "chet"
    /// cho NHIP quen. Da giao thu => thanh chan nang len, tia tat.
    /// </summary>
    public class SlotBarrier : MonoBehaviour
    {
        public NhipTowerMind mind;
        public float nearZ = 0f, farZ = 2.2f;
        public float moveSpeed = 5f;
        public float raiseHeight = 3.2f;
        [Header("Tia quet")]
        public Hazard beam;
        public float beamX = 3.3f;
        public float beamY = 1.5f;

        Vector3 _base;
        Renderer[] _beamRs;

        void Start()
        {
            if (!mind) mind = NhipTowerMind.Instance;
            _base = transform.position;
            if (beam) _beamRs = beam.GetComponentsInChildren<Renderer>(true);
            Snap();
        }

        float GuardZ => mind && mind.GuardLane == KhangController.Lane.Near ? nearZ : farZ;

        /// <summary>Dat ngay vao lan dang canh (khong chuyen dong).</summary>
        public void Snap()
        {
            transform.position = new Vector3(_base.x, _base.y, GuardZ);
            if (beam) beam.transform.position = new Vector3(beamX, beamY, GuardZ);
        }

        void Update()
        {
            if (!mind) return;
            bool open = mind.Delivered;
            Vector3 want = new Vector3(_base.x, open ? _base.y + raiseHeight : _base.y, GuardZ);
            transform.position = Vector3.MoveTowards(transform.position, want, moveSpeed * Time.deltaTime);
            if (beam)
            {
                Vector3 bp = beam.transform.position;
                beam.transform.position = Vector3.MoveTowards(bp, new Vector3(beamX, beamY, GuardZ), moveSpeed * Time.deltaTime);
                bool moving = Mathf.Abs(beam.transform.position.z - GuardZ) > 0.01f;
                beam.active = !open && !moving;
                if (_beamRs != null) foreach (var r in _beamRs) if (r) r.enabled = !open;
            }
        }
    }
}
