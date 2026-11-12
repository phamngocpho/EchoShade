using System;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 2 - toa tau do NHIP dieu khien, chay tren LAN GAN (duong ray).
    /// Vi tri = ham thuan cua (thoi gian man, tier NHIP):
    ///   Tier 0: t = 4.0 -> 7.0   Tier 1: 3.4 -> 6.6 (som hon)   Tier 2: 3.2 -> 6.2 (som + nhanh hon)
    /// Ngoai cua so thoi gian tau an di. Dung tren lan gan khi tau quet qua = -1 HP (Hazard).
    /// Den tau doi mau theo bac (xanh -> vang -> cam, v2) de nguoi cham thay NHIP nang cap.
    /// </summary>
    public class TrainCar : TimelineObject
    {
        [Serializable]
        public struct TierRun { public float startTime, endTime; public Color lightColor; public float glow; }

        public NhipBrain brain;
        public Hazard hazard;
        public Renderer[] lightRenderers;
        public float startX = -24f, endX = 30f, laneZ = 0f, y = 0.9f;
        public float length = 14f;

        public TierRun[] tiers =
        {
            new TierRun { startTime = 4.0f, endTime = 7.0f, lightColor = new Color(0.3f, 1f, 0.6f), glow = 2f },
            new TierRun { startTime = 3.4f, endTime = 6.6f, lightColor = new Color(1f, 0.75f, 0.1f), glow = 4f },
            new TierRun { startTime = 3.2f, endTime = 6.2f, lightColor = new Color(1f, 0.5f, 0.08f), glow = 8f },
        };

        Renderer[] _renderers;
        Material[] _lightMats;

        public TierRun Current => tiers[Mathf.Clamp(brain ? brain.Tier : 0, 0, tiers.Length - 1)];

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
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

        /// <summary>X cua dau tau tai thoi diem t.</summary>
        public float FrontXAt(float t)
        {
            var r = Current;
            return Mathf.Lerp(startX, endX, Mathf.InverseLerp(r.startTime, r.endTime, t));
        }

        /// <summary>Thoi diem dau tau toi vi tri x (tier hien tai).</summary>
        public float ArrivalTime(float x)
        {
            var r = Current;
            return Mathf.Lerp(r.startTime, r.endTime, Mathf.InverseLerp(startX, endX, x));
        }

        public bool IsRunningAt(float t) { var r = Current; return t >= r.startTime && t <= r.endTime; }

        public override void Evaluate(float t)
        {
            bool running = IsRunningAt(t);
            float front = FrontXAt(t);
            transform.position = new Vector3(front - length * 0.5f, y, laneZ);
            if (_renderers != null) foreach (var r in _renderers) if (r) r.enabled = running;
            if (hazard) hazard.active = running;

            var c = Current;
            if (_lightMats != null)
                foreach (var m in _lightMats)
                {
                    m.SetColor("_BaseColor", c.lightColor);
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", c.lightColor * c.glow);
                }
        }
    }
}
