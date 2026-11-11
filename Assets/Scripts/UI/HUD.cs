using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace EchoShade
{
    /// <summary>HUD: tim (HP), cap do NHIP, dong muc tieu, phu de loi thoai NHIP, bang hoan thanh man.</summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        public Image[] hearts;
        public TMP_Text tierText;
        public TMP_Text objectiveText;
        public TMP_Text subtitleText;
        public TMP_Text bannerText;

        [TextArea] public string objective = "Mang móc qua hẻm trước khi càng máy lấy mất.";
        [TextArea] public string startLine = "Ca giao đã trễ. Lịch không có chỗ cho thư đến muộn. Hãy quay lại vạch bắt đầu.";
        public float startLineDuration = 7f;
        [TextArea] public string tierUpLine = "Đã ghi nhận. Lần này càng máy sẽ đến sớm hơn.";
        [Tooltip("Loi thoai rieng cho tung tier moi (index = tier). De trong = dung tierUpLine.")]
        public string[] tierUpLines;

        public Color heartOn = new Color(1f, 0.25f, 0.3f);
        public Color heartOff = new Color(1f, 1f, 1f, 0.15f);
        public Color[] tierColors = { new Color(0.4f, 0.85f, 1f), new Color(1f, 0.3f, 0.2f), new Color(1f, 0f, 0.6f) };

        float _subUntil;
        float _tierFlashUntil;
        NhipBrain _brain;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            if (objectiveText) objectiveText.text = "Mục tiêu: " + objective;
            if (bannerText) bannerText.gameObject.SetActive(false);
            ShowSubtitle(startLine, startLineDuration, true);
            _brain = NhipBrain.Instance;
            if (_brain) _brain.OnTierChanged += OnTier;
        }

        void OnTier(int oldTier, int newTier)
        {
            _tierFlashUntil = Time.unscaledTime + 2f;
            if (newTier > oldTier)
            {
                string line = tierUpLines != null && newTier < tierUpLines.Length && !string.IsNullOrEmpty(tierUpLines[newTier])
                    ? tierUpLines[newTier] : tierUpLine;
                ShowSubtitle(line, 4f, true);
            }
        }

        public void ShowSubtitle(string line, float seconds, bool fromNhip)
        {
            if (!subtitleText) return;
            subtitleText.text = fromNhip ? "NHỊP: “" + line + "”" : line;
            subtitleText.gameObject.SetActive(true);
            _subUntil = Time.unscaledTime + seconds;
        }

        public void ShowBanner(string text)
        {
            if (!bannerText) return;
            bannerText.text = text;
            bannerText.gameObject.SetActive(true);
        }

        public string CurrentSubtitle => subtitleText && subtitleText.gameObject.activeSelf ? subtitleText.text : "";

        void Update()
        {
            var ph = PlayerHealth.Instance;
            if (ph && hearts != null)
                for (int i = 0; i < hearts.Length; i++)
                    if (hearts[i]) hearts[i].color = i < ph.HP ? heartOn : heartOff;

            if (tierText && _brain)
            {
                int t = _brain.Tier;
                tierText.text = $"NHỊP cấp độ: {t} / {_brain.tierCap}";
                Color c = tierColors[Mathf.Clamp(t, 0, tierColors.Length - 1)];
                if (Time.unscaledTime < _tierFlashUntil && Mathf.Repeat(Time.unscaledTime * 6f, 1f) > 0.5f) c = Color.white;
                tierText.color = c;
                tierText.fontSize = Time.unscaledTime < _tierFlashUntil ? 40 : 32;
            }

            if (subtitleText && subtitleText.gameObject.activeSelf && Time.unscaledTime > _subUntil)
                subtitleText.gameObject.SetActive(false);
        }
    }
}
