using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace EchoShade
{
    /// <summary>
    /// Thanh thoi gian mong + chu: thoi gian hien tai, huong, so lan tua hop le.
    /// Esc: tam dung (Time.timeScale = 0).
    /// </summary>
    public class TimeBarUI : MonoBehaviour
    {
        public LevelClock clock;
        [Tooltip("RectTransform cua phan 'fill'; anchorMax.x = ty le thoi gian.")]
        public RectTransform fill;
        public Image fillImage;
        public TMP_Text label;
        public GameObject pausePanel;

        public Color forwardColor = new Color(0.35f, 0.85f, 1f);
        public Color backwardColor = new Color(1f, 0.55f, 0.25f);
        public Color frozenColor = new Color(0.8f, 0.8f, 0.8f);

        bool _paused;
        // Chi dung lai chuoi khi gia tri hien thi thay doi (tranh cap phat bo nho moi frame).
        int _lastTimeCs = int.MinValue, _lastRewindDs = int.MinValue, _lastCount = int.MinValue;
        bool _lastQualified;
        TimeDirection _lastDir;

        void Start()
        {
            if (!clock) clock = LevelClock.Instance ? LevelClock.Instance : FindAnyObjectByType<LevelClock>();
            if (pausePanel) pausePanel.SetActive(false);
        }

        void Update()
        {
            if (KhangInput.Pause)
            {
                _paused = !_paused;
                Time.timeScale = _paused ? 0f : 1f;
                if (pausePanel) pausePanel.SetActive(_paused);
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

            int timeCs = Mathf.RoundToInt(clock.CurrentTime * 100f);
            int rewindDs = Mathf.RoundToInt(clock.CurrentRewindAmount * 10f);
            bool changed = timeCs != _lastTimeCs || rewindDs != _lastRewindDs || clock.RewindCount != _lastCount
                || clock.CurrentRewindQualified != _lastQualified || clock.Direction != _lastDir;
            if (label && changed)
            {
                _lastTimeCs = timeCs; _lastRewindDs = rewindDs; _lastCount = clock.RewindCount;
                _lastQualified = clock.CurrentRewindQualified; _lastDir = clock.Direction;
                string rewind = clock.CurrentRewindAmount > 0f
                    ? $"   lùi: {clock.CurrentRewindAmount:0.0}s{(clock.CurrentRewindQualified ? " (hợp lệ)" : "")}"
                    : "";
                label.text = $"{dir}  t = {clock.CurrentTime:0.00}s / {clock.duration:0}s     Tua hợp lệ: {clock.RewindCount}{rewind}";
            }
        }

        void OnDisable() { if (_paused) Time.timeScale = 1f; }
    }
}
