using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 2 - bien treo. Chi khi NHIP dat TIER 2 bien moi roi o t = 8.0 -> 8.25 s, ngay truoc cua.
    /// Lan bi roi = lan Khang da di trong khu tau (t = 4..8) o DONG THOI GIAN DA BI XOA gan nhat
    /// (NhipBrain.RecallLane), mac dinh = lan XA. Day la "phuc kich" dua tren bang ghi nho cua NHIP.
    ///
    /// LOI GIAI (tier 2): dung lai truoc t = 8 (X < 8.4), xuong lan gan (tau da di qua), di qua cho bien,
    /// de bien roi xuong lan xa trong, roi len lai lan xa truoc cua (t = 9) va dat thung len can.
    /// Bien da nam xuong chi chan MOT diem (rong 0.5) nen van di vong duoc.
    /// </summary>
    public class HangingSign : TimelineObject
    {
        public NhipBrain brain;
        public Hazard hazard;
        public Transform tether;
        public Renderer[] lightRenderers;
        public string memoryZone = "L2_TrainZone";
        public int activeFromTier = 2;
        public float signX = 9f, hangY = 4.6f, landY = 0.7f, beamY = 5.4f;
        public float nearZ = 0f, farZ = 2.2f;
        public float fallStart = 8f, fallEnd = 8.25f;
        public Color[] tierLight = { new Color(0.3f, 1f, 0.5f), new Color(1f, 0.7f, 0.1f), new Color(1f, 0.05f, 0.05f) };

        Collider _col;
        Material[] _lightMats;

        public int TargetLane => brain ? brain.RecallLane(memoryZone, 1) : 1;
        public bool Armed => brain && brain.Tier >= activeFromTier;

        void Awake()
        {
            _col = GetComponent<Collider>();
            if (lightRenderers != null)
            {
                _lightMats = new Material[lightRenderers.Length];
                for (int i = 0; i < lightRenderers.Length; i++) _lightMats[i] = lightRenderers[i].material;
            }
        }

        protected override void Start()
        {
            if (!brain) brain = NhipBrain.Instance;
            base.Start();
        }

        public override void Evaluate(float t)
        {
            float z = Armed ? (TargetLane == 0 ? nearZ : farZ) : farZ;
            float y = hangY;
            bool falling = false, landed = false;
            if (Armed && t >= fallStart)
            {
                float u = Mathf.Clamp01((t - fallStart) / (fallEnd - fallStart));
                y = Mathf.Lerp(hangY, landY, u * u); // roi nhanh dan
                landed = t >= fallEnd;
                falling = !landed;
            }
            transform.position = new Vector3(signX, y, z);
            if (_col) _col.enabled = landed;
            if (hazard) hazard.active = falling;
            if (tether)
            {
                float len = Mathf.Max(0.05f, beamY - (y + 0.7f));
                tether.gameObject.SetActive(!landed && !falling);
                tether.position = new Vector3(signX, y + 0.7f + len * 0.5f, z);
                tether.localScale = new Vector3(0.05f, len, 0.05f);
            }

            int tier = brain ? Mathf.Clamp(brain.Tier, 0, tierLight.Length - 1) : 0;
            float blink = tier >= 2 ? (Mathf.Repeat(Time.unscaledTime * 4f, 1f) > 0.5f ? 1f : 0.3f) : 1f;
            if (_lightMats != null)
                foreach (var m in _lightMats)
                {
                    m.SetColor("_BaseColor", tierLight[tier]);
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", tierLight[tier] * (1.5f + 2.5f * tier) * blink);
                }
        }
    }
}
