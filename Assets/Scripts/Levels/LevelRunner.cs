using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Nap LevelData vao man luc bat dau: bac vao man / tran bac cho NhipBrain, chu HUD, vach su kien.
    /// Kiem tra goi y tu dong (v2 3.3): mat mau 3 lan lien tiep trong cung mot vung => hien goi y cua vung.
    /// Phim R: tu ve thung thu (nhu het mau: bac -1, quen quyet dinh gan nhat, Anh vong bien mat).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class LevelRunner : MonoBehaviour
    {
        public static LevelRunner Instance { get; private set; }
        public LevelData data;

        string _lastZone;
        int _streak;

        void Awake()
        {
            Instance = this;
            var brain = FindAnyObjectByType<NhipBrain>();
            if (data && brain)
            {
                brain.entryTier = data.entryTier;
                brain.tierCap = data.tierCap;
                brain.tierUpRequiresTimeReached = 0f;
                brain.zones = data.zones;
                brain.ResetTierToEntry();
            }
            if (brain && !brain.GetComponent<NhipPattern>()) brain.gameObject.AddComponent<NhipPattern>();
            var hud = FindAnyObjectByType<HUD>();
            if (data && hud)
            {
                if (!string.IsNullOrEmpty(data.objective)) hud.objective = data.objective;
                if (!string.IsNullOrEmpty(data.startLine)) hud.startLine = data.startLine;
                if (data.tierUpLines != null && data.tierUpLines.Length > 0) hud.tierUpLines = data.tierUpLines;
            }
        }

        void Start()
        {
            var ph = PlayerHealth.Instance;
            if (ph) ph.OnDamaged += OnDamaged;
            Mailbox.OnLevelComplete += OnLevelComplete;
        }

        /// <summary>Thoai NHIP o cuoi man da phat (v3.1: du 2 Manh nho).</summary>
        public string EndFragmentLine { get; private set; } = "";

        void OnLevelComplete(Mailbox mb)
        {
            if (!data || string.IsNullOrEmpty(data.fragmentsCompleteLine)) return;
            if (MemoryStore.CountInLevel(data.levelIndex) < MemoryStore.PerLevel) return;
            EndFragmentLine = data.fragmentsCompleteLine;
            float delay = mb && !string.IsNullOrEmpty(mb.arrivalLine) ? 2.4f : 1.0f;
            GameFlow.Ensure().extraLoadDelay = delay + 4.6f - GameFlow.Ensure().loadDelay;
            StartCoroutine(SayEnd(delay));
        }

        System.Collections.IEnumerator SayEnd(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            GameLog.Info("[Manh nho] Thoai cuoi man: " + EndFragmentLine);
            if (HUD.Instance) HUD.Instance.ShowSubtitle(EndFragmentLine, 5f, true);
        }

        void OnDestroy()
        {
            Mailbox.OnLevelComplete -= OnLevelComplete;
            if (Instance == this) Instance = null;
            var ph = PlayerHealth.Instance;
            if (ph) ph.OnDamaged -= OnDamaged;
        }

        void Update()
        {
            if (Time.timeScale == 0f || Mailbox.LevelCompleted) return;
            if (KhangInput.Restart && PlayerHealth.Instance) PlayerHealth.Instance.ReturnToMailbox(true);
        }

        void OnDamaged(string source, float t)
        {
            if (!data) return;
            var z = data.ZoneAt(t);
            string name = z != null ? z.name : "";
            if (name == _lastZone) _streak++; else { _lastZone = name; _streak = 1; }
            if (_streak >= 3 && z != null && !string.IsNullOrEmpty(z.hint))
            {
                _streak = 0;
                if (HUD.Instance) HUD.Instance.ShowHint(z.hint, 7f);
            }
        }
    }
}
