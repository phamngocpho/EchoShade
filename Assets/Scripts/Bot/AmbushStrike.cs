using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// PHUC KICH MOT DIEM (v2 bac 2): don cua NHIP danh vao dung MOT diem (vi tri strikeTime + mot lan), khong bit ca lan.
    /// Lan bi danh = lan Khang dung trong vung memoryZone o dong ghi gan nhat cua bang nho (khong co = defaultLane).
    /// Luon co vong do bao truoc tren san. Ne bang doi lan TRUOC diem do.
    ///
    /// Vi thoi gian = vi tri, vat danh la ham thuan cua t: treo tren cao, roi xuong trong [strikeTime - fall, strikeTime],
    /// nam duoi toi strikeTime + hold, roi nang len. Dung im giua chung thi vat dung lo lung (thanh pho nin tho).
    /// Dung cho: Man 3 moc cau (G12,5), Man 4 thanh rang (G6), Man 5 tia do mot diem (G18).
    /// </summary>
    public class AmbushStrike : TimelineObject, IWarnable
    {
        public NhipBrain brain;
        public Hazard hazard;
        public Transform striker;           // vat danh (co Renderer cho Hazard)
        public Transform tether;            // day treo (tuy chon)
        public string memoryZone = "Zone";
        public float strikeTime = 6f;
        public int defaultLane = 0;
        public int activeFromTier = 2;
        public float nearZ = 0f, farZ = 2.2f;
        public float hangY = 5f, downY = 0.6f, topY = 6f;
        public float fallTime = 0.35f, holdTime = 0.5f, riseTime = 0.5f;
        [Tooltip("Che do tia: thay vi roi, cot sang hien len (scale Y) trong cua so don.")]
        public bool beamMode = false;

        public bool Armed => brain && brain.Tier >= activeFromTier;
        public int TargetLane => brain ? brain.RecallLane(memoryZone, defaultLane) : defaultLane;
        public float X => clock ? clock.TimeToX(strikeTime) : 0f;
        float LaneZ => TargetLane == 0 ? nearZ : farZ;

        public bool WarnVisible => Armed && clock && clock.CurrentTime < strikeTime + holdTime;
        public Vector3 WarnPosition => new Vector3(X, downY - 0.55f, LaneZ);

        protected override void Start()
        {
            if (!brain) brain = NhipBrain.Instance;
            base.Start();
        }

        public override void Evaluate(float t)
        {
            if (!striker) return;
            float y = hangY; bool danger = false; bool visible = Armed;
            if (Armed)
            {
                float a = strikeTime - fallTime, b = strikeTime + holdTime, c = b + riseTime;
                if (t < a) y = hangY;
                else if (t < strikeTime) { float u = (t - a) / fallTime; y = Mathf.Lerp(hangY, downY, u * u); danger = true; }
                else if (t < b) { y = downY; danger = true; }
                else if (t < c) y = Mathf.Lerp(downY, hangY, (t - b) / riseTime);
                else y = hangY;
                if (beamMode) visible = t >= a && t < b;
            }
            else y = topY;
            if (beamMode)
            {
                striker.position = new Vector3(X, downY + 1.6f, LaneZ);
                foreach (var r in striker.GetComponentsInChildren<Renderer>()) r.enabled = visible;
                if (visible)
                {
                    float u = Mathf.Clamp01((t - (strikeTime - fallTime)) / fallTime);
                    striker.localScale = new Vector3(Mathf.Lerp(0.15f, 0.7f, u), 3.2f, Mathf.Lerp(0.15f, 0.7f, u));
                }
            }
            else
            {
                striker.position = new Vector3(X, y, LaneZ);
                if (tether)
                {
                    float len = Mathf.Max(0.05f, topY + 2f - y);
                    tether.position = new Vector3(X, y + len * 0.5f, LaneZ);
                    tether.localScale = new Vector3(0.05f, len, 0.05f);
                    tether.gameObject.SetActive(Armed);
                }
            }
            if (hazard) hazard.active = danger;
        }
    }
}
