using System.Collections;
using UnityEngine;
using TMPro;

namespace EchoShade
{
    /// <summary>
    /// MANH NHO trong man (v3.1 muc 3.1). Vat nho phat sang, KHONG chan duong.
    ///  - Bam E khi Khang dung canh (cung lan, |dx| &lt;= interactRadius), luc nao cung duoc - khong can dung im
    ///    (dung im qua 1 giay la xac nhan tua) - tru khi DANG TUA (dang lui): khong nhat duoc.
    ///  - Nhat xong: hien ghi chu, luu vao MemoryStore (ngoai thoi gian). Tua/het mau/phim R khong tra lai.
    ///  - Manh da nhat (vd. choi lai man) chi con de mo.
    /// </summary>
    public class MemoryFragment : MonoBehaviour
    {
        public MemoryFragmentData data;
        [Tooltip("Z cua lan ma Khang phai dung de nhat (0 = gan, 2.2 = xa).")]
        public float laneZ = 0f;
        public float floorY = 0f;
        public float interactRadius = 1.0f;
        public Transform glow;
        public Renderer glowRenderer;
        public TMP_Text label;
        public float labelRange = 3.2f;
        public Color glowColor = new Color(0.55f, 0.85f, 1f);

        public bool Collected => data && MemoryStore.Has(data.id);
        public string LastMessage { get; private set; } = "";

        Vector3 _glow0;
        Material _mat;
        float _hintUntil;
        bool _vanishing;

        void Awake()
        {
            if (glow) _glow0 = glow.localPosition;
            if (glowRenderer) _mat = glowRenderer.material;
        }

        void OnEnable() { KhangInteractor.InteractOverride += OnInteract; }
        void OnDisable() { KhangInteractor.InteractOverride -= OnInteract; }

        void Start() { Refresh(); }

        void Refresh()
        {
            bool done = Collected;
            if (glow && !_vanishing) glow.gameObject.SetActive(!done);
            if (label) label.gameObject.SetActive(false);
        }

        bool KhangNear(out KhangController k, float radius)
        {
            var ph = PlayerHealth.Instance;
            k = ph ? ph.GetComponent<KhangController>() : null;
            if (!k) return false;
            var p = k.transform.position;
            return Mathf.Abs(p.x - transform.position.x) <= radius && Mathf.Abs(p.z - laneZ) < 0.7f && Mathf.Abs(p.y - (floorY + 1.05f)) < 1.5f;
        }

        bool OnInteract()
        {
            if (!data || Collected || !KhangNear(out var k, interactRadius) || k.IsSwitchingLane) return false;
            var clock = LevelClock.Instance;
            if (clock && (clock.Direction == TimeDirection.Backward || clock.CurrentRewindAmount > 0.05f))
            {
                if (HUD.Instance && Time.unscaledTime > _hintUntil) { HUD.Instance.ShowSubtitle("Đang tua — ký ức trượt khỏi tay. Đứng yên một nhịp rồi bấm E.", 2.5f, false); _hintUntil = Time.unscaledTime + 2f; }
                LastMessage = "rewinding";
                return true;
            }
            Collect();
            return true;
        }

        /// <summary>Nhat manh nho (cung dung cho kiem thu).</summary>
        public void Collect()
        {
            if (!data || !MemoryStore.Collect(data)) return;
            int inLevel = MemoryStore.CountInLevel(data.levelIndex);
            LastMessage = data.displayName;
            if (HUD.Instance)
            {
                HUD.Instance.ShowSubtitle($"<color=#9fd8ff>Mảnh nhớ · {data.displayName}</color>\n<size=85%>{data.note}</size>", 6f, false);
                if (inLevel >= MemoryStore.PerLevel) HUD.Instance.ShowHint($"Đã đủ {inLevel}/{MemoryStore.PerLevel} Mảnh nhớ của màn này.", 4f);
                if (!string.IsNullOrEmpty(data.nhipLine)) StartCoroutine(SayLater(data.nhipLine, 6.2f));
            }
            StartCoroutine(Vanish());
        }

        IEnumerator SayLater(string line, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (HUD.Instance) HUD.Instance.ShowSubtitle(line, 4f, true);
        }

        IEnumerator Vanish()
        {
            _vanishing = true;
            if (label) label.gameObject.SetActive(false);
            if (glow)
            {
                Vector3 s0 = glow.localScale;
                for (float u = 0f; u < 1f; u += Time.deltaTime / 0.6f)
                {
                    glow.localScale = s0 * (1f + 0.8f * u) * (1f - u);
                    glow.localPosition = _glow0 + Vector3.up * u * 0.8f;
                    if (_mat) _mat.SetColor("_EmissionColor", glowColor * (3f + 6f * (1f - u)));
                    yield return null;
                }
                glow.gameObject.SetActive(false);
                glow.localScale = s0; glow.localPosition = _glow0;
            }
            _vanishing = false;
        }

        void Update()
        {
            if (Collected || _vanishing) return;
            if (glow)
            {
                glow.localPosition = _glow0 + Vector3.up * 0.08f * Mathf.Sin(Time.time * 2.2f + transform.position.x);
                glow.Rotate(0f, 70f * Time.deltaTime, 0f, Space.World);
            }
            if (_mat)
            {
                _mat.EnableKeyword("_EMISSION");
                _mat.SetColor("_EmissionColor", glowColor * (2.2f + 0.9f * Mathf.Sin(Time.time * 3f)));
            }
            if (label)
            {
                bool show = KhangNear(out _, labelRange);
                if (label.gameObject.activeSelf != show) label.gameObject.SetActive(show);
                if (show) label.text = KhangNear(out _, interactRadius) ? "E · " + (data ? data.displayName : "Mảnh nhớ") : "Mảnh nhớ";
            }
        }
    }
}
