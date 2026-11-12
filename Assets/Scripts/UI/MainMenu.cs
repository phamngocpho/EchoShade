using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EchoShade
{
    /// <summary>
    /// MENU CHINH (v1 5.1 + v2): nen san thuong pho Cu dang dong bang, toa tau treo giua troi.
    /// Choi (vao Man 1), Chon man (mo khoa theo tien do, PlayerPrefs), Huong dan (bang luat), Thoat, am luong.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public Button playButton, selectButton, guideButton, quitButton;
        public GameObject guidePanel, selectPanel;
        public Button guideClose, selectClose;
        public Button[] levelButtons;
        public Slider volume;
        public TMP_Text progressText;

        void Start()
        {
            Time.timeScale = 1f;
            AudioListener.volume = PlayerPrefs.GetFloat("ES_Volume", 0.8f);
            if (guidePanel) guidePanel.SetActive(false);
            if (selectPanel) selectPanel.SetActive(false);
            if (playButton) playButton.onClick.AddListener(() => GameFlow.Ensure().StartLevel(1));
            if (guideButton) guideButton.onClick.AddListener(() => Toggle(guidePanel, true));
            if (guideClose) guideClose.onClick.AddListener(() => Toggle(guidePanel, false));
            if (selectButton) selectButton.onClick.AddListener(() => Toggle(selectPanel, true));
            if (selectClose) selectClose.onClick.AddListener(() => Toggle(selectPanel, false));
            if (quitButton) quitButton.onClick.AddListener(Quit);
            int unlocked = GameFlow.Unlocked;
            if (levelButtons != null)
                for (int i = 0; i < levelButtons.Length; i++)
                {
                    int lv = i + 1;
                    var b = levelButtons[i]; if (!b) continue;
                    b.interactable = lv <= unlocked;
                    b.onClick.AddListener(() => GameFlow.Ensure().StartLevel(lv));
                }
            if (volume)
            {
                volume.value = AudioListener.volume;
                volume.onValueChanged.AddListener(v => { AudioListener.volume = v; PlayerPrefs.SetFloat("ES_Volume", v); });
            }
            if (progressText) progressText.text = $"Đã mở tới màn {unlocked}/5";
        }

        void Update()
        {
            if (KhangInput.Pause) { Toggle(guidePanel, false); Toggle(selectPanel, false); }
        }

        static void Toggle(GameObject g, bool on) { if (g) g.SetActive(on); }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
