using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// TICH TAC THEO BAC (v2 Should): tieng tich tac sinh bang code (khong can file am thanh),
    /// nhanh dan theo bac NHIP. Chi keu khi thoi gian man dang chay (dung im = thanh pho nin tho = im lang).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class NhipTick : MonoBehaviour
    {
        public float[] intervalByTier = { 0.9f, 0.7f, 0.5f, 0.33f };
        public float volume = 0.25f;
        AudioSource _src;
        AudioClip _tick, _tock;
        float _next;
        bool _alt;

        void Awake()
        {
            _src = GetComponent<AudioSource>();
            _src.playOnAwake = false; _src.spatialBlend = 0f;
            _tick = Make(2200f, 0.03f); _tock = Make(1500f, 0.035f);
        }

        static AudioClip Make(float freq, float len)
        {
            int rate = 44100, n = Mathf.CeilToInt(rate * len);
            var c = AudioClip.Create("tick", n, 1, rate, false);
            var d = new float[n];
            for (int i = 0; i < n; i++) { float t = i / (float)rate; d[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Exp(-t * 140f); }
            c.SetData(d, 0); return c;
        }

        void Update()
        {
            var clock = LevelClock.Instance; var b = NhipBrain.Instance;
            if (!clock || !b || Time.timeScale == 0f) return;
            if (clock.Direction == TimeDirection.Frozen) { _next = Mathf.Max(_next, Time.time + 0.05f); return; }
            if (Time.time < _next) return;
            _next = Time.time + intervalByTier[Mathf.Clamp(b.Tier, 0, intervalByTier.Length - 1)];
            _src.pitch = clock.Direction == TimeDirection.Backward ? 0.8f : 1f;
            _src.PlayOneShot(_alt ? _tock : _tick, volume * (0.6f + 0.15f * b.Tier));
            _alt = !_alt;
        }
    }
}
