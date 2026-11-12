using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

namespace EchoShade
{
    /// <summary>
    /// Thanh thoi gian mong: phan da di, vach su kien (LevelData.marks), chu thoi gian / huong / so lan tua.
    /// Hieu ung tua nguoc: man hinh phu mau xanh lanh khi thoi gian chay nguoc.
    /// Esc: tam dung (Time.timeScale = 0). Khi tam dung: M = ve menu chinh.
    /// </summary>
    public class TimeBarUI : MonoBehaviour
    {
        public LevelClock clock;
        [Tooltip("RectTransform cua phan 'fill'; anchorMax.x = ty le thoi gian.")]
        public RectTransform fill;
        public Image fillImage;
        public TMP_Text label;
        public GameObject pausePanel;
        public string menuScene = "MainMenu";

        public Color forwardColor = new Color(0.35f, 0.85f, 1f);
        public Color backwardColor = new Color(1f, 0.55f, 0.25f);
        public Color frozenColor = new Color(0.8f, 0.8f, 0.8f);

        bool _paused;
        int _lastTimeCs = int.MinValue, _lastRewindDs = int.MinValue, _lastCount = int.MinValue;
        bool _lastQualified;
        TimeDirection _lastDir;
        Image _rewindTint;
        float _tintA;

        void Start()
        {
            if (!clock) clock = LevelClock.Instance ? LevelClock.Instance : FindAnyObjectByType<LevelClock>();
            if (!pausePanel) { var p = transform.Find("PausePanel"); if (p) pausePanel = p.gameObject; }
            if (pausePanel) pausePanel.SetActive(false);
            BuildMarks();
            var go = new GameObject("RewindTint", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            go.transform.SetAsFirstSibling();
            var rt = (RectTransform)go.transform; rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            _rewindTint = go.GetComponent<Image>(); _rewindTint.raycastTarget = false; _rewindTint.color = new Color(0.3f, 0.6f, 1f, 0f);
        }

        void BuildMarks()
        {
            var lr = LevelRunner.Instance;
            if (!lr || !lr.data || !fill || !clock) return;
            var bar = fill.parent as RectTransform;
            foreach (var m in lr.data.marks)
            {
                float u = Mathf.Clamp01(m.time / clock.duration);
                var go = new GameObject("Mark_" + m.label, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(bar, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = new Vector2(u, 0f); rt.anchorMax = new Vector2(u, 1f); rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(4f, 10f); rt.anchoredPosition = Vector2.zero;
                var img = go.GetComponent<Image>(); img.raycastTarget = false;
                img.color = m.lane == 0 ? new Color(1f, 0.85f, 0.4f, 0.9f) : m.lane == 1 ? new Color(0.6f, 0.8f, 1f, 0.9f) : new Color(1f, 1f, 1f, 0.8f);
            }
        }

        void Update()
        {
            if (KhangInput.Pause)
            {
                _paused = !_paused;
                Time.timeScale = _paused ? 0f : 1f;
                if (pausePanel) pausePanel.SetActive(_paused);
            }
            if (_paused && KhangInput.Menu && Application.CanStreamedLevelBeLoaded(menuScene))
            {
                Time.timeScale = 1f; _paused = false;
                SceneManager.LoadScene(menuScene);
                return;
            }
            if (!clock) return;

            if (fill)
            {
                var max = fill.anchorMax;
                max.x = clock.Normalized;
                fill.anchorMax = max;
            }

            string dir;
            Color c;
            switch (clock.Direction)
            {
                case TimeDirection.Forward: dir = ">>"; c = forwardColor; break;
                case TimeDirection.Backward: dir = "<<"; c = backwardColor; break;
                default: dir = "||"; c = frozenColor; break;
            }
            if (fillImage) fillImage.color = c;

            if (_rewindTint)
            {
                float want = clock.Direction == TimeDirection.Backward ? (clock.CurrentRewindQualified ? 0.16f : 0.09f) : 0f;
                _tintA = Mathf.MoveTowards(_tintA, want, Time.unscaledDeltaTime * 0.6f);
                _rewindTint.color = new Color(0.3f, 0.6f, 1f, _tintA);
            }

            int timeCs = Mathf.RoundToInt(clock.CurrentTime * 100f);
            int rewindDs = Mathf.RoundToInt(clock.CurrentRewindAmount * 10f);
            bool changed = timeCs != _lastTimeCs || rewindDs != _lastRewindDs || clock.RewindCount != _lastCount
                || clock.CurrentRewindQualified != _lastQualified || clock.Direction != _lastDir;
            if (label && changed)
            {
                _lastTimeCs = timeCs; _lastRewindDs = rewindDs; _lastCount = clock.RewindCount;
                _lastQualified = clock.CurrentRewindQualified; _lastDir = clock.Direction;
                string rewind = clock.CurrentRewindAmount > 0f
                    ? $"   lùi {clock.CurrentRewindAmount:0.0}s{(clock.CurrentRewindQualified ? " (đủ để tính)" : "")}"
                    : "";
                label.text = $"{dir}  G{clock.CurrentTime:0.0} / {clock.duration:0}s     Lần tua: {clock.RewindCount}{rewind}";
            }
        }

        void OnDisable() { if (_paused) Time.timeScale = 1f; }
    }
}
