using System;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Hom thu = checkpoint.
    ///  - Khang di ngang qua (|dx| <= activationRadius, lan nao cung duoc) => hom thu thanh "dang kich hoat",
    ///    vien hom thu phat sang.
    ///  - Hoi sinh se ve hom thu dang kich hoat; thoi gian man dat lai theo X cua hom thu.
    ///  - isFinal = hom thu cuoi man: cham vao = hoan thanh man.
    /// </summary>
    public class Mailbox : MonoBehaviour
    {
        public static Mailbox Active { get; private set; }
        public static bool LevelCompleted { get; private set; }
        public static event Action<Mailbox> OnLevelComplete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Active = null; LevelCompleted = false; OnLevelComplete = null; }

        public bool activeAtStart;
        public bool isFinal;
        public float activationRadius = 1.0f;
        [Tooltip("Z cua lan Khang hoi sinh (lan gan = 0).")]
        public float spawnZ = 0f;
        public Renderer rim;
        public Color idleColor = new Color(0.25f, 0.25f, 0.3f);
        public Color activeColor = new Color(1f, 0.85f, 0.3f);
        public float activeGlow = 4f;
        [TextArea] public string arrivalLine;
        public string completeBanner = "HOÀN THÀNH MÀN";

        public Vector3 SpawnPoint => new Vector3(transform.position.x, 1.1f, spawnZ);

        Material _rimMat;

        void Awake()
        {
            if (rim) _rimMat = rim.material; // ban sao rieng de phat sang
            LevelCompleted = false; // man moi duoc nap
            if (activeAtStart) Active = this;
        }

        void Start() { RefreshRim(); }

        void Update()
        {
            var ph = PlayerHealth.Instance;
            Transform p = ph ? ph.transform : null;
            if (!p) return;
            if (Mathf.Abs(p.position.x - transform.position.x) <= activationRadius && p.position.y > -0.5f)
            {
                if (Active != this) Activate();
                if (isFinal && !LevelCompleted) Complete();
            }
            if (Time.frameCount % 10 == 0) RefreshRim();
        }

        public void Activate()
        {
            var old = Active;
            Active = this;
            if (old) old.RefreshRim();
            RefreshRim();
            Debug.Log($"[Mailbox] Checkpoint: {name}");
        }

        void Complete()
        {
            LevelCompleted = true;
            Debug.Log("[Level] Level complete!");
            if (HUD.Instance)
            {
                if (!string.IsNullOrEmpty(arrivalLine)) HUD.Instance.ShowSubtitle(arrivalLine, 8f, true);
                HUD.Instance.ShowBanner(completeBanner);
            }
            OnLevelComplete?.Invoke(this);
            string next = GameFlow.Ensure().CompleteLevel(gameObject.scene.name);
            if (HUD.Instance && !string.IsNullOrEmpty(next)) HUD.Instance.ShowBanner(completeBanner + "\n<size=36>Sang " + DisplayName(next) + "...</size>");
        }

        /// <summary>Ten man hien thi cho nguoi choi.</summary>
        static string DisplayName(string scene)
        {
            switch (scene)
            {
                case "Level1": return "Màn 1";
                case "Level2": return "Màn 2";
                case "Level3": return "Màn 3: Tháp Nhịp";
                case "End": return "hồi kết";
                default: return scene;
            }
        }

        void RefreshRim()
        {
            if (!_rimMat) return;
            bool on = Active == this;
            Color c = on ? activeColor : idleColor;
            _rimMat.SetColor("_BaseColor", c);
            if (on)
            {
                _rimMat.EnableKeyword("_EMISSION");
                _rimMat.SetColor("_EmissionColor", activeColor * activeGlow);
            }
            else
            {
                _rimMat.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
