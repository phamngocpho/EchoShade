using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 3 - BONG (Echo Shade), chi xuat hien khi NHIP dat tier 3 va dang tin Khang o lan gan.
    ///  - Bong phat lai duong lan gan Khang da di (committedPath, theo giay thuc x replaySpeed),
    ///    bat dau moi luot khi Khang vuot qua nga ba (t >= junctionTime + startDelay).
    ///  - Mat NHIP bam theo Bong ("giu NHIP o lan gan" trong luc Bong di).
    ///  - Neu Bong toi khe gia ma khong giao thu => NHIP nhan ra bi lua (NhipTowerMind.ExposeTrick).
    ///  - Vat neo (HoldableItem Anchored do Khang dat, AnchorTime >= 0) nam tren duong lan gan
    ///    phia truoc Bong => Bong dung lai truoc vat neo va khong bao gio toi khe gia.
    /// Bong khong gay sat thuong; no chi la "bang chung" cua NHIP.
    /// </summary>
    public class EchoShadeReplay : MonoBehaviour
    {
        public NhipTowerMind mind;
        public NhipBrain brain;
        public LevelClock clock;
        public L3Junction junction;
        [Tooltip("Instance prefab EchoShade (Khang ban mo).")]
        public Transform visual;
        public Animator animator;

        public int activeFromTier = 3;
        public float replaySpeed = 1.3f;
        public float maxSpeed = 5.2f;
        [Tooltip("Bong xuat phat khi thoi gian man vuot qua nga ba them bao nhieu giay.")]
        public float startDelay = 0.5f;
        public float blockGap = 0.7f;
        public float laneZ = 0f;

        public bool Active { get; private set; }
        public bool Stopped { get; private set; }
        public int ExposeCount { get; private set; }
        public float X { get; private set; }
        public HoldableItem BlockedBy { get; private set; }

        float _tau;

        void Start()
        {
            if (!mind) mind = NhipTowerMind.Instance;
            if (!brain) brain = NhipBrain.Instance;
            if (!clock) clock = LevelClock.Instance;
            if (!junction) junction = FindAnyObjectByType<L3Junction>();
            Show(false);
        }

        bool CanRun => mind && brain && !mind.Delivered && brain.Tier >= activeFromTier && mind.Fooled && mind.committedPath.Count > 0;

        void Update()
        {
            if (!CanRun) { Active = false; Show(false); return; }
            float t = clock.CurrentTime, jt = junction ? junction.junctionTime : 2f;

            // Khang quay ve truoc nga ba => luot moi (bong cho xuat phat lai)
            if (Active && t < jt - 0.1f) { Active = false; Show(false); }
            // Vat neo bi nhac len => bong di tiep
            if (Active && Stopped && (!BlockedBy || BlockedBy.State != HoldableItem.ItemState.Anchored))
            { Stopped = false; BlockedBy = null; }

            if (!Active)
            {
                if (t < jt + startDelay) { Show(false); return; }
                Active = true; Stopped = false; BlockedBy = null; _tau = 0f;
                X = mind.committedPath[0].y;
                Show(true);
                if (HUD.Instance) HUD.Instance.ShowSubtitle("Bóng của anh sẽ đi lại đường cũ. Tôi sẽ xem nó làm gì.", 4f, true);
            }

            float speed = 0f;
            if (!Stopped)
            {
                _tau += Time.deltaTime * replaySpeed;
                float target = PathX(_tau);
                float nx = Mathf.MoveTowards(X, target, maxSpeed * Time.deltaTime);
                var a = AnchorAhead(X);
                if (a && nx > a.transform.position.x - blockGap)
                {
                    nx = Mathf.Max(X, a.transform.position.x - blockGap);
                    Stopped = true; BlockedBy = a;
                    Debug.Log($"[Echo] Bong bi vat neo {a.name} chan tai X={nx:0.00}");
                    if (HUD.Instance) HUD.Instance.ShowSubtitle("...Bóng không qua được vật neo. Tôi vẫn tin anh ở làn gần.", 4f, true);
                }
                speed = Time.deltaTime > 0f ? (nx - X) / Time.deltaTime : 0f;
                X = nx;
                if (!Stopped && X >= mind.SlotX - 0.3f)
                {
                    ExposeCount++;
                    Debug.Log("[Echo] Bong toi khe gia => NHIP biet bi lua");
                    mind.ExposeTrick();
                    Active = false; Show(false);
                    return;
                }
            }
            if (visual)
            {
                visual.position = new Vector3(X, 0f, laneZ);
                visual.rotation = Quaternion.Euler(0f, 90f, 0f);
            }
            if (animator) animator.SetFloat("Speed", Mathf.Abs(speed));
        }

        float PathX(float tau)
        {
            var p = mind.committedPath;
            if (tau <= p[0].x) return p[0].y;
            for (int i = 1; i < p.Count; i++)
                if (tau <= p[i].x)
                {
                    float u = Mathf.InverseLerp(p[i - 1].x, p[i].x, tau);
                    return Mathf.Lerp(p[i - 1].y, p[i].y, u);
                }
            return p[p.Count - 1].y;
        }

        HoldableItem AnchorAhead(float x)
        {
            HoldableItem best = null;
            float bx = float.MaxValue;
            foreach (var it in HoldableItem.All)
            {
                if (!it || it.State != HoldableItem.ItemState.Anchored || it.AnchorTime < 0f) continue;
                Vector3 p = it.transform.position;
                if (Mathf.Abs(p.z - laneZ) > 0.8f) continue;
                if (p.x < x - 0.05f || p.x > mind.SlotX) continue;
                if (p.x < bx) { bx = p.x; best = it; }
            }
            return best;
        }

        void Show(bool on)
        {
            if (visual && visual.gameObject.activeSelf != on) visual.gameObject.SetActive(on);
        }
    }
}
