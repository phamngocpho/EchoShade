using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 2 - thung go nam tren duong ray (lan gan) o vi tri t = 2 s.
    /// La VAT THEO THOI GIAN cho toi khi Khang nhat:
    ///   t < luc dau tau toi thung  => nam yen tren ray, nhat duoc.
    ///   t >= luc do                 => bi tau hat vang ra ngoai ga, roi xuong duoi (mat, khong voi toi).
    /// Tua ve truoc luc tau toi => thung tro lai ray.
    /// Neu Khang DAT thung (vat neo) xuong lan gan ma tau sau do chay qua cho do, tau tra thung ve
    /// dong thoi gian goc (ResetToTimeline) => no bi hat vang; tua ve truoc luc tau den de lay lai o cho cu.
    /// </summary>
    [RequireComponent(typeof(HoldableItem))]
    public class TrackCrate : TimelineObject
    {
        public TrainCar train;
        public Vector3 restPosition;
        public float nearLaneZ = 0f;
        public Vector3 knockOffset = new Vector3(9f, -9f, -4f);
        public float knockDuration = 0.9f;

        HoldableItem _item;

        public bool IsKnockedAt(float t) => train && t >= train.ArrivalTime(restPosition.x);

        void Awake() { _item = GetComponent<HoldableItem>(); }

        public override void Evaluate(float t)
        {
            if (!IsKnockedAt(t))
            {
                transform.SetPositionAndRotation(restPosition, Quaternion.identity);
                _item.pickable = true;
                return;
            }
            float u = Mathf.Clamp01((t - train.ArrivalTime(restPosition.x)) / knockDuration);
            Vector3 p = restPosition + new Vector3(knockOffset.x * u, 2.5f * Mathf.Sin(u * Mathf.PI) + knockOffset.y * u * u, knockOffset.z * u);
            transform.SetPositionAndRotation(p, Quaternion.Euler(0f, 0f, -540f * u));
            _item.pickable = false;
        }

        void Update()
        {
            // Thung da duoc dat (neo) tren ray va tau chay qua cho do => bi hat vang (quay ve dong thoi gian goc).
            if (!_item || !clock || _item.State != HoldableItem.ItemState.Anchored || !train) return;
            if (Mathf.Abs(transform.position.z - nearLaneZ) > 0.8f) return;
            float arrive = train.ArrivalTime(transform.position.x);
            if (train.IsRunningAt(clock.CurrentTime) && clock.CurrentTime >= arrive && _item.AnchorTime < arrive)
            {
                Debug.Log("[Level2] Tau hat vang thung dang nam tren ray.");
                _item.ResetToTimeline();
            }
        }
    }
}
