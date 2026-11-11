using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// VAT THEO THOI GIAN (timeline object) - lop co so.
    ///
    /// Trang thai cua vat la HAM cua thoi gian man: state = f(LevelClock.CurrentTime).
    /// Vat khong luu lich su, moi khung chi hoi "o thoi diem t thi toi o dau?".
    /// Vi vay khi Khang lui (tua nguoc), vat tu dong tro ve dung trang thai cu.
    ///
    /// Neu vat bi Khang cam (Held) hoac dat xuong (Anchored) thi FollowsClock = false
    /// va vat ngung doc dong ho (xem HoldableItem).
    /// </summary>
    public abstract class TimelineObject : MonoBehaviour
    {
        [Tooltip("De trong se tu dung LevelClock.Instance.")]
        public LevelClock clock;

        /// <summary>Tat de vat ngung doc dong ho (dang cam / da neo).</summary>
        public bool FollowsClock { get; set; } = true;

        protected virtual void Start()
        {
            if (!clock) clock = LevelClock.Instance ? LevelClock.Instance : FindAnyObjectByType<LevelClock>();
            if (clock && FollowsClock) Evaluate(clock.CurrentTime);
        }

        protected virtual void LateUpdate()
        {
            if (!clock || !FollowsClock) return;
            Evaluate(clock.CurrentTime);
        }

        /// <summary>Dat trang thai cua vat tai thoi diem t (giay). Phai la ham thuan: cung t => cung ket qua.</summary>
        public abstract void Evaluate(float t);
    }
}
