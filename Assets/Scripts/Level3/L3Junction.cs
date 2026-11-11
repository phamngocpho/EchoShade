using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 3 - NGA BA (t = 2 s). Hai lan bi ngan boi bo tuong thap; chi o nga ba Khang moi doi lan duoc.
    /// Doi lan khong doi X nen thoi gian DUNG trong luc doi lan (dung quy tac chung).
    /// </summary>
    public class L3Junction : MonoBehaviour
    {
        public LevelClock clock;
        public KhangController khang;
        public float junctionTime = 2f;
        [Tooltip("Nua be rong vung nga ba (don vi X).")]
        public float halfWidth = 0.8f;

        float _hintUntil;

        public float X => clock ? clock.TimeToX(junctionTime) : -8.4f;
        public bool KhangAtJunction => khang && Mathf.Abs(khang.transform.position.x - X) <= halfWidth;

        void Start()
        {
            if (!clock) clock = LevelClock.Instance;
            if (!khang) khang = FindAnyObjectByType<KhangController>();
            if (khang) khang.laneSwitchGate = CanSwitch;
        }

        void OnDestroy()
        {
            if (khang) khang.laneSwitchGate = null;
        }

        bool CanSwitch(KhangController.Lane target)
        {
            if (KhangAtJunction) return true;
            if (HUD.Instance && Time.unscaledTime > _hintUntil)
            {
                HUD.Instance.ShowSubtitle("Chỉ đổi làn được ở ngã ba (t = 2 s).", 2f, false);
                _hintUntil = Time.unscaledTime + 3f;
            }
            return false;
        }
    }
}
