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
    ///  - NHAY QUA VAT THAP (v2: "nhay qua thung", "bien nam thap nhay qua duoc"): bam Space khi dang sat mot vat neo
    ///    hoac vat co Vaultable o phia truoc cung lan => Khang vuot qua theo vong cung. X thay doi nen thoi gian
    ///    chay dung theo luat. Nhay tai cho (khong co vat truoc mat) thi thoi gian dung.
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

        [Header("Vault")]
        public float vaultReach = 0.85f;
        public float vaultMaxHeight = 1.25f;
        public float vaultDuration = 0.5f;

        public Lane CurrentLane { get; private set; } = Lane.Near;
        public bool IsVaulting { get; private set; }
        public int VaultCount { get; private set; }
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
            if (Time.timeScale == 0f || !_cc.enabled || IsVaulting) return;
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
                if (KhangInput.Jump && !IsSwitchingLane)
                {
                    if (TryVault()) return;
                    _vy = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
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

        /// <summary>Tim vat thap ngay truoc mat (cung lan) va vuot qua. Tra ve true neu bat dau vuot.</summary>
        public bool TryVault()
        {
            if (IsVaulting || IsSwitchingLane) return false;
            float feet = transform.position.y - _cc.height * 0.5f;
            float x = transform.position.x, dir = Facing;
            Bounds best = default; bool found = false; float bestD = float.MaxValue;
            void Consider(Bounds b)
            {
                if (Mathf.Abs(b.center.z - transform.position.z) > 0.8f) return;
                if (b.max.y - feet > vaultMaxHeight || b.max.y < feet + 0.1f) return;
                float near = dir > 0 ? b.min.x - (x + _cc.radius) : (x - _cc.radius) - b.max.x;
                if (near < -0.25f || near > vaultReach) return;
                if (near < bestD) { bestD = near; best = b; found = true; }
            }
            foreach (var it in HoldableItem.All)
                if (it && it.vaultable && it.State == HoldableItem.ItemState.Anchored && !it.IsReturning) Consider(it.WorldBounds);
            foreach (var v in Vaultable.All) if (v && v.isActiveAndEnabled && v.CanVault) Consider(v.Bounds);
            if (!found) return false;
            float land = dir > 0 ? best.max.x + _cc.radius + 0.15f : best.min.x - _cc.radius - 0.15f;
            if (clock) land = Mathf.Clamp(land, clock.levelStartX, clock.levelEndX);
            var g = GhostPlayer.Instance;
            if (g && g.Active && Mathf.Abs(g.Position.x - land) < 0.7f && Mathf.Abs(g.Position.z - transform.position.z) < 0.9f) return false;
            StartCoroutine(Vault(land, best.max.y - feet));
            return true;
        }

        System.Collections.IEnumerator Vault(float landX, float h)
        {
            IsVaulting = true; VaultCount++;
            Vector3 a = transform.position;
            Vector3 b = new Vector3(landX, a.y, _laneToZ);
            _cc.enabled = false;
            for (float u = 0f; u < 1f;)
            {
                u = Mathf.Min(1f, u + Time.deltaTime / vaultDuration);
                Vector3 p = Vector3.Lerp(a, b, u);
                p.y = a.y + Mathf.Sin(u * Mathf.PI) * (h + 0.35f);
                transform.position = p;
                yield return null;
            }
            transform.position = b;
            _cc.enabled = true;
            _vy = -2f;
            IsVaulting = false;
        }

        /// <summary>Dich chuyen tuc thi (hoi sinh, ket thuc di cap). Goi LevelClock.ResetTracking() neu khong muon tinh la di chuyen.</summary>
        public void Teleport(Vector3 pos)
        {
            StopAllCoroutines();
            IsVaulting = false;
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
