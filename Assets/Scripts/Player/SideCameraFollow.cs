using UnityEngine;

namespace EchoShade
{
    /// <summary>Camera nhin ngang, hoi nghieng tu tren xuong; chi bam theo X cua Khang.</summary>
    public class SideCameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smooth = 6f;
        public float minX = -8f, maxX = 8f;
        [Tooltip("Bam theo do cao cua Khang (Man 5 co hai tang).")]
        public bool followY = false;
        public float yOffset = 4.55f;

        void LateUpdate()
        {
            if (!target) return;
            Vector3 p = transform.position;
            float x = Mathf.Clamp(target.position.x, minX, maxX);
            p.x = Mathf.Lerp(p.x, x, 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime));
            if (followY) p.y = Mathf.Lerp(p.y, target.position.y + yOffset, 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));
            transform.position = p;
        }
    }
}
