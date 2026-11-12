using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace EchoShade
{
    /// <summary>
    /// MAN KET (v2 muc 5). Ket duoc quyet dinh boi LUA CHON o Man 5 (GameFlow.EndingVong), khong boi so lan tua:
    ///  - Ket Lang: Anh vong bi neo chan o G25. Thap tat tung vong, bong dung yen roi tan, toa tau treo ha xuong ray.
    ///  - Ket Vong: Anh vong di het quy dao. Thap tat, thanh pho chay lai, mot Khang trong suot van buoc tren san thuong.
    /// Hien: tong so lan tua, bac cao nhat, ten ket + bang "ket ban chua thay". Nut: Choi lai, Thoat.
    /// Khong bao gio sua sharedMaterial (tranh lam ban asset).
    /// </summary>
    public class EndScreen : MonoBehaviour
    {
        public const string LangName = "Kết Lặng";
        public const string VongName = "Kết Vọng";
        public const string LangLine = "Không đủ dữ liệu để chắc. Lần này tôi quên. Anh đi giao nốt những thư còn lại.";
        public const string VongLine = "Thư đã tới. Tôi cũng đã thuộc đường của anh. Lần khác tôi đứng sẵn ở đó.";
        public const string LangDesc = "Ảnh vọng bị thùng neo chặn ở G25. Tháp tắt từng vòng, bóng đứng yên rồi tan. Toa tàu treo trên trời hạ xuống ray, như một lịch mới.";
        public const string VongDesc = "Ảnh vọng đi hết quỹ đạo. Tháp tắt, thành phố chạy lại. Trên sân thượng phố Cũ, một Khang trong suốt vẫn bước đúng ca đêm đó.";

        public TMP_Text titleText, lineText, statsText, descText, unseenText;
        public Button restartButton, quitButton, menuButton;
        public GameObject langRoot, vongRoot;
        public string firstScene = "Level1";
        [Tooltip("Xem truoc trong editor khi khong co GameFlow: true = Ket Vong.")]
        public bool previewVong;

        public string EndingName { get; private set; }
        public string EndingLine { get; private set; }
        public bool IsVong { get; private set; }

        void Start()
        {
            var gf = GameFlow.Instance;
            bool vong = gf && gf.HasEnding ? gf.EndingVong : previewVong;
            Show(gf ? gf.TotalRewinds : 0, gf ? gf.MaxTier : 0, vong);
            if (restartButton) restartButton.onClick.AddListener(Restart);
            if (quitButton) quitButton.onClick.AddListener(Quit);
            if (menuButton) menuButton.onClick.AddListener(() => GameFlow.Ensure().GoMenu());
        }

        public void Show(int rewinds, int maxTier, bool vong)
        {
            IsVong = vong;
            EndingName = vong ? VongName : LangName;
            EndingLine = vong ? VongLine : LangLine;
            if (titleText) titleText.text = EndingName.ToUpperInvariant();
            if (lineText) lineText.text = "NHỊP: “" + EndingLine + "”";
            if (descText) descText.text = vong ? VongDesc : LangDesc;
            if (statsText) statsText.text = $"Tổng số lần tua: {rewinds}     Bậc NHỊP cao nhất: {maxTier}     Kết: {EndingName}";
            if (langRoot) langRoot.SetActive(!vong);
            if (vongRoot) vongRoot.SetActive(vong);
            if (unseenText)
            {
                bool seenL = PlayerPrefs.GetInt("ES_SeenLang", 0) == 1 || !vong;
                bool seenV = PlayerPrefs.GetInt("ES_SeenVong", 0) == 1 || vong;
                if (seenL && seenV) unseenText.text = "Kết bạn chưa thấy: không còn — bạn đã thấy cả hai kết.";
                else if (!seenV) unseenText.text = "Kết bạn chưa thấy: <b>Kết Vọng</b> — ở tầng 2 Tháp Nhịp, đừng đặt thùng vào ô vàng; để Ảnh vọng đi hết đường.";
                else unseenText.text = "Kết bạn chưa thấy: <b>Kết Lặng</b> — ở tầng 2 Tháp Nhịp, đặt thùng vào ô vàng G25 làn gần để chặn Ảnh vọng.";
            }
        }

        public void Restart()
        {
            GameFlow.Ensure().ResetRun();
            Time.timeScale = 1f;
            SceneManager.LoadScene(firstScene);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
