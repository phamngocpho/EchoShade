using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// BANG PATTERN CUA NHIP (v3.1 muc 3.3 + 7). Ngoai bac, NHIP ghi nhan thoi quen lap lai:
    ///  - Lan duoc dung nhieu nhat: doan bi tua (KhangTrail) co >= laneShare mau o cung mot lan,
    ///    lap lai sameLaneRewinds lan tua lien tiep => mot cau thoai.
    ///  - Dat neo cung cho: dat vat neo lan thu sameSpotPlacements trong ban kinh spotRadius (cung lan) => mot cau thoai.
    /// CHI DUNG DE CHON THOAI: khong doi bac, khong doi don danh, khong doi luat. Moi pattern noi toi da 1 lan / man.
    /// </summary>
    [DefaultExecutionOrder(-85)]
    public class NhipPattern : MonoBehaviour
    {
        public static NhipPattern Instance { get; private set; }

        public NhipBrain brain;
        [Range(0.5f, 1f)] public float laneShare = 0.7f;
        public int sameLaneRewinds = 2;
        public int sameSpotPlacements = 3;
        public float spotRadius = 1.2f;
        [Tooltip("Giay thuc cho truoc khi noi (de khong de len thoai len bac).")]
        public float lineDelay = 4.2f;

        [TextArea] public string nearLaneLine = "Lại là làn gần. Anh đi làn này như thể nó là đường về nhà.";
        [TextArea] public string farLaneLine = "Lại là làn xa. Anh nghĩ tôi không nhìn tới chỗ đó sao?";
        [TextArea] public string anchorLine = "Lần thứ ba anh đặt neo đúng chỗ này. Tôi bắt đầu nghĩ chỗ đó có ý nghĩa với anh.";

        /// <summary>Lan thoi quen (-1 = chua co). Chi de doc / thong ke.</summary>
        public int LaneHabit { get; private set; } = -1;
        public int LaneStreak { get; private set; }
        public int LastDominantLane { get; private set; } = -1;
        public int SpotRepeats { get; private set; }
        public string LastLine { get; private set; } = "";
        public int LinesSaid { get; private set; }

        readonly bool[] _laneSaid = new bool[2];
        bool _anchorSaid;
        readonly List<Vector3> _placements = new List<Vector3>();

        void Awake() { Instance = this; if (!brain) brain = GetComponent<NhipBrain>(); }

        void Start()
        {
            if (!brain) brain = NhipBrain.Instance;
            if (brain) brain.OnRewindProcessed += OnRewind;
            HoldableItem.OnPlaced += OnPlaced;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (brain) brain.OnRewindProcessed -= OnRewind;
            HoldableItem.OnPlaced -= OnPlaced;
        }

        // Dem thoi gian thuc Khang DI TOI (thoi gian tien) o moi lan, tu lan tua truoc toi lan tua nay.
        readonly float[] _laneTime = new float[2];
        KhangController _k;

        void Update()
        {
            var clock = LevelClock.Instance;
            if (!clock || clock.Direction != TimeDirection.Forward) return;
            if (!_k) { var ph = PlayerHealth.Instance; _k = ph ? ph.GetComponent<KhangController>() : null; if (!_k) return; }
            _laneTime[_k.CurrentLane == KhangController.Lane.Near ? 0 : 1] += Time.deltaTime;
        }

        void OnRewind(List<TrailSample> seg)
        {
            float near = _laneTime[0], far = _laneTime[1], n = near + far;
            _laneTime[0] = _laneTime[1] = 0f;
            int dom = -1;
            if (n >= 0.75f) { if (near >= laneShare * n) dom = 0; else if (far >= laneShare * n) dom = 1; }
            LaneStreak = dom >= 0 && dom == LastDominantLane ? LaneStreak + 1 : (dom >= 0 ? 1 : 0);
            LastDominantLane = dom;
            GameLog.Info($"[NHIP/pattern] Doan tua: gan={near:0.0}s xa={far:0.0}s => lan troi={(dom < 0 ? "-" : dom == 0 ? "GAN" : "XA")} chuoi={LaneStreak}");
            if (dom >= 0 && LaneStreak >= sameLaneRewinds && !_laneSaid[dom])
            {
                _laneSaid[dom] = true; LaneHabit = dom;
                Say(dom == 0 ? nearLaneLine : farLaneLine);
            }
        }

        void OnPlaced(HoldableItem it)
        {
            if (!it) return;
            Vector3 p = it.transform.position;
            int same = 0;
            foreach (var q in _placements) if (Mathf.Abs(q.x - p.x) <= spotRadius && Mathf.Abs(q.z - p.z) < 0.8f && Mathf.Abs(q.y - p.y) < 1.5f) same++;
            _placements.Add(p);
            SpotRepeats = Mathf.Max(SpotRepeats, same + 1);
            if (same + 1 >= sameSpotPlacements && !_anchorSaid)
            {
                _anchorSaid = true;
                Say(anchorLine);
            }
        }

        void Say(string line)
        {
            LastLine = line; LinesSaid++;
            GameLog.Info("[NHIP/pattern] " + line);
            StartCoroutine(SayLater(line));
        }

        IEnumerator SayLater(string line)
        {
            yield return new WaitForSecondsRealtime(lineDelay);
            if (HUD.Instance) HUD.Instance.ShowSubtitle(line, 4.5f, true);
        }
    }
}
