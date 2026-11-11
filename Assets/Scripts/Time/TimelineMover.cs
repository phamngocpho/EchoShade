using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Vat theo thoi gian di chuyen tu startPosition den endPosition trong khoang [startTime, endTime].
    /// Truoc startTime: o startPosition. Sau endTime: o endPosition.
    /// Vi du: to giay roi o t = 1.5 s, tau quet lan gan tu t = 4 den 7 s.
    /// </summary>
    public class TimelineMover : TimelineObject
    {
        [Header("Time window (level seconds)")]
        public float startTime = 0f;
        public float endTime = 1f;

        [Header("Path (world space)")]
        public Vector3 startPosition;
        public Vector3 endPosition;
        [Tooltip("Duong cong 0..1 -> 0..1 (vd: roi tang toc).")]
        public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Visibility")]
        [Tooltip("An renderer/collider khi t nam ngoai cua so thoi gian (vd: tau chi xuat hien khi chay).")]
        public bool hideOutsideWindow = false;

        Renderer[] _renderers;
        Collider[] _colliders;

        void Awake()
        {
            _renderers = GetComponentsInChildren<Renderer>(true);
            _colliders = GetComponentsInChildren<Collider>(true);
        }

        public override void Evaluate(float t)
        {
            float u = Mathf.Approximately(endTime, startTime)
                ? (t >= endTime ? 1f : 0f)
                : Mathf.Clamp01((t - startTime) / (endTime - startTime));
            transform.position = Vector3.LerpUnclamped(startPosition, endPosition, curve.Evaluate(u));

            if (hideOutsideWindow)
            {
                bool visible = t >= startTime && t <= endTime;
                foreach (var r in _renderers) if (r) r.enabled = visible;
                foreach (var c in _colliders) if (c) c.enabled = visible;
            }
        }
    }
}
