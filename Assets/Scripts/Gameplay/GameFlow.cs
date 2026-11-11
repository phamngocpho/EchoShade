using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoShade
{
    /// <summary>
    /// Quan ly luong man choi, song qua cac scene (DontDestroyOnLoad).
    ///  - Hom thu cuoi man goi CompleteLevel(): cong don so lan tua hop le + tier NHIP cao nhat,
    ///    cho loadDelay giay roi nap man tiep theo trong sceneOrder (neu co trong Build Settings).
    ///  - Thu tu: Level1 -> Level2 -> Level3 -> End.
    /// </summary>
    public class GameFlow : MonoBehaviour
    {
        public static GameFlow Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public string[] sceneOrder = { "Level1", "Level2", "Level3", "End" };
        public float loadDelay = 2f;

        public int TotalRewinds { get; private set; }
        public int MaxTier { get; private set; }
        public readonly List<string> CompletedLevels = new List<string>();
        public bool IsLoading { get; private set; }

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

        /// <summary>Ghi thong ke man vua xong va nap man ke. Tra ve ten man ke (hoac null).</summary>
        public string CompleteLevel(string sceneName)
        {
            if (IsLoading) return null;
            int rewinds = LevelClock.Instance ? LevelClock.Instance.RewindCount : 0;
            int tier = NhipBrain.Instance ? NhipBrain.Instance.MaxTierReached : 0;
            TotalRewinds += rewinds;
            MaxTier = Mathf.Max(MaxTier, tier);
            CompletedLevels.Add(sceneName);
            Debug.Log($"[GameFlow] Xong {sceneName}: tua={rewinds}, tierMax={tier}. Tong tua={TotalRewinds}, tier cao nhat={MaxTier}");

            string next = NextScene(sceneName);
            if (next == null) { Debug.Log("[GameFlow] Khong con man tiep theo trong Build Settings."); return null; }
            StartCoroutine(LoadAfterDelay(next));
            return next;
        }

        /// <summary>Choi lai tu dau: xoa thong ke tong (so lan tua, tier cao nhat, cac man da xong).</summary>
        public void ResetRun()
        {
            StopAllCoroutines();
            IsLoading = false;
            TotalRewinds = 0;
            MaxTier = 0;
            CompletedLevels.Clear();
        }

        /// <summary>Chi dung cho test / man End trong editor: dat thong ke tong.</summary>
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

        IEnumerator LoadAfterDelay(string scene)
        {
            IsLoading = true;
            yield return new WaitForSecondsRealtime(loadDelay);
            Time.timeScale = 1f;
            SceneManager.LoadScene(scene);
            IsLoading = false;
        }
    }
}
