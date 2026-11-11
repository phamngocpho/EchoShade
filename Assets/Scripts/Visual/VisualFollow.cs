using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Visual di theo mot vat logic nhung KHONG la con cua no.
    /// Ly do: Hazard tinh vung sat thuong tu tat ca Renderer con; neu visual xoay/lon hon nam ben trong,
    /// vung sat thuong se bi doi. Dat visual ra ngoai va cho no bam theo => vung sat thuong giu nguyen.
    /// Chay sau TimelineObject (LateUpdate) de lay vi tri moi nhat cua khung.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class VisualFollow : MonoBehaviour
    {
        public Transform target;
        [Tooltip("Lech vi tri so voi target (toa do the gioi).")]
        public Vector3 offset;
        [Tooltip("Neu co: an/hien visual theo renderer nay (vd: placeholder bi TrainCar tat).")]
        public Renderer mirrorRenderer;

        Renderer[] _rs;

        void Awake() { _rs = GetComponentsInChildren<Renderer>(true); }

        void LateUpdate()
        {
            if (!target) return;
            transform.position = target.position + offset;
            if (mirrorRenderer)
            {
                bool on = mirrorRenderer.enabled && mirrorRenderer.gameObject.activeInHierarchy;
                foreach (var r in _rs) if (r) r.enabled = on;
            }
        }
    }
}
