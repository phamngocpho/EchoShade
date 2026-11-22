using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace EchoShade
{
    /// <summary>
    /// Hieu ung hinh khi tua nguoc (thay cho lop phu xanh toan man hinh cu):
    ///  - Volume toan cuc rieng (uu tien cao): giam bao hoa + loc mau lanh, vignette lanh dap nhe, quang sai mau o mep.
    ///  - Tat nhanh (~0.25 s) khi het tua. Chi la hinh anh, khong anh huong gameplay.
    /// Tat ca tham so nam trong pass uber cua URP nen gan nhu khong ton them GPU.
    /// </summary>
    public class RewindFX : MonoBehaviour
    {
        public LevelClock clock;
        [Range(0f, 1f)] public float weightRewinding = 0.65f;
        [Range(0f, 1f)] public float weightQualified = 1f;
        public float fadeIn = 0.15f, fadeOut = 0.25f;

        Volume _vol;
        VolumeProfile _profile;
        Vignette _vig;
        float _w, _pulse;

        void Start()
        {
            if (!clock) clock = LevelClock.Instance ? LevelClock.Instance : FindAnyObjectByType<LevelClock>();

            var go = new GameObject("RewindFX_Volume");
            go.transform.SetParent(transform, false);
            _vol = go.AddComponent<Volume>();
            _vol.isGlobal = true; _vol.priority = 50f; _vol.weight = 0f;
            _profile = ScriptableObject.CreateInstance<VolumeProfile>();
            _profile.name = "RewindFX (runtime)";
            var ca = _profile.Add<ColorAdjustments>(true);
            ca.saturation.Override(-38f);
            ca.colorFilter.Override(new Color(0.84f, 0.93f, 1.08f));
            ca.contrast.Override(4f);
            _vig = _profile.Add<Vignette>(true);
            _vig.color.Override(new Color(0.05f, 0.13f, 0.22f));
            _vig.intensity.Override(0.36f);
            _vig.smoothness.Override(0.45f);
            var chr = _profile.Add<ChromaticAberration>(true);
            chr.intensity.Override(0.22f);
            _vol.sharedProfile = _profile;
        }

        void Update()
        {
            if (!clock || !_vol) return;
            float dt = Time.unscaledDeltaTime;
            bool back = clock.Direction == TimeDirection.Backward;
            bool active = back || (clock.CurrentRewindAmount > 0.05f && clock.Direction == TimeDirection.Frozen);

            float want = active ? (clock.CurrentRewindQualified ? weightQualified : weightRewinding) : 0f;
            float rate = want > _w ? 1f / Mathf.Max(0.01f, fadeIn) : 1f / Mathf.Max(0.01f, fadeOut);
            _w = Mathf.MoveTowards(_w, want, dt * rate);
            _vol.weight = _w;

            if (_w > 0.001f && _vig)
            {
                _pulse += dt * (clock.CurrentRewindQualified ? 5.5f : 3.5f);
                _vig.intensity.value = 0.33f + 0.05f * (0.5f + 0.5f * Mathf.Sin(_pulse));
            }
        }

        void OnDestroy()
        {
            if (_profile) Destroy(_profile);
        }
    }
}
