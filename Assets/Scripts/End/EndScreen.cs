using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace EchoShade
{
    /// <summary>
    /// Man ket: doc tong so lan tua hop le + tier NHIP cao nhat tu GameFlow va chon ket.
    ///  - <= 8 lan tua: "Ket Lang" (NHIP khong du du lieu, quen Khang).
    ///  - >= 9 lan tua: "Ket Vong" (NHIP da thuoc duong cua Khang).
    /// Nut: Choi lai tu dau (xoa thong ke, nap Level1) / Thoat.
    /// </summary>
    public class EndScreen : MonoBehaviour
    {
        public const string LangName = "Kết Lặng";
        public const string VongName = "Kết Vọng";
        public const string LangLine = "Không đủ dữ liệu để chắc. Lần này tôi quên. Anh đi giao nốt những thư còn lại.";
        public const string VongLine = "Thư đã tới. Tôi cũng đã thuộc đường của anh. Lần khác tôi đứng sẵn ở đó.";

        public TMP_Text titleText, lineText, statsText;
        public Button restartButton, quitButton;
        public int vongFromRewinds = 9;
        [Tooltip("Mat thap NHIP o phong nen: dim khi Ket Lang, do khi Ket Vong.")]
        public Renderer towerEye;
        public string firstScene = "Level1";

        public string EndingName { get; private set; }
        public string EndingLine { get; private set; }

        void Start()
        {
            var gf = GameFlow.Instance;
            Show(gf ? gf.TotalRewinds : 0, gf ? gf.MaxTier : 0);
            if (restartButton) restartButton.onClick.AddListener(Restart);
            if (quitButton) quitButton.onClick.AddListener(Quit);
        }

        /// <summary>Hien ket theo thong ke (co the goi trong editor de xem truoc).</summary>
        public void Show(int rewinds, int maxTier)
        {
            bool vong = rewinds >= vongFromRewinds;
            EndingName = vong ? VongName : LangName;
            EndingLine = vong ? VongLine : LangLine;
            if (titleText) titleText.text = EndingName.ToUpperInvariant();
            if (lineText) lineText.text = "NHỊP: “" + EndingLine + "”";
            if (statsText) statsText.text = $"Tổng số lần tua hợp lệ: {rewinds}      Cấp NHỊP cao nhất: {maxTier}";
            if (towerEye)
            {
                var m = Application.isPlaying ? towerEye.material : towerEye.sharedMaterial;
                Color c = vong ? new Color(0.85f, 0.05f, 0.04f) : new Color(0.6f, 0.75f, 0.85f);
                m.SetColor("_BaseColor", c);
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", vong ? new Color(1f, 0.08f, 0.05f) * 1.8f : c * 0.4f);
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
