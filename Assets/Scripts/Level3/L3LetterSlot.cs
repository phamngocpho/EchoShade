using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 3 - khe thu. Phim E khi dung truoc khe (cung lan): khe gia => khong nhan thu;
    /// khe that => giao thu (neu lan xa khong bi chan).
    /// </summary>
    public class L3LetterSlot : MonoBehaviour
    {
        public bool isReal;
        public NhipTowerMind mind;
        public KhangController khang;
        public float laneZ;
        public float halfWidth = 0.8f;
        public Renderer glow;

        Material _glowMat;

        void Start()
        {
            if (!mind) mind = NhipTowerMind.Instance;
            if (!khang) khang = FindAnyObjectByType<KhangController>();
            if (glow) _glowMat = glow.material;
        }

        void OnEnable() { KhangInteractor.InteractOverride += OnInteract; }
        void OnDisable() { KhangInteractor.InteractOverride -= OnInteract; }

        public bool KhangHere => khang && Mathf.Abs(khang.transform.position.x - transform.position.x) <= halfWidth
                                       && Mathf.Abs(khang.transform.position.z - laneZ) < 0.6f;

        bool OnInteract() => KhangHere && TryDeliver();

        /// <summary>Thu giao thu. Tra ve true neu da xu ly phim E.</summary>
        public bool TryDeliver()
        {
            if (!mind || mind.Delivered) return false;
            if (!isReal)
            {
                if (HUD.Instance) HUD.Instance.ShowSubtitle("Khe này bị hàn kín. Đây là khe giả.", 3f, false);
                return true;
            }
            if (mind.GuardLane == KhangController.Lane.Far)
            {
                if (HUD.Instance) HUD.Instance.ShowSubtitle("Thanh chắn đang khóa khe thật.", 3f, false);
                return true;
            }
            mind.Deliver();
            return true;
        }

        void Update()
        {
            if (!_glowMat || !mind) return;
            bool hint = isReal && !mind.Delivered && mind.GuardLane == KhangController.Lane.Near;
            Color c = mind.Delivered && isReal ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.8f, 0.4f);
            float g = mind.Delivered && isReal ? 2.5f : hint ? 1.2f + 0.8f * Mathf.Abs(Mathf.Sin(Time.time * 3f)) : 0.25f;
            _glowMat.EnableKeyword("_EMISSION");
            _glowMat.SetColor("_EmissionColor", c * g);
        }
    }
}
