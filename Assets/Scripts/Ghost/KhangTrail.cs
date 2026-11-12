using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>Mot mau duong di cua Khang (thoi gian THUC + vi tri + thoi gian man).</summary>
    public struct TrailSample
    {
        public float rt;     // giay thuc (tinh tu dau doan)
        public Vector3 pos;  // vi tri transform cua Khang
        public float t;      // thoi gian man luc lay mau
        public int lane;     // 0 gan, 1 xa
    }

    /// <summary>
    /// GHI DUONG DI CUA KHANG (GhostRecorder, v2 muc 3.6 + 7).
    /// Lay mau 30 lan / giay thuc (khong phu thuoc fps): vi tri, lan, nhay (y), dung im (vi tri khong doi).
    /// Khi co lan tua hop le, ExtractRewoundSegment() cat ra "doan di tien gan nhat bi tua":
    ///   tu mau cuoi cung o thoi diem quay dau (LastRewindTo) toi mau luc bat dau lui (LastRewindFrom).
    /// Doan nay dung cho (1) bang nho NHIP (lan, cho dung) va (2) quy dao cua Anh vong.
    /// Dung im qua lau bi nen lai toi maxStillSeconds de bong khong dung cho vo han.
    /// </summary>
    [DefaultExecutionOrder(-95)]
    public class KhangTrail : MonoBehaviour
    {
        public static KhangTrail Instance { get; private set; }
        public float sampleRate = 30f;
        public int maxSamples = 6000;
        public float maxStillSeconds = 2.5f;

        readonly List<TrailSample> _samples = new List<TrailSample>();
        KhangController _khang;
        float _acc, _rt;

        void Awake() { Instance = this; _khang = GetComponent<KhangController>(); }
        void OnDestroy() { if (Instance == this) Instance = null; }

        public void Clear() { _samples.Clear(); _rt = 0f; _acc = 0f; }

        void Update()
        {
            var clock = LevelClock.Instance;
            if (!clock || Time.deltaTime <= 0f) return;
            _acc += Time.deltaTime;
            float step = 1f / sampleRate;
            while (_acc >= step)
            {
                _acc -= step; _rt += step;
                _samples.Add(new TrailSample { rt = _rt, pos = transform.position, t = clock.CurrentTime, lane = _khang ? (int)_khang.CurrentLane : 0 });
            }
            if (_samples.Count > maxSamples) _samples.RemoveRange(0, _samples.Count - maxSamples);
        }

        /// <summary>Doan di tien vua bi tua (rt bat dau tu 0). Xoa bo dem va bat dau ghi doan moi.</summary>
        public List<TrailSample> ExtractRewoundSegment(float fromTime, float toTime)
        {
            var seg = new List<TrailSample>();
            int iEnd = -1;
            for (int i = _samples.Count - 1; i >= 0; i--)
                if (_samples[i].t >= fromTime - 0.02f) { iEnd = i; break; }
            if (iEnd < 0) { Clear(); return seg; }
            int iStart = 0;
            for (int i = iEnd; i >= 0; i--)
                if (_samples[i].t <= toTime + 0.02f) { iStart = i; break; }
            float rt0 = _samples[iStart].rt, shift = 0f, still = 0f;
            for (int i = iStart; i <= iEnd; i++)
            {
                var s = _samples[i];
                if (i > iStart)
                {
                    var p = _samples[i - 1];
                    float dt = s.rt - p.rt;
                    bool same = (s.pos - p.pos).sqrMagnitude < 1e-6f;
                    if (same) { still += dt; if (still > maxStillSeconds) shift += dt; }
                    else still = 0f;
                    if (same && still > maxStillSeconds) continue; // nen doan dung qua lau
                }
                s.rt = s.rt - rt0 - shift;
                seg.Add(s);
            }
            Clear();
            return seg;
        }
    }
}
