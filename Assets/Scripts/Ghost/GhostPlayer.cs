using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// ANH VONG (bong cua Khang) - v2 muc 3.6.
    ///  - Xuat hien khi NHIP dat bac 3 sau mot lan tua hop le: di lai "doan di tien gan nhat bi tua"
    ///    (vi tri, lan, nhay, dung im) bang THOI GIAN THUC, toc do = speedFactor (0,6) lan Khang.
    ///    Khong bi dong bang khi Khang dung im (khong doc LevelClock).
    ///  - Moi lan tua hop le moi o bac 3: bong bat dau lai voi doan moi nhat.
    ///  - Vat ran: Khang khong di xuyen qua (CapsuleCollider). Khong gay mat mau.
    ///    Neu Khang dung chan duong, bong cho (khong day Khang).
    ///  - Bi neo chan: quy dao dam vao vat neo => dung tai cho, dung yen cho toi khi bac < 3.
    ///  - Het quy dao: dung yen o diem cuoi. Bien mat khi bac < 3 hoac khi Khang ve thung thu.
    ///  - De ban dap cam bien nhu nguoi that (SensorPedal doc vi tri cua bong).
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class GhostPlayer : MonoBehaviour
    {
        public static GhostPlayer Instance { get; private set; }

        public NhipBrain brain;
        public Transform visual;
        public Animator animator;
        public CapsuleCollider solid;
        public int activeFromTier = 3;
        public float speedFactor = 0.6f;
        [Tooltip("Giay thuc bong dung yen (hien dan) truoc khi buoc di.")]
        public float appearDelay = 1.0f;
        public float feetOffset = -1.0f; // tu tam CharacterController xuong chan
        public float nearLaneZ = 0f, farLaneZ = 2.2f;

        public bool Active { get; private set; }
        public bool Finished { get; private set; }
        public bool BlockedByAnchor => BlockedBy != null;
        public HoldableItem BlockedBy { get; private set; }
        public bool WaitingForKhang { get; private set; }
        public Vector3 Position { get; private set; }
        public int Lane => Mathf.Abs(Position.z - farLaneZ) < Mathf.Abs(Position.z - nearLaneZ) ? 1 : 0;
        public int SpawnCount { get; private set; }
        public List<TrailSample> Path => _path;

        public event System.Action OnSpawned;

        List<TrailSample> _path = new List<TrailSample>();
        float _play, _delay, _speed;
        int _idx;
        bool _graceOverlap;
        float _yaw = 90f;
        Renderer[] _rs;

        void Awake()
        {
            Instance = this;
            if (visual) _rs = visual.GetComponentsInChildren<Renderer>(true);
            Show(false);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            if (!brain) brain = NhipBrain.Instance;
            if (brain) { brain.OnRewindProcessed += OnRewind; brain.OnTierChanged += OnTier; }
            var lr = LevelRunner.Instance;
            if (lr && lr.data) speedFactor = lr.data.ghostSpeedFactor;
            var ph = PlayerHealth.Instance;
            if (ph) ph.OnRespawned += Despawn;
        }

        void OnRewind(List<TrailSample> seg)
        {
            if (brain.Tier < activeFromTier || seg == null || seg.Count < 2) return;
            Spawn(seg);
        }

        void OnTier(int oldT, int newT) { if (newT < activeFromTier) Despawn(); }

        public void Spawn(List<TrailSample> seg)
        {
            _path = seg;
            _play = 0f; _idx = 0; _delay = appearDelay;
            Active = true; Finished = false; BlockedBy = null; WaitingForKhang = false;
            Position = FeetAt(0);
            _graceOverlap = true; // luc xuat hien bong trung cho Khang: chua tinh la vat ran
            SpawnCount++;
            Show(true);
            Place(Position, 0f);
            Debug.Log($"[Anh vong] Xuat hien: {seg.Count} mau, x {seg[0].pos.x:0.0} -> {seg[seg.Count - 1].pos.x:0.0}, {seg[seg.Count - 1].rt:0.0}s thuc");
            OnSpawned?.Invoke();
            var lr = LevelRunner.Instance;
            if (HUD.Instance)
            {
                string line = lr && lr.data && !string.IsNullOrEmpty(lr.data.ghostLine) ? lr.data.ghostLine : "NHỊP đã thuộc đường anh.";
                HUD.Instance.ShowSubtitle(line, 5f, true);
            }
        }

        public void Despawn()
        {
            if (!Active) return;
            Active = false; BlockedBy = null; Finished = false;
            Show(false);
            Debug.Log("[Anh vong] Bien mat");
        }

        Vector3 FeetAt(int i) { var p = _path[Mathf.Clamp(i, 0, _path.Count - 1)].pos; p.y += feetOffset; return p; }

        Vector3 Sample(float rt, out int idx)
        {
            idx = _idx;
            while (idx < _path.Count - 1 && _path[idx + 1].rt <= rt) idx++;
            if (idx >= _path.Count - 1) { idx = _path.Count - 1; return FeetAt(idx); }
            var a = _path[idx]; var b = _path[idx + 1];
            float u = Mathf.InverseLerp(a.rt, b.rt, rt);
            var p = Vector3.Lerp(a.pos, b.pos, u); p.y += feetOffset; return p;
        }

        void Update()
        {
            if (!Active) { _speed = 0f; return; }
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            var ph = PlayerHealth.Instance;
            Vector3 k = ph ? ph.transform.position : new Vector3(9999, 0, 0);
            k.y += feetOffset;

            if (_graceOverlap && !Overlaps(Position, k, 0.9f)) _graceOverlap = false;
            if (solid) solid.enabled = !_graceOverlap;

            Vector3 prev = Position;
            if (_delay > 0f) _delay -= dt;
            else if (!BlockedByAnchor && !Finished)
            {
                float np = _play + dt * speedFactor * playbackBoost;
                var next = Sample(np, out int ni);
                WaitingForKhang = !_graceOverlap && Overlaps(next, k, 0.82f) && !Overlaps(Position, k, 0.82f);
                var anchor = WaitingForKhang ? null : AnchorHit(next);
                if (anchor)
                {
                    BlockedBy = anchor;
                    Debug.Log($"[Anh vong] Bi vat neo {anchor.name} chan tai x={Position.x:0.00}");
                    if (HUD.Instance) HUD.Instance.ShowSubtitle("Bóng va vào vật neo và đứng lại.", 3f, false);
                }
                else if (!WaitingForKhang)
                {
                    _play = np; _idx = ni; Position = next;
                    if (_idx >= _path.Count - 1) { Finished = true; Debug.Log("[Anh vong] Het quy dao"); }
                }
            }
            float vx = (Position.x - prev.x) / dt;
            _speed = Mathf.Lerp(_speed, Mathf.Abs(vx), 1f - Mathf.Exp(-15f * dt));
            if (Mathf.Abs(vx) > 0.05f) _yaw = vx > 0 ? 90f : -90f;
            Place(Position, _speed);
        }

        /// <summary>Quy dao con lai co dam vao vat neo nao khong (de chot ket o Man 5).</summary>
        public bool PathBlockedAhead()
        {
            if (!Active) return false;
            if (BlockedByAnchor) return true;
            var keep = Position;
            for (int i = _idx; i < _path.Count; i += 2)
            {
                var f = FeetAt(i);
                if (AnchorHit(f) != null) { Position = keep; return true; }
                Position = f;
            }
            Position = keep;
            return false;
        }

        /// <summary>Man 5 sau khi giao thu: bong di nhanh hon de ket thuc quy dao.</summary>
        public float playbackBoost = 1f;

        static bool Overlaps(Vector3 a, Vector3 b, float dx)
            => Mathf.Abs(a.x - b.x) < dx && Mathf.Abs(a.z - b.z) < 0.9f && Mathf.Abs(a.y - b.y) < 1.6f;

        HoldableItem AnchorHit(Vector3 feet)
        {
            // Hop kiem tra keo xuong duoi chan 1,3 m: bong lap lai ca cu nhay cua Khang nhung VAN bi vat neo chan
            // (quy dao "dam vao" vat neo theo phuong ngang => dung lai).
            var gb = new Bounds(feet + Vector3.up * 0.2f, new Vector3(0.6f, 3.0f, 0.6f));
            foreach (var it in HoldableItem.All)
            {
                if (!it || it.State != HoldableItem.ItemState.Anchored || it.IsReturning) continue;
                var r = it.GetComponentInChildren<Renderer>();
                if (!r) continue;
                var b = r.bounds; b.Expand(-0.04f);
                // chi chan khi bong dang tien VAO vat (khong chan neu da nam san ben trong)
                var cur = new Bounds(Position + Vector3.up * 0.2f, gb.size);
                if (b.Intersects(gb) && !b.Intersects(cur)) return it;
            }
            return null;
        }

        void Place(Vector3 feet, float speed)
        {
            transform.position = feet;
            if (visual)
            {
                visual.position = feet;
                visual.rotation = Quaternion.Euler(0f, _yaw, 0f);
            }
            if (animator && animator.isActiveAndEnabled)
            {
                animator.SetFloat("Speed", speed);
                animator.speed = speed < 0.05f ? 1f : Mathf.Clamp(speed / 2.8f, 0.4f, 2.5f);
            }
        }

        void Show(bool on)
        {
            if (visual && visual.gameObject.activeSelf != on) visual.gameObject.SetActive(on);
            if (solid) solid.enabled = on && !_graceOverlap;
        }
    }
}
