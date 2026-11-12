using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// CONTAINER CUA CAN CAU (Man 3, lan xa G10-11,5). Vat theo thoi gian (ham thuan cua t):
    ///   t &lt; liftTime: container nam tren ben, chan lan xa (cham = mat mau, bi day sang lan kia).
    ///   t tu liftTime: can cau nhac container len => lan xa trong tu giay 11,5.
    /// Vi thoi gian = vi tri, Khang chi toi duoc t &gt; 11,5 bang lan gan (qua cau), roi moi doi sang lan xa.
    /// </summary>
    public class CraneContainer : TimelineObject
    {
        public Transform box;
        public Transform cable;
        public Hazard hazard;
        public Collider solid;
        public float restY = 1.3f, liftedY = 8f, topY = 10f;
        public float liftTime = 11.5f, liftDuration = 0.8f;

        public override void Evaluate(float t)
        {
            if (!box) return;
            float u = Mathf.Clamp01((t - liftTime) / liftDuration);
            float y = Mathf.Lerp(restY, liftedY, u * u);
            var p = box.position; p.y = y; box.position = p;
            bool down = u <= 0.001f;
            if (solid) solid.enabled = down;
            if (hazard) hazard.active = !down && u < 0.35f || down;
            if (cable)
            {
                float top = y + box.localScale.y * 0.5f;
                float len = Mathf.Max(0.05f, topY - top);
                cable.position = new Vector3(p.x, top + len * 0.5f, p.z);
                cable.localScale = new Vector3(0.06f, len, 0.06f);
            }
        }
    }
}
