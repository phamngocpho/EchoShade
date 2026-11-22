using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// DOAN KHONG DOI LAN (v3.1 Man 2 - hanh lang phu): trong cac khoang X nay Khang khong doi lan duoc,
    /// nen muon ra khoi hanh lang cut phai lui ve loi vao (ton buoc chan - co the thanh mot lan tua).
    /// Dung KhangController.laneSwitchGate (man nao da co cong khac thi khong dung component nay).
    /// </summary>
    public class LaneLock : MonoBehaviour
    {
        [System.Serializable] public struct Span { public float fromX, toX; public string message; }
        public KhangController khang;
        public Span[] spans = new Span[0];
        float _hintUntil;

        void Start()
        {
            if (!khang) khang = FindAnyObjectByType<KhangController>();
            if (khang) khang.laneSwitchGate = CanSwitch;
        }

        void OnDestroy() { if (khang && khang.laneSwitchGate == (System.Func<KhangController.Lane, bool>)CanSwitch) khang.laneSwitchGate = null; }

        bool CanSwitch(KhangController.Lane target)
        {
            float x = khang.transform.position.x;
            foreach (var s in spans)
                if (x > s.fromX && x < s.toX)
                {
                    if (HUD.Instance && Time.unscaledTime > _hintUntil && !string.IsNullOrEmpty(s.message))
                    { HUD.Instance.ShowSubtitle(s.message, 2.5f, false); _hintUntil = Time.unscaledTime + 3f; }
                    return false;
                }
            return true;
        }
    }
}
