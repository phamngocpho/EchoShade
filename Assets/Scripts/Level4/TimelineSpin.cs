using UnityEngine;

namespace EchoShade
{
    /// <summary>Banh rang / bang chuyen quay theo THOI GIAN MAN (goc = t * degPerSecond): Khang dung im thi banh rang dung.</summary>
    public class TimelineSpin : TimelineObject
    {
        public Vector3 axis = Vector3.forward;
        public float degPerSecond = 90f;
        Quaternion _base;
        bool _init;
        public override void Evaluate(float t)
        {
            if (!_init) { _base = transform.localRotation; _init = true; }
            transform.localRotation = _base * Quaternion.AngleAxis(t * degPerSecond, axis);
        }
    }
}
