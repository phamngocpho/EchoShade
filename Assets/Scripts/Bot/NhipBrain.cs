using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Mot dong trong "bang ghi nho" cua NHIP. Moi lan Khang tua hop le, NHIP ghi lai:
    /// khu vuc, lan Khang dung, vi tri dung (diem quay dau), co vat neo nao da duoc dat chua.
    /// Man 1 chi ghi; cac man sau se dung bang nay de doan truoc hanh vi cua Khang.
    /// </summary>
    [Serializable]
    public class NhipMemoryEntry
    {
        public string zone;
        public int lane;          // 0 = gan, 1 = xa
        public float stopX;       // vi tri X noi Khang quay dau
        public float stopTime;    // thoi gian man tai diem quay dau
        public bool anchorPlaced; // luc do co vat neo nao da duoc dat xuong chua
        public int rewindIndex;   // lan tua hop le thu may
    }

    /// <summary>
    /// "Nao" cua bot NHIP: cap do (tier).
    ///  - Tier bat dau = entryTier (cap khi vao man), toi da = tierCap (Man 1: cap = 1).
    ///  - Moi lan tua hop le (LevelClock.OnValidRewind) ma Khang da tung vuot qua
    ///    tierUpRequiresTimeReached (Man 1: t = 6 s, luc cang may lay moc) => tier +1.
    ///    Y nghia: NHIP "nho" la Khang da quay lai sua chuyen, nen lan sau no den som hon.
    ///  - Khang het mau va hoi sinh => tier -1 nhung khong duoi entryTier.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class NhipBrain : MonoBehaviour
    {
        public static NhipBrain Instance { get; private set; }

        public LevelClock clock;
        public KhangController khang;
        public string zoneName = "Level1";
        public int entryTier = 0;
        public int tierCap = 1;
        [Tooltip("Chi tang tier khi Khang da tung dat toi thoi diem nay (giay) truoc khi tua.")]
        public float tierUpRequiresTimeReached = 0f;

        public int Tier { get; private set; }
        /// <summary>Tier cao nhat da dat trong man nay (GameFlow cong don qua cac man).</summary>
        public int MaxTierReached { get; private set; }
        /// <summary>(tierCu, tierMoi)</summary>
        public event Action<int, int> OnTierChanged;

        [Header("Bang ghi nho (chi doc)")]
        public List<NhipMemoryEntry> memory = new List<NhipMemoryEntry>();

        void Awake()
        {
            Instance = this;
            Tier = entryTier;
            MaxTierReached = Tier;
        }

        // ---- Quan sat lan theo khu vuc (Man 2+) ----
        // NHIP theo doi Khang dung lan nao trong mot khu vuc thoi gian (vd: Man 2, t = 4..8).
        // Quan sat chi la "tam"; khi Khang tua hop le, NHIP CHOT quan sat vao bang ghi nho.
        // Nghia la: NHIP nho dong thoi gian ma Khang vua xoa, va dung no de phuc kich o lan sau.
        readonly Dictionary<string, int> _observedLane = new Dictionary<string, int>();

        public void ObserveLane(string zone, int lane) { _observedLane[zone] = lane; }

        /// <summary>Lan Khang dung gan nhat trong khu vuc (lay tu bang ghi nho), mac dinh = fallback.</summary>
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
            Remember(count);
            foreach (var kv in _observedLane)
            {
                memory.Add(new NhipMemoryEntry
                {
                    zone = kv.Key, lane = kv.Value,
                    stopX = khang ? khang.transform.position.x : 0f,
                    stopTime = clock ? clock.CurrentTime : 0f,
                    anchorPlaced = false, rewindIndex = count
                });
                Debug.Log($"[NHIP] Ghi nho khu {kv.Key}: Khang da di lan {(kv.Value == 0 ? "GAN" : "XA")}");
            }
            _observedLane.Clear();
            if (clock.MaxTimeReached >= tierUpRequiresTimeReached) RaiseTier();
            else Debug.Log($"[NHIP] Tua hop le #{count} nhung chua qua moc t={tierUpRequiresTimeReached}s: khong tang cap.");
        }

        void Remember(int count)
        {
            bool anchor = false;
            foreach (var it in HoldableItem.All)
                if (it && it.State == HoldableItem.ItemState.Anchored && it.AnchorTime >= 0f) { anchor = true; break; }
            var e = new NhipMemoryEntry
            {
                zone = zoneName,
                lane = khang ? (int)khang.CurrentLane : 0,
                stopX = khang ? khang.transform.position.x : 0f,
                stopTime = clock ? clock.CurrentTime : 0f,
                anchorPlaced = anchor,
                rewindIndex = count
            };
            memory.Add(e);
            Debug.Log($"[NHIP] Ghi nho: zone={e.zone} lane={e.lane} stopX={e.stopX:0.0} t={e.stopTime:0.00} anchor={e.anchorPlaced}");
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

        /// <summary>Goi khi Khang het mau va hoi sinh o hom thu.</summary>
        public void LowerTierOnRespawn()
        {
            SetTier(Mathf.Max(entryTier, Tier - 1));
        }

        void SetTier(int t)
        {
            if (t == Tier) return;
            int old = Tier;
            Tier = t;
            if (Tier > MaxTierReached) MaxTierReached = Tier;
            Debug.Log($"[NHIP] Tier {old} -> {Tier}");
            OnTierChanged?.Invoke(old, Tier);
        }
    }
}
