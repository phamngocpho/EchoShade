using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// TIA KHOA NHAP NHAY (v3.1 Man 5 tang 1 - phong phu): man tia do chan loi vao phong phu, bat/tat theo
    /// THOI GIAN THUC (khong theo thuoc) => phai canh nhip ma qua. Cham tia khi dang bat = mat 1 mau.
    /// Truoc khi bat lai co nhap nhay canh bao.
    /// </summary>
    public class BlinkBeam : MonoBehaviour
    {
        public Hazard hazard;
        public Renderer[] renderers;
        public float onSeconds = 1.3f, offSeconds = 1.1f, warnSeconds = 0.35f, phase = 0f;
        public bool IsOn { get; private set; }

        void Update()
        {
            float cycle = onSeconds + offSeconds;
            float u = Mathf.Repeat(Time.time + phase, cycle);
            IsOn = u < onSeconds;
            bool warn = !IsOn && u > cycle - warnSeconds;
            bool vis = IsOn || (warn && Mathf.Repeat(Time.time * 12f, 1f) > 0.5f);
            if (hazard) hazard.active = IsOn;
            if (renderers != null) foreach (var r in renderers) if (r) r.enabled = vis;
        }
    }
}
