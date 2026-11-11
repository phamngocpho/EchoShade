using UnityEngine;

namespace EchoShade
{
    /// <summary>Camera nhin ngang, hoi nghieng tu tren xuong; chi bam theo X cua Khang.</summary>
    public class SideCameraFollow : MonoBehaviour
    {
        public Transform target;
        public float smooth = 6f;
        public float minX = -8f, maxX = 8f;

        void LateUpdate()
        {
            if (!target) return;
            Vector3 p = transform.position;
            float x = Mathf.Clamp(target.position.x, minX, maxX);
            p.x = Mathf.Lerp(p.x, x, 1f - Mathf.Exp(-smooth * Time.unscaledDeltaTime));
            transform.position = p;
        }
    }
}
