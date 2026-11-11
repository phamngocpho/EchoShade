using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Vat cam duoc (vd: thung go). Ba trang thai:
    ///  - Timeline : vat theo thoi gian (neu co TimelineObject tren cung GameObject thi doc dong ho).
    ///  - Held     : vat dang cam - gan vao Khang, KHONG doc dong ho.
    ///  - Anchored : vat neo - da dat xuong, ghi lai thoi diem + vi tri dat,
    ///               KHONG bi tua nguoc xoa (vi no khong con la ham cua thoi gian).
    /// </summary>
    public class HoldableItem : MonoBehaviour
    {
        public enum ItemState { Timeline, Held, Anchored }

        public static readonly List<HoldableItem> All = new List<HoldableItem>();

        [Tooltip("Trang thai ban dau. Vat khong co TimelineObject nen de Anchored.")]
        public ItemState startState = ItemState.Anchored;

        [Tooltip("Tat = khong nhat duoc luc nay (vd: moc dang nam trong cang may NHIP).")]
        public bool pickable = true;

        public ItemState State { get; private set; }
        /// <summary>Thoi gian man (giay) luc vat duoc dat xuong lan cuoi.</summary>
        public float AnchorTime { get; private set; } = -1f;
        public Vector3 AnchorPosition { get; private set; }

        TimelineObject _timeline;
        Collider[] _colliders;

        void Awake()
        {
            _timeline = GetComponent<TimelineObject>();
            _colliders = GetComponentsInChildren<Collider>(true);
            State = startState;
            if (State == ItemState.Anchored) AnchorPosition = transform.position;
            if (_timeline) _timeline.FollowsClock = State == ItemState.Timeline;
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() { All.Remove(this); }

        public void PickUp(Transform holder, Vector3 localOffset)
        {
            State = ItemState.Held;
            if (_timeline) _timeline.FollowsClock = false; // ngung doc dong ho
            SetColliders(false); // tranh va cham voi CharacterController cua Khang
            transform.SetParent(holder, true);
            transform.localPosition = localOffset;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Dat xuong tai worldPos (Y duoc dua xuong mat dat). Tro thanh vat neo.</summary>
        public void Place(Vector3 worldPos)
        {
            transform.SetParent(null, true);
            float halfH = GetHalfHeight();
            Vector3 origin = new Vector3(worldPos.x, worldPos.y + 3f, worldPos.z);
            if (Physics.Raycast(origin, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore))
                worldPos.y = hit.point.y + halfH;
            transform.SetPositionAndRotation(worldPos, Quaternion.identity);

            State = ItemState.Anchored;
            var clock = LevelClock.Instance;
            AnchorTime = clock ? clock.CurrentTime : 0f;
            AnchorPosition = worldPos;
            SetColliders(true);
        }

        /// <summary>Tra vat ve dong thoi gian cua man (vd: tau hat vang thung dang nam tren ray).</summary>
        public void ResetToTimeline()
        {
            transform.SetParent(null, true);
            State = ItemState.Timeline;
            AnchorTime = -1f;
            if (_timeline) _timeline.FollowsClock = true;
            SetColliders(true);
        }

        float GetHalfHeight()
        {
            var r = GetComponentInChildren<Renderer>();
            return r ? r.bounds.extents.y : 0.5f;
        }

        void SetColliders(bool on)
        {
            foreach (var c in _colliders) if (c) c.enabled = on;
        }
    }
}
