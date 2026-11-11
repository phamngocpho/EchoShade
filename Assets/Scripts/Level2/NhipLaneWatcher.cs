using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 2 - NHIP quan sat lan Khang dung trong khu tau (t = 4..8).
    /// Quan sat chi duoc CHOT vao bang ghi nho khi Khang tua hop le (xem NhipBrain.HandleValidRewind),
    /// nen bien treo o tier 2 se nham vao lan Khang da dung o dong thoi gian vua bi xoa.
    /// </summary>
    public class NhipLaneWatcher : MonoBehaviour
    {
        public NhipBrain brain;
        public LevelClock clock;
        public KhangController khang;
        public string zone = "L2_TrainZone";
        public float fromTime = 4f, toTime = 8f;

        void Start()
        {
            if (!brain) brain = NhipBrain.Instance;
            if (!clock) clock = LevelClock.Instance;
            if (!khang) khang = FindAnyObjectByType<KhangController>();
        }

        void LateUpdate()
        {
            if (!brain || !clock || !khang) return;
            float t = clock.CurrentTime;
            if (t >= fromTime && t < toTime) brain.ObserveLane(zone, (int)khang.CurrentLane);
        }
    }
}
