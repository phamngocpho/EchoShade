using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Vat cam duoc (vd: thung go, moc). Ba trang thai (v2 muc 3.2):
    ///  - Timeline : vat theo thoi gian (neu co TimelineObject tren cung GameObject thi doc dong ho). Tua thi ve cho cu.
    ///  - Held     : vat tren tay - roi dong thoi gian, di theo Khang ke ca khi lui. Chi mang mot vat.
    ///  - Anchored : vat neo - dat xuong thi o lai san khau, khong phu thuoc thoi diem, khong bi tua xoa,
    ///               giu nguyen khi ve thung thu. NHIP khong quen vat neo.
    /// LUAT HOI VI (v2 muc 1): vat neo bi pha (vd tau hat) hoac bi day / roi khoi san thi bay ve DIEM GOC
    ///   (noi vat nam truoc khi Khang nhat) kem hieu ung tua nguoc ngan. Khong mat mau, khong tinh lan tua,
    ///   khong doi bac. Neu diem goc dang co Khang hoac bong thi dat sang o trong gan nhat.
    /// </summary>
    public class HoldableItem : MonoBehaviour
    {
        public enum ItemState { Timeline, Held, Anchored }

        public static readonly List<HoldableItem> All = new List<HoldableItem>();

        [Tooltip("Trang thai ban dau. Vat khong co TimelineObject nen de Anchored.")]
        public ItemState startState = ItemState.Anchored;

        [Tooltip("Tat = khong nhat duoc luc nay (vd: moc dang nam trong cang may NHIP).")]
        public bool pickable = true;

        [Tooltip("Vat neo co the dung de Khang nhay qua (Space sat vat).")]
        public bool vaultable = true;

        public ItemState State { get; private set; }
        /// <summary>Thoi gian man luc vat duoc Khang dat xuong lan cuoi (-1 = chua dat / dang o diem goc).</summary>
        public float AnchorTime { get; private set; } = -1f;
        public Vector3 AnchorPosition { get; private set; }
        public Vector3 HomePosition { get; private set; }
        public bool IsReturning { get; private set; }
        public bool AtHome => State == ItemState.Anchored && AnchorTime < 0f && (transform.position - HomePosition).sqrMagnitude < 0.05f;
        public int ReturnCount { get; private set; }

        TimelineObject _timeline;
        Collider[] _colliders;
        Renderer[] _renderers;
        float _fallVy;
        bool _falling;

        void Awake()
        {
            _timeline = GetComponent<TimelineObject>();
            _colliders = GetComponentsInChildren<Collider>(true);
            _renderers = GetComponentsInChildren<Renderer>(true);
            State = startState;
            HomePosition = transform.position;
            if (State == ItemState.Anchored) AnchorPosition = transform.position;
            if (_timeline) _timeline.FollowsClock = State == ItemState.Timeline;
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>Dat diem goc (vd: TrackCrate dung restPosition).</summary>
        public void SetHome(Vector3 p) { HomePosition = p; }

        public void PickUp(Transform holder, Vector3 localOffset)
        {
            if (IsReturning) return;
            _falling = false;
            State = ItemState.Held;
            if (_timeline) _timeline.FollowsClock = false; // ngung doc dong ho
            SetColliders(false); // tranh va cham voi CharacterController cua Khang
            transform.SetParent(holder, true);
            transform.localPosition = localOffset;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Dat xuong tai worldPos (Y duoc dua xuong mat dat). Tro thanh vat neo. Khong co dat ben duoi => roi, roi hoi vi.</summary>
        public void Place(Vector3 worldPos)
        {
            transform.SetParent(null, true);
            float halfH = GetHalfHeight();
            Vector3 origin = new Vector3(worldPos.x, worldPos.y + 1.5f, worldPos.z);
            bool ground = Physics.Raycast(origin, Vector3.down, out var hit, 6f, ~0, QueryTriggerInteraction.Ignore);
            if (ground) worldPos.y = hit.point.y + halfH;
            transform.SetPositionAndRotation(worldPos, Quaternion.identity);

            State = ItemState.Anchored;
            var clock = LevelClock.Instance;
            AnchorTime = clock ? clock.CurrentTime : 0f;
            AnchorPosition = worldPos;
            SetColliders(true);
            if (!ground) { _falling = true; _fallVy = 0f; SetColliders(false); }
            Debug.Log($"[Vat neo] {name} dat tai x={worldPos.x:0.00} z={worldPos.z:0.0} t={AnchorTime:0.00}{(ground ? "" : " (khong co san - roi)")}");
        }

        /// <summary>v1: tra vat ve dong thoi gian goc. v2 dung ReturnHome() thay the.</summary>
        public void ResetToTimeline()
        {
            transform.SetParent(null, true);
            State = ItemState.Timeline;
            AnchorTime = -1f;
            if (_timeline) _timeline.FollowsClock = true;
            SetColliders(true);
        }

        void Update()
        {
            if (State != ItemState.Anchored || IsReturning) return;
            if (_falling)
            {
                _fallVy -= 25f * Time.deltaTime;
                transform.position += Vector3.up * _fallVy * Time.deltaTime;
                if (transform.position.y < AnchorPosition.y - 4f) { _falling = false; ReturnHome("rơi khỏi sân"); }
                return;
            }
            var clock = LevelClock.Instance;
            bool off = transform.position.y < -3f || (clock && (transform.position.x < clock.levelStartX - 1f || transform.position.x > clock.levelEndX + 1f));
            if (off) ReturnHome("bị đẩy khỏi sân");
        }

        /// <summary>LUAT HOI VI: bay ve diem goc (hieu ung tua nguoc ngan), thanh vat neo tai diem goc.</summary>
        public void ReturnHome(string reason)
        {
            if (IsReturning || State == ItemState.Held) return;
            StartCoroutine(ReturnRoutine(reason));
        }

        IEnumerator ReturnRoutine(string reason)
        {
            IsReturning = true;
            ReturnCount++;
            Debug.Log($"[Hoi vi] {name} ve diem goc ({reason}). Khong mat mau, khong tinh lan tua.");
            if (HUD.Instance) HUD.Instance.ShowSubtitle("Hồi vị: " + reason + " — vật quay về chỗ cũ.", 3f, false);
            transform.SetParent(null, true);
            SetColliders(false);
            if (_timeline) _timeline.FollowsClock = false;
            Vector3 target = FreeHomeSpot();
            Vector3 a = transform.position;
            Quaternion ra = transform.rotation;
            var mats = new List<Material>();
            foreach (var r in _renderers) if (r) mats.AddRange(r.materials);
            const float dur = 0.9f;
            for (float u = 0f; u < 1f; u += Time.deltaTime / dur)
            {
                float e = Mathf.SmoothStep(0f, 1f, u);
                transform.position = Vector3.Lerp(a, target, e) + Vector3.up * Mathf.Sin(e * Mathf.PI) * 2.2f;
                transform.rotation = Quaternion.Slerp(ra, Quaternion.identity, e) * Quaternion.Euler(0f, 0f, 360f * (1f - e));
                float glow = Mathf.Sin(u * Mathf.PI) * 2.5f;
                foreach (var m in mats) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(0.35f, 0.75f, 1f) * glow); }
                yield return null;
            }
            foreach (var m in mats) m.SetColor("_EmissionColor", Color.black);
            transform.SetPositionAndRotation(target, Quaternion.identity);
            State = ItemState.Anchored;
            AnchorTime = -1f;
            AnchorPosition = target;
            SetColliders(true);
            IsReturning = false;
        }

        Vector3 FreeHomeSpot()
        {
            Vector3 p = HomePosition;
            for (int i = 0; i < 6; i++)
            {
                if (!Occupied(p)) return p;
                p = HomePosition + Vector3.right * 1.3f * ((i / 2) + 1) * (i % 2 == 0 ? 1f : -1f);
            }
            return HomePosition;
        }

        static bool Occupied(Vector3 p)
        {
            var ph = PlayerHealth.Instance;
            if (ph && Mathf.Abs(ph.transform.position.x - p.x) < 0.9f && Mathf.Abs(ph.transform.position.z - p.z) < 0.9f) return true;
            var g = GhostPlayer.Instance;
            if (g && g.Active && Mathf.Abs(g.Position.x - p.x) < 0.9f && Mathf.Abs(g.Position.z - p.z) < 0.9f) return true;
            return false;
        }

        public float GetHalfHeight()
        {
            var r = GetComponentInChildren<Renderer>();
            return r ? r.bounds.extents.y : 0.5f;
        }

        public Bounds WorldBounds
        {
            get
            {
                var r = GetComponentInChildren<Renderer>();
                return r ? r.bounds : new Bounds(transform.position, Vector3.one * 0.8f);
            }
        }

        void SetColliders(bool on)
        {
            foreach (var c in _colliders) if (c) c.enabled = on;
        }
    }
}
