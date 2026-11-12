using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoShade
{
    /// <summary>
    /// Luong game (v2 GameManager/SceneFlow), song qua cac scene (DontDestroyOnLoad).
    ///  - MainMenu -> Level1 -> ... -> Level5 -> End.
    ///  - Thung thu cuoi man (hoac khe that o Man 5) goi CompleteLevel(): cong don so lan tua + bac NHIP cao nhat,
    ///    mo khoa man ke (PlayerPrefs "ES_Unlocked"), cho loadDelay giay roi nap man tiep theo.
    ///  - Ket (v2 muc 5) do lua chon o Man 5: SetEnding(vong). So lan tua chi con la thong ke.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public const string MenuScene = "MainMenu";
        public const string UnlockKey = "ES_Unlocked";
        public string[] sceneOrder = { "Level1", "Level2", "Level3", "Level4", "Level5", "End" };
        public float loadDelay = 2f;

        public int TotalRewinds { get; private set; }
        public int MaxTier { get; private set; }
        public readonly List<string> CompletedLevels = new List<string>();
        public bool IsLoading { get; private set; }
        public bool HasEnding { get; private set; }
        public bool EndingVong { get; private set; }

        public static GameFlow Ensure()
        {
            if (Instance) return Instance;
            var go = new GameObject("GameFlow");
            Instance = go.AddComponent<GameFlow>();
            DontDestroyOnLoad(go);
            return Instance;
        }

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public static int Unlocked => Mathf.Clamp(PlayerPrefs.GetInt(UnlockKey, 1), 1, 5);

        /// <summary>Ghi thong ke man vua xong va nap man ke. Tra ve ten man ke (hoac null).</summary>
        public string CompleteLevel(string sceneName)
        {
            if (IsLoading) return null;
            int rewinds = LevelClock.Instance ? LevelClock.Instance.RewindCount : 0;
            int tier = NhipBrain.Instance ? NhipBrain.Instance.MaxTierReached : 0;
            TotalRewinds += rewinds;
            MaxTier = Mathf.Max(MaxTier, tier);
            CompletedLevels.Add(sceneName);
            int idx = System.Array.IndexOf(sceneOrder, sceneName);
            if (idx >= 0 && idx + 2 > Unlocked) { PlayerPrefs.SetInt(UnlockKey, Mathf.Min(5, idx + 2)); PlayerPrefs.Save(); }
            Debug.Log($"[GameFlow] Xong {sceneName}: tua={rewinds}, bacMax={tier}. Tong tua={TotalRewinds}, bac cao nhat={MaxTier}");

            string next = NextScene(sceneName);
            if (next == null) { Debug.Log("[GameFlow] Khong con man tiep theo trong Build Settings."); return null; }
            StartCoroutine(LoadAfterDelay(next, loadDelay));
            return next;
        }

        public void SetEnding(bool vong)
        {
            HasEnding = true; EndingVong = vong;
            PlayerPrefs.SetInt(vong ? "ES_SeenVong" : "ES_SeenLang", 1); PlayerPrefs.Save();
            Debug.Log("[GameFlow] Ket: " + (vong ? "Ket Vong" : "Ket Lang"));
        }

        /// <summary>Choi lai tu dau: xoa thong ke tong.</summary>
        public void ResetRun()
        {
            StopAllCoroutines();
            IsLoading = false;
            TotalRewinds = 0;
            MaxTier = 0;
            HasEnding = false; EndingVong = false;
            CompletedLevels.Clear();
        }

        public void StartLevel(int levelIndex)
        {
            ResetRun();
            Time.timeScale = 1f;
            SceneManager.LoadScene("Level" + Mathf.Clamp(levelIndex, 1, 5));
        }

        public void GoMenu()
        {
            StopAllCoroutines(); IsLoading = false; Time.timeScale = 1f;
            SceneManager.LoadScene(MenuScene);
        }

        /// <summary>Chi dung cho test / man End trong editor.</summary>
        public void SetTotalsForTest(int rewinds, int maxTier)
        {
            TotalRewinds = rewinds;
            MaxTier = maxTier;
        }

        public string NextScene(string current)
        {
            int i = System.Array.IndexOf(sceneOrder, current);
            for (int j = i + 1; j < sceneOrder.Length && i >= 0; j++)
                if (Application.CanStreamedLevelBeLoaded(sceneOrder[j])) return sceneOrder[j];
            return null;
        }

        IEnumerator LoadAfterDelay(string scene, float delay)
        {
            IsLoading = true;
            yield return new WaitForSecondsRealtime(delay);
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            IsLoading = false;
        }
    }
}
