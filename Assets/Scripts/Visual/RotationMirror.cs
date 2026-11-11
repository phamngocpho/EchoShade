using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Xoay visual theo goc xoay cua mot transform logic (vd: tay cam can gat cu cua Man 2).
    /// factor = ty le goc (0.6 = nghieng 60% goc cua tay cam). Chuyen dong muot theo thoi gian thuc.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class RotationMirror : MonoBehaviour
    {
        public Transform target;
        public float factor = 0.6f;
        public float smooth = 10f;

        Quaternion _base, _targetBase;

        void Start()
        {
            _base = transform.localRotation;
            _targetBase = target ? target.localRotation : Quaternion.identity;
        }

        void LateUpdate()
        {
            if (!target) return;
            var delta = target.localRotation * Quaternion.Inverse(_targetBase);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            if (axis.sqrMagnitude < 1e-6f) axis = Vector3.forward;
            var want = Quaternion.AngleAxis(angle * factor, axis) * _base;
            transform.localRotation = Quaternion.Slerp(transform.localRotation, want, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        }
    }
}
