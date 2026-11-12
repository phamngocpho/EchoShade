using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace EchoShade
{
    /// <summary>
    /// HUD (v2 vai tro D): tim (mau), bac NHIP (mau mat: xanh/vang/cam/do), dong muc tieu,
    /// phu de thoai NHIP, dong goi y, bang hoan thanh man. Chi nghe su kien, khong goi nguoc vao gameplay.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        public Image[] hearts;
        public TMP_Text tierText;
        public TMP_Text objectiveText;
        public TMP_Text subtitleText;
        public TMP_Text bannerText;
        [Tooltip("Dong goi y (tuy chon). Khong co thi dung phu de.")]
        public TMP_Text hintText;

        [TextArea] public string objective = "Mang móc qua hẻm trước khi càng máy lấy mất.";
        [TextArea] public string startLine = "Ca giao đã trễ. Lịch không có chỗ cho thư đến muộn. Hãy quay lại vạch bắt đầu.";
        public float startLineDuration = 7f;
        [TextArea] public string tierUpLine = "Đã ghi nhận. Tôi nhớ thêm một bước của anh.";
        [Tooltip("Loi thoai rieng cho tung bac moi (index = bac). De trong = dung tierUpLine.")]
        public string[] tierUpLines;

        public Color heartOn = new Color(1f, 0.25f, 0.3f);
        public Color heartOff = new Color(1f, 1f, 1f, 0.15f);
        [Tooltip("Khong dung nua (v2 dung NhipBrain.TierColors). Giu de tuong thich scene cu.")]
        public Color[] tierColors;

        float _subUntil, _hintUntil;
        float _tierFlashUntil;
        NhipBrain _brain;
        int _lastTier = int.MinValue, _lastCap = int.MinValue;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }

        void Start()
        {
            SetObjective(objective);
            if (bannerText) bannerText.gameObject.SetActive(false);
            if (hintText) hintText.gameObject.SetActive(false);
            ShowSubtitle(startLine, startLineDuration, true);
            _brain = NhipBrain.Instance;
            if (_brain) _brain.OnTierChanged += OnTier;
        }

        public void SetObjective(string text)
        {
            objective = text;
            if (objectiveText) objectiveText.text = "Mục tiêu: " + text;
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
            if (!subtitleText || string.IsNullOrEmpty(line)) return;
            subtitleText.text = fromNhip ? "NHỊP: “" + line + "”" : line;
            subtitleText.gameObject.SetActive(true);
            _subUntil = Time.unscaledTime + seconds;
        }

        public void ShowHint(string line, float seconds)
        {
            if (!hintText) { ShowSubtitle("Gợi ý: " + line, seconds, false); return; }
            hintText.text = "Gợi ý: " + line;
            hintText.gameObject.SetActive(true);
            _hintUntil = Time.unscaledTime + seconds;
        }

        public void ShowBanner(string text)
        {
            if (!bannerText) return;
            bannerText.text = text;
            bannerText.gameObject.SetActive(true);
        }

        public string CurrentSubtitle => subtitleText && subtitleText.gameObject.activeSelf ? subtitleText.text : "";
        public string CurrentHint => hintText && hintText.gameObject.activeSelf ? hintText.text : "";

        void Update()
        {
            var ph = PlayerHealth.Instance;
            if (ph && hearts != null)
                for (int i = 0; i < hearts.Length; i++)
                    if (hearts[i]) hearts[i].color = i < ph.HP ? heartOn : heartOff;

            if (tierText && _brain)
            {
                int t = _brain.Tier;
                if (t != _lastTier || _brain.tierCap != _lastCap)
                {
                    _lastTier = t; _lastCap = _brain.tierCap;
                    tierText.text = $"NHỊP · bậc {t}/{_brain.tierCap} ({NhipBrain.TierColorName(t)})";
                }
                Color c = NhipBrain.TierColor(t);
                if (Time.unscaledTime < _tierFlashUntil && Mathf.Repeat(Time.unscaledTime * 6f, 1f) > 0.5f) c = Color.white;
                tierText.color = c;
                tierText.fontSize = Time.unscaledTime < _tierFlashUntil ? 38 : 30;
            }

            if (subtitleText && subtitleText.gameObject.activeSelf && Time.unscaledTime > _subUntil)
                subtitleText.gameObject.SetActive(false);
            if (hintText && hintText.gameObject.activeSelf && Time.unscaledTime > _hintUntil)
                hintText.gameObject.SetActive(false);
        }
    }
}
