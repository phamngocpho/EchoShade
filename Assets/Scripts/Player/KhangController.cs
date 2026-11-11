using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Dieu khien Khang (CharacterController).
    ///
    /// Lien quan den thoi gian: thoi gian CHI phu thuoc vi tri X (xem LevelClock).
    ///  - Doi lan chi doi Z  => thoi gian dung.
    ///  - Nhay chi doi Y      => thoi gian dung. Mac dinh khong cho dieu khien X tren khong
    ///    (allowAirControl = false) de "nhay = thoi gian dung" dung voi kich ban.
    ///  - Trong luc doi lan cung khoa di ngang.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class KhangController : MonoBehaviour
    {
        public enum Lane { Near = 0, Far = 1 }

        [Header("Move")]
        public float moveSpeed = 4f;

        [Header("Lanes (Z)")]
        public float nearLaneZ = 0f;
        public float farLaneZ = 2.2f;
        [Tooltip("Thoi gian (giay thuc) cho 1 buoc doi lan.")]
        public float laneSwitchDuration = 0.18f;

        [Header("Jump")]
        public float jumpHeight = 1.4f;
        public float gravity = -25f;
        public bool allowAirControl = false;

        [Header("Bounds")]
        [Tooltip("Neu co, X bi kep trong [levelStartX, levelEndX] cua dong ho.")]
        public LevelClock clock;

        public Lane CurrentLane { get; private set; } = Lane.Near;
        public bool IsSwitchingLane => _laneT < 1f;
        public bool IsGrounded => _cc.isGrounded;
        /// <summary>+1 nhin phai, -1 nhin trai (dung de dat vat phia truoc).</summary>
        public float Facing { get; private set; } = 1f;

        CharacterController _cc;
        float _vy;
        float _laneFromZ, _laneToZ, _laneT = 1f;

        void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (!clock) clock = FindAnyObjectByType<LevelClock>();
            // Bat dau o lan gan nhat voi Z hien tai
            CurrentLane = Mathf.Abs(transform.position.z - farLaneZ) < Mathf.Abs(transform.position.z - nearLaneZ) ? Lane.Far : Lane.Near;
            _laneFromZ = _laneToZ = LaneZ(CurrentLane);
        }

        float LaneZ(Lane l) => l == Lane.Near ? nearLaneZ : farLaneZ;

        void Update()
        {
            if (Time.timeScale == 0f || !_cc.enabled) return;
            float dt = Time.deltaTime;
            Vector3 move = Vector3.zero;

            // --- Doi lan (khong anh huong thoi gian) ---
            if (!IsSwitchingLane && _cc.isGrounded)
            {
                if (KhangInput.FarLane && CurrentLane != Lane.Far) TrySwitchLane(Lane.Far);
                else if (KhangInput.NearLane && CurrentLane != Lane.Near) TrySwitchLane(Lane.Near);
            }

            // --- Di ngang (DUY NHAT thu lam thoi gian chay) ---
            float h = KhangInput.Horizontal;
            bool canMoveX = !IsSwitchingLane && (_cc.isGrounded || allowAirControl);
            if (canMoveX && Mathf.Abs(h) > 0.01f)
            {
                move.x = h * moveSpeed * dt;
                Facing = Mathf.Sign(h);
            }

            // --- Nhay + trong luc (khong anh huong thoi gian) ---
            if (_cc.isGrounded)
            {
                if (_vy < 0f) _vy = -2f;
                if (KhangInput.Jump && !IsSwitchingLane) _vy = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            _vy += gravity * dt;
            move.y = _vy * dt;

            // --- Buoc doi lan muot ---
            if (IsSwitchingLane)
            {
                _laneT = Mathf.Min(1f, _laneT + dt / Mathf.Max(0.01f, laneSwitchDuration));
                float targetZ = Mathf.Lerp(_laneFromZ, _laneToZ, Mathf.SmoothStep(0f, 1f, _laneT));
                move.z = targetZ - transform.position.z;
            }
            else
            {
                move.z = _laneToZ - transform.position.z; // giu dung lan
            }

            // --- Kep X trong thuoc thoi gian ---
            if (clock)
            {
                float nx = Mathf.Clamp(transform.position.x + move.x, clock.levelStartX, clock.levelEndX);
                move.x = nx - transform.position.x;
            }

            _cc.Move(move);
        }

        /// <summary>Dich chuyen tuc thi (hoi sinh, ket thuc di cap). Goi LevelClock.ResetTracking() neu khong muon tinh la di chuyen.</summary>
        public void Teleport(Vector3 pos)
        {
            _cc.enabled = false;
            transform.position = pos;
            _cc.enabled = true;
            _vy = 0f;
            CurrentLane = Mathf.Abs(pos.z - farLaneZ) < Mathf.Abs(pos.z - nearLaneZ) ? Lane.Far : Lane.Near;
            _laneFromZ = _laneToZ = LaneZ(CurrentLane);
            _laneT = 1f;
        }

        /// <summary>
        /// Bo loc doi lan tuy chon (vd: Man 3 chi cho doi lan o nga ba t = 2 s). null = luon cho phep (Man 1-2).
        /// </summary>
        public System.Func<Lane, bool> laneSwitchGate;

        /// <summary>Yeu cau doi lan (dung cho phim W/S va test). Tra ve true neu bat dau doi lan.</summary>
        public bool TrySwitchLane(Lane target)
        {
            if (IsSwitchingLane || !_cc.isGrounded || CurrentLane == target) return false;
            if (laneSwitchGate != null && !laneSwitchGate(target)) return false;
            BeginLaneSwitch(target);
            return true;
        }

        void BeginLaneSwitch(Lane target)
        {
            CurrentLane = target;
            _laneFromZ = transform.position.z;
            _laneToZ = LaneZ(target);
            _laneT = 0f;
        }
    }
}
