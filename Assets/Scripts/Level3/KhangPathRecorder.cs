using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 3 - ghi lai duong Khang di o LAN GAN tu nga ba toi khe gia (theo thoi gian thuc).
    /// Khi NHIP chot quyet dinh "lan gan", duong nay duoc chep lai de Bong (Echo Shade) phat lai o tier 3.
    /// Mau = (giay thuc tu luc bat dau ghi, X).
    /// </summary>
    public class KhangPathRecorder : MonoBehaviour
    {
        public KhangController khang;
        public L3Junction junction;
        public NhipTowerMind mind;
        public float sampleInterval = 0.05f;

        readonly List<Vector2> _samples = new List<Vector2>();
        float _t;
        bool _recording;

        public IReadOnlyList<Vector2> Samples => _samples;
        public bool IsRecording => _recording;

        void Start()
        {
            if (!khang) khang = FindAnyObjectByType<KhangController>();
            if (!junction) junction = FindAnyObjectByType<L3Junction>();
            if (!mind) mind = NhipTowerMind.Instance;
        }

        /// <summary>Bat dau luot moi (sau moi lan NHIP danh gia mot lan tua, hoac Khang chet).</summary>
        public void ResetPass() { _samples.Clear(); _recording = false; _t = 0f; }

        void Update()
        {
            if (!khang || !junction || !mind || mind.Delivered) return;
            if (mind.StaredThisPass) return; // duong da hoan tat, giu nguyen cho toi luc chot
            float x = khang.transform.position.x;
            bool near = khang.CurrentLane == KhangController.Lane.Near;
            if (!near || x < junction.X - 0.2f)
            {
                // Ra khoi lan gan / ve truoc nga ba => bo luot ghi dang do
                if (_recording) ResetPass();
                return;
            }
            if (!_recording) { _recording = true; _t = 0f; _samples.Clear(); _samples.Add(new Vector2(0f, x)); return; }
            _t += Time.deltaTime;
            if (_t - _samples[_samples.Count - 1].x >= sampleInterval) _samples.Add(new Vector2(_t, x));
        }

        /// <summary>
        /// Chep duong da ghi thanh duong phat lai: luon bat dau tu nga ba; neu mau dau o xa nga ba
        /// (vd: Khang dich chuyen), chen diem nga ba va gia thoi gian di bo voi toc do moveSpeed.
        /// </summary>
        public List<Vector2> BuildReplayPath(float moveSpeed, float slotX)
        {
            var path = new List<Vector2>();
            float jx = junction ? junction.X : -8.4f;
            if (_samples.Count == 0)
            {
                path.Add(new Vector2(0f, jx));
                path.Add(new Vector2((slotX - jx) / moveSpeed, slotX));
                return path;
            }
            float shift = 0f;
            if (_samples[0].y > jx + 0.5f)
            {
                path.Add(new Vector2(0f, jx));
                shift = (_samples[0].y - jx) / Mathf.Max(0.1f, moveSpeed);
            }
            foreach (var s in _samples) path.Add(new Vector2(s.x + shift, s.y));
            return path;
        }
    }
}
