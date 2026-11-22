using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// ONG XA HOI (Man 1 v3.1, phan doan "di cham quan sat"): cot hoi phun theo THOI GIAN MAN o mot lan.
    /// Phun trong [onFrom, onTo] (gay mat mau, day sang lan kia). Truoc do warnLead giay man hoi bat dau ri ra
    /// de nguoi choi di cham nhin truoc. Vi thoi gian = vi tri nen dung im thi hoi cung dung (thanh pho nin tho).
    /// </summary>
    public class SteamVent : TimelineObject
    {
        public float onFrom = 10f, onTo = 11f;
        public float warnLead = 1.5f;
        public Transform plume;
        public Hazard hazard;
        public float fullHeight = 2.6f;
        public Renderer plumeRenderer;
        Material _mat;

        void Awake() { if (plumeRenderer) _mat = plumeRenderer.material; }

        public bool IsOn(float t) => t >= onFrom && t <= onTo;

        public override void Evaluate(float t)
        {
            bool on = IsOn(t);
            float h;
            if (on) h = fullHeight * (0.85f + 0.15f * Mathf.Sin(t * 40f));
            else if (t >= onFrom - warnLead && t < onFrom) h = Mathf.Lerp(0.08f, 0.6f, (t - (onFrom - warnLead)) / warnLead);
            else h = 0.04f;
            if (plume)
            {
                var s = plume.localScale; s.y = h; plume.localScale = s;
                var p = plume.localPosition; p.y = h * 0.5f + 0.1f; plume.localPosition = p;
            }
            if (_mat) { _mat.EnableKeyword("_EMISSION"); _mat.SetColor("_EmissionColor", (on ? new Color(1f, 0.55f, 0.35f) : new Color(0.7f, 0.75f, 0.8f)) * (on ? 1.4f : 0.4f)); }
            if (hazard) hazard.active = on;
        }
    }
}
