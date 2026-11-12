using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// MAT NHIP (v2 muc 1 + 7 "NhipEyeView"): chi hien thi. Mau theo bac (0 xanh, 1 vang, 2 cam, 3 do),
    /// sang + to dan theo bac, nhin theo Khang (hoac theo Anh vong khi bong dang di). Mat lon dan qua 5 man.
    /// </summary>
    [DefaultExecutionOrder(160)]
    public class NhipEyeView : MonoBehaviour
    {
        public NhipBrain brain;
        public Renderer eyeRenderer;
        public Transform lookPivot;
        [Tooltip("Pupil o phia -Z cua mat (prefab NhipTower) => dao huong.")]
        public bool pupilOnNegZ = false;
        public bool lookAtGhost = true;
        public float baseScale = 1f;
        public float turnSpeed = 5f;
        [Tooltip("Khi bat: mat tat (vd: thap da doc thu).")]
        public bool off;
        public Transform overrideTarget;

        Material _mat;
        Vector3 _scale0;

        void Start()
        {
            if (!brain) brain = NhipBrain.Instance;
            if (eyeRenderer) _mat = eyeRenderer.material;
            if (lookPivot) _scale0 = lookPivot.localScale;
        }

        void LateUpdate()
        {
            int t = brain ? brain.Tier : 0;
            if (lookPivot)
            {
                Vector3 target;
                var g = GhostPlayer.Instance;
                if (off) target = lookPivot.position + new Vector3(0f, -1f, -1.5f);
                else if (overrideTarget) target = overrideTarget.position;
                else if (lookAtGhost && g && g.Active) target = g.Position + Vector3.up;
                else target = PlayerHealth.Instance ? PlayerHealth.Instance.transform.position : lookPivot.position + Vector3.back;
                Vector3 d = pupilOnNegZ ? lookPivot.position - target : target - lookPivot.position;
                if (d.sqrMagnitude > 1e-4f)
                    lookPivot.rotation = Quaternion.Slerp(lookPivot.rotation, Quaternion.LookRotation(d, Vector3.up), 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
                float pulse = off ? 1f : 1f + 0.06f * t * Mathf.Sin(Time.time * (4f + 2f * t));
                lookPivot.localScale = _scale0 * baseScale * (off ? 1f : (1f + 0.08f * t)) * pulse;
            }
            if (_mat)
            {
                Color c = off ? new Color(0.25f, 0.27f, 0.3f) : NhipBrain.TierColor(t);
                _mat.SetColor("_BaseColor", c);
                _mat.EnableKeyword("_EMISSION");
                _mat.SetColor("_EmissionColor", c * (off ? 0.05f : 1.4f + 1.1f * t));
            }
        }
    }
}
