using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Mot dong trong "bang nho" cua NHIP (v2 muc 3.4). Moi lan Khang tua hop le, NHIP ghi cho moi vung
    /// ma doan vua bi tua di qua: lan Khang chu yeu dung, cho dung im lau nhat va thoi gian dung, vat neo trong vung.
    /// (Cot "vat dang cam" cua v2 khong duoc lam vi khong man nao doc no.)
    /// </summary>
    [Serializable]
    public class NhipMemoryEntry
    {
        public string zone;
        public int lane;          // 0 = gan, 1 = xa
        public float stopX;       // vi tri X noi Khang dung im lau nhat (hoac quay dau)
        public float stopTime;    // thoi gian man tai cho dung
        public float stopSeconds; // so giay thuc dung im o do
        public bool anchorPlaced; // co vat neo (do Khang dat) trong vung khong
        public float anchorX;
        public int rewindIndex;   // lan tua hop le thu may
    }

    /// <summary>
    /// "Nao" cua bot NHIP: bac (tier) + bang nho. Khong biet gi ve tung man; man doc bac va bang nho.
    ///  - Bac luc vao man = entryTier, tran = tierCap (LevelData).
    ///  - Moi lan tua hop le (LevelClock.OnValidRewind): ghi bang nho tu doan vua bi tua (KhangTrail), +1 bac.
    ///  - Het mau / tu ve thung thu: bac -1 (khong duoi entryTier) va QUEN cac dong cua lan tua gan nhat.
    ///  Bac 0 xanh: tuan tra theo lich. Bac 1 vang: don den som ~1 giay man. Bac 2 cam: phuc kich 1 diem
    ///  theo dong ghi gan nhat (co vong do bao truoc). Bac 3 do: goi Anh vong (GhostPlayer), man 5 them tia khoa.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class NhipBrain : MonoBehaviour
    {
        public static NhipBrain Instance { get; private set; }

        public static readonly Color[] TierColors =
        {
            new Color(0.25f, 0.75f, 1f),   // 0 xanh
            new Color(1f, 0.88f, 0.2f),    // 1 vang
            new Color(1f, 0.5f, 0.08f),    // 2 cam
            new Color(1f, 0.08f, 0.06f),   // 3 do
        };
        public static Color TierColor(int t) => TierColors[Mathf.Clamp(t, 0, TierColors.Length - 1)];
        public static string TierColorName(int t) => t <= 0 ? "xanh" : t == 1 ? "vàng" : t == 2 ? "cam" : "đỏ";

        public LevelClock clock;
        public KhangController khang;
        public string zoneName = "Level1";
        public int entryTier = 0;
        public int tierCap = 1;
        [Tooltip("v1: chi tang bac khi Khang da tung toi thoi diem nay. v2 dat 0.")]
        public float tierUpRequiresTimeReached = 0f;
        [HideInInspector] public LevelData.Zone[] zones;

        public int Tier { get; private set; }
        public int MaxTierReached { get; private set; }
        public int ForgetCount { get; private set; }
        /// <summary>(bacCu, bacMoi)</summary>
        public event Action<int, int> OnTierChanged;
        /// <summary>Sau khi da ghi bang nho + doi bac cho mot lan tua hop le. Tham so = doan di tien vua bi tua.</summary>
        public event Action<List<TrailSample>> OnRewindProcessed;
        /// <summary>Khi quen (het mau / ve thung thu).</summary>
        public event Action OnForget;

        [Header("Bang nho (chi doc)")]
        public List<NhipMemoryEntry> memory = new List<NhipMemoryEntry>();

        void Awake()
        {
            Instance = this;
            Tier = entryTier;
            MaxTierReached = Tier;
        }

        public void ResetTierToEntry() { Tier = entryTier; MaxTierReached = Tier; }

        // ---- Quan sat lan theo khu vuc (Man 2 - NhipLaneWatcher, giu tu v1) ----
        readonly Dictionary<string, int> _observedLane = new Dictionary<string, int>();
        public void ObserveLane(string zone, int lane) { _observedLane[zone] = lane; }

        /// <summary>Lan Khang dung trong vung o dong ghi gan nhat; khong co thi = fallback.</summary>
        public int RecallLane(string zone, int fallback)
        {
            var e = RecallLatest(zone);
            return e != null ? e.lane : fallback;
        }

        void Start()
        {
            if (!clock) clock = LevelClock.Instance;
            if (!khang) khang = FindAnyObjectByType<KhangController>();
            if (clock) clock.OnValidRewind += HandleValidRewind;
        }

        void OnDestroy()
        {
            if (clock) clock.OnValidRewind -= HandleValidRewind;
            if (Instance == this) Instance = null;
        }

        void HandleValidRewind(int count)
        {
            var trail = KhangTrail.Instance;
            var seg = trail ? trail.ExtractRewoundSegment(clock.LastRewindFrom, clock.LastRewindTo) : new List<TrailSample>();
            RecordZones(seg, count);
            foreach (var kv in _observedLane)
                AddEntry(new NhipMemoryEntry { zone = kv.Key, lane = kv.Value, stopX = khang ? khang.transform.position.x : 0f, stopTime = clock.CurrentTime, rewindIndex = count });
            _observedLane.Clear();
            if (clock.MaxTimeReached >= tierUpRequiresTimeReached) RaiseTier();
            OnRewindProcessed?.Invoke(seg);
        }

        void RecordZones(List<TrailSample> seg, int count)
        {
            if (zones == null || seg.Count == 0) return;
            foreach (var z in zones)
            {
                int near = 0, far = 0;
                float bestStill = 0f, still = 0f, stillX = 0f, stillT = 0f, bestX = 0f, bestT = 0f;
                for (int i = 0; i < seg.Count; i++)
                {
                    var s = seg[i];
                    if (s.t < z.fromTime || s.t > z.toTime) { still = 0f; continue; }
                    if (s.lane == 0) near++; else far++;
                    if (i > 0 && (s.pos - seg[i - 1].pos).sqrMagnitude < 1e-6f)
                    {
                        if (still == 0f) { stillX = s.pos.x; stillT = s.t; }
                        still += s.rt - seg[i - 1].rt;
                        if (still > bestStill) { bestStill = still; bestX = stillX; bestT = stillT; }
                    }
                    else still = 0f;
                }
                if (near + far < 15) continue; // it hon ~0,5 s trong vung: khong du de ghi
                var e = new NhipMemoryEntry { zone = z.name, lane = far > near ? 1 : 0, stopSeconds = bestStill, stopX = bestX, stopTime = bestT, rewindIndex = count };
                foreach (var it in HoldableItem.All)
                {
                    if (!it || it.State != HoldableItem.ItemState.Anchored || it.AnchorTime < 0f || !clock) continue;
                    float at = clock.XToTime(it.transform.position.x);
                    if (at >= z.fromTime && at <= z.toTime) { e.anchorPlaced = true; e.anchorX = it.transform.position.x; }
                }
                AddEntry(e);
            }
        }

        void AddEntry(NhipMemoryEntry e)
        {
            memory.Add(e);
            Debug.Log($"[NHIP] Ghi nho #{e.rewindIndex}: vung={e.zone} lan={(e.lane == 0 ? "GAN" : "XA")} dung={e.stopSeconds:0.0}s@x={e.stopX:0.0} neo={e.anchorPlaced}");
        }

        public NhipMemoryEntry RecallLatest(string zone)
        {
            for (int i = memory.Count - 1; i >= 0; i--) if (memory[i].zone == zone) return memory[i];
            return null;
        }

        public void RaiseTier()
        {
            if (Tier >= tierCap) return;
            SetTier(Tier + 1);
        }

        /// <summary>Het mau / tu ve thung thu: bac -1 (khong duoi bac vao man).</summary>
        public void LowerTierOnRespawn()
        {
            SetTier(Mathf.Max(entryTier, Tier - 1));
        }

        /// <summary>Quen cac dong cua lan tua gan nhat (quyet dinh NHIP vua hoc).</summary>
        public void ForgetLatest()
        {
            if (memory.Count > 0)
            {
                int last = memory[memory.Count - 1].rewindIndex;
                int n = memory.RemoveAll(m => m.rewindIndex == last);
                Debug.Log($"[NHIP] Quen {n} dong cua lan tua #{last}");
            }
            ForgetCount++;
            OnForget?.Invoke();
        }

        void SetTier(int t)
        {
            if (t == Tier) return;
            int old = Tier;
            Tier = t;
            if (Tier > MaxTierReached) MaxTierReached = Tier;
            Debug.Log($"[NHIP] Bac {old} -> {Tier}");
            OnTierChanged?.Invoke(old, Tier);
        }

        /// <summary>Chi cho test/cong cu: dat bac truc tiep.</summary>
        public void DebugSetTier(int t) => SetTier(Mathf.Clamp(t, 0, 3));
    }
}
