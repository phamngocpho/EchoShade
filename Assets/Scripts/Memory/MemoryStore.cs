using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// KY UC NAM NGOAI THOI GIAN (v3.1 muc 3.1): danh sach Manh nho da nhat trong luot choi.
    /// Static, song qua cac scene; KHONG nam trong ban ghi tua (LevelClock/KhangTrail) nen tua, het mau,
    /// phim R deu khong xoa. Chi xoa khi bat dau luot moi (GameFlow.ResetRun).
    /// </summary>
    public static class MemoryStore
    {
        public const int Total = 10;
        public const int PerLevel = 2;

        static readonly Dictionary<string, int> _collected = new Dictionary<string, int>();
        static readonly List<string> _order = new List<string>();

        /// <summary>Ban ra moi khi nhat them / xoa. Tham so = id vua nhat (null khi xoa).</summary>
        public static event Action<string> OnChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _collected.Clear(); _order.Clear(); OnChanged = null; }

        public static int Count => _collected.Count;
        public static bool AllCollected => _collected.Count >= Total;
        public static IReadOnlyList<string> Order => _order;

        public static bool Has(string id) => !string.IsNullOrEmpty(id) && _collected.ContainsKey(id);

        public static int CountInLevel(int levelIndex)
        {
            int n = 0;
            foreach (var kv in _collected) if (kv.Value == levelIndex) n++;
            return n;
        }

        /// <summary>Tra ve true neu la lan nhat dau tien.</summary>
        public static bool Collect(MemoryFragmentData d)
        {
            if (!d || Has(d.id)) return false;
            _collected[d.id] = d.levelIndex;
            _order.Add(d.id);
            GameLog.Info($"[Manh nho] Nhat {d.id} ({d.displayName}). Tong {Count}/{Total}, man {d.levelIndex}: {CountInLevel(d.levelIndex)}/{PerLevel}");
            OnChanged?.Invoke(d.id);
            return true;
        }

        public static void ResetRun()
        {
            _collected.Clear(); _order.Clear();
            OnChanged?.Invoke(null);
        }

        /// <summary>Chi cho test: dat san so manh (vd. de xem dong thoai ket khi du 10 manh).</summary>
        public static void DebugFill(int levelIndexUpTo, int count)
        {
            for (int l = 1; l <= 5 && Count < count; l++)
                for (int i = 0; i < PerLevel && Count < count; i++)
                {
                    string id = $"L{l}_{(char)('A' + i)}";
                    if (l > levelIndexUpTo || Has(id)) continue;
                    _collected[id] = l; _order.Add(id);
                }
            OnChanged?.Invoke(null);
        }
    }
}
