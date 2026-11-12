using System;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 1 - cang may cua NHIP chay tren ray tran.
    ///
    /// Quy tac thoi gian: vi tri cang may la HAM THUAN cua (thoi gian man, tier cua NHIP).
    ///   Tier 0: t = 4.5 -> 6.0 truot tu bai do toi cho moc, 6.0 -> 6.4 ha xuong, KEP tai 6.4, roi nang len va mang ve bai.
    ///   Tier 1: den som hon (t ~ 3.5) va truot nhanh hon (0.8 s thay vi 1.5 s). Mat NHIP vang (v2), to hon; vong do bao cho cang ha.
    /// Kep: neu moc van con la "vat theo thoi gian" (chua bi Khang nhat) luc kep => moc theo cang may di mat.
    ///      Neu Khang da cam moc => cang kep vao khoang trong.
    /// Tua lai truoc luc kep => moc tu tro ve mai (vi no van la ham cua thoi gian).
    ///
    /// LOI GIAI MAN 1: nhat moc o t ~ 3 (truoc khi cang toi), mang qua t = 6 (cang kep hut),
    /// toi diem moc t = 8 va di cap qua hem, cham hom thu cuoi man.
    /// Neu de mat moc roi tua >= 2 s thi NHIP len tier 1: cang toi luc ~3.5 s, van kip nhat neu nhat ngay.
    /// </summary>
    public class ClawRail : MonoBehaviour, IWarnable
    {
        [Serializable]
        public struct TierTiming
        {
            public float arriveTime;     // luc cang toi tren cho moc
            public float travelDuration; // thoi gian truot tren ray
            public Color eyeColor;
            public float eyeGlow;
            public float eyeScale;
        }

        public LevelClock clock;
        public NhipBrain brain;
        public HoldableItem targetHook;

        [Header("Parts")]
        public Transform claw;
        public Transform fingerL, fingerR;
        public Transform tether;   // day noi cang voi ray (scale theo do cao)
        public Transform eye;
        public Renderer eyeRenderer;
        public Renderer[] tintRenderers;

        [Header("Path")]
        public float railY = 5.8f;
        public float hangY = 4.9f;
        public float downY = 0.55f;
        public float parkX = 6f;
        public float targetX = -5.6f;
        public float laneZ = 0f;
        public float descendDuration = 0.4f;
        public float liftDuration = 0.5f;
        public float returnDuration = 1.5f;
        public Vector3 gripOffset = new Vector3(0f, -0.55f, 0f);

        public TierTiming[] tiers =
        {
            new TierTiming { arriveTime = 6f,   travelDuration = 1.5f, eyeColor = new Color(0.2f, 0.8f, 1f),  eyeGlow = 1.5f, eyeScale = 1f },
            new TierTiming { arriveTime = 3.5f, travelDuration = 0.8f, eyeColor = new Color(1f, 0.12f, 0.08f), eyeGlow = 8f,  eyeScale = 1.4f },
        };

        Material _eyeMat;
        Material[] _tintMats;

        public int Tier => brain ? Mathf.Clamp(brain.Tier, 0, tiers.Length - 1) : 0;
        public TierTiming Current => tiers[Tier];
        public float GrabTime => Current.arriveTime + descendDuration;

        void Awake()
        {
            if (eyeRenderer) _eyeMat = eyeRenderer.material;
            if (tintRenderers != null)
            {
                _tintMats = new Material[tintRenderers.Length];
                for (int i = 0; i < tintRenderers.Length; i++) if (tintRenderers[i]) _tintMats[i] = tintRenderers[i].material;
            }
        }

        void Start()
        {
            if (!clock) clock = LevelClock.Instance;
            if (!brain) brain = NhipBrain.Instance;
        }

        /// <summary>Vi tri cang may tai thoi diem t voi tier hien tai (ham thuan).</summary>
        public Vector3 ClawPositionAt(float t)
        {
            var c = Current;
            float a = c.arriveTime, d = c.travelDuration;
            float x = parkX, y = hangY;
            if (t < a - d) { x = parkX; y = hangY; }
            else if (t < a) { x = Mathf.Lerp(parkX, targetX, Mathf.SmoothStep(0, 1, (t - (a - d)) / d)); y = hangY; }
            else if (t < a + descendDuration) { x = targetX; y = Mathf.Lerp(hangY, downY, (t - a) / descendDuration); }
            else if (t < a + descendDuration + liftDuration) { x = targetX; y = Mathf.Lerp(downY, hangY, (t - a - descendDuration) / liftDuration); }
            else
            {
                float u = Mathf.Clamp01((t - a - descendDuration - liftDuration) / returnDuration);
                x = Mathf.Lerp(targetX, parkX, Mathf.SmoothStep(0, 1, u)); y = hangY;
            }
            return new Vector3(x, y, laneZ);
        }

        /// <summary>v2: vong do bao truoc cho cang ha xuong (bac 1 tro len).</summary>
        public bool WarnVisible => Tier >= 1 && clock && clock.CurrentTime < GrabTime + liftDuration;
        public Vector3 WarnPosition => new Vector3(targetX, 0f, laneZ);

        public bool IsClosedAt(float t) => t >= GrabTime && t < GrabTime + liftDuration + returnDuration + 0.5f;

        /// <summary>Moc bi cang lay di tai thoi diem t? Chi khi moc van la vat theo thoi gian (chua bi nhat).</summary>
        public bool HookTakenAt(float t) =>
            targetHook && targetHook.State == HoldableItem.ItemState.Timeline && t >= GrabTime;

        void LateUpdate()
        {
            if (!clock) return;
            float t = clock.CurrentTime;
            Vector3 p = ClawPositionAt(t);
            if (claw) claw.position = p;

            float open = IsClosedAt(t) ? 0.18f : 0.38f;
            if (fingerL) fingerL.localPosition = new Vector3(-open, fingerL.localPosition.y, 0f);
            if (fingerR) fingerR.localPosition = new Vector3(open, fingerR.localPosition.y, 0f);

            if (tether)
            {
                float len = Mathf.Max(0.05f, railY - p.y);
                tether.position = new Vector3(p.x, p.y + len * 0.5f, laneZ);
                tether.localScale = new Vector3(0.06f, len, 0.06f);
            }

            if (targetHook) targetHook.pickable = !HookTakenAt(t);

            // --- Hien thi tier ro rang cho nguoi cham: mat doi mau, sang hon, to hon, nhap nhay ---
            var c = Current;
            float pulse = 1f + 0.15f * Tier * Mathf.Sin(Time.unscaledTime * 8f);
            if (_eyeMat)
            {
                _eyeMat.SetColor("_BaseColor", c.eyeColor);
                _eyeMat.EnableKeyword("_EMISSION");
                _eyeMat.SetColor("_EmissionColor", c.eyeColor * c.eyeGlow * pulse);
            }
            if (eye)
            {
                eye.localScale = Vector3.one * 1.2f * c.eyeScale * pulse;
                var ph = PlayerHealth.Instance;
                if (ph) eye.LookAt(ph.transform.position); // mat nhin theo Khang
            }
            if (_tintMats != null)
                foreach (var m in _tintMats)
                    if (m)
                    {
                        m.EnableKeyword("_EMISSION");
                        m.SetColor("_EmissionColor", c.eyeColor * (0.3f + 0.6f * Tier));
                    }
        }
    }
}
