using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 3 - MAT cua thap NHIP (phan phat sang rieng, tach khoi than thap).
    /// Nhin: khe that (mac dinh) -> quay dan sang Khang khi Khang dung o khe gia -> lan gan khi bi lua
    ///      -> bam theo Bong o tier 3 -> cui xuong khi da doc thu.
    /// Mau: theo tier (xanh / vang / do), trang mo khi dung.
    /// </summary>
    public class TowerEye : MonoBehaviour
    {
        public NhipTowerMind mind;
        public NhipBrain brain;
        public EchoShadeReplay echo;
        public Transform eye;
        public Renderer eyeRenderer;
        public Transform nearSlot, farSlot;
        public float turnSpeed = 5f;
        public Color[] tierColors = { new Color(0.2f, 0.8f, 1f), new Color(0.2f, 0.8f, 1f), new Color(1f, 0.7f, 0.15f), new Color(1f, 0.1f, 0.08f) };
        public Color stoppedColor = new Color(0.8f, 0.85f, 0.9f);

        Material _mat;

        void Start()
        {
            if (!mind) mind = NhipTowerMind.Instance;
            if (!brain) brain = NhipBrain.Instance;
            if (eyeRenderer) _mat = eyeRenderer.material;
        }

        void LateUpdate()
        {
            if (!mind || !eye) return;
            var k = PlayerHealth.Instance ? PlayerHealth.Instance.transform : null;
            Vector3 look;
            if (mind.Delivered) look = eye.position + new Vector3(0f, -1f, -1.5f);
            else if (echo && echo.Active && echo.visual) look = echo.visual.position + Vector3.up;
            else if (mind.StareProgress > 0f && k) look = Vector3.Lerp(farSlot.position, k.position, mind.StareProgress);
            else look = (mind.Fooled ? nearSlot : farSlot).position;

            // Pupil nam o phia -Z cua mat => huong -Z ve muc tieu
            var want = Quaternion.LookRotation(eye.position - look, Vector3.up);
            eye.rotation = Quaternion.Slerp(eye.rotation, want, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));

            if (_mat)
            {
                Color c; float glow;
                if (mind.Delivered) { c = stoppedColor; glow = 0.4f; }
                else
                {
                    int t = brain ? Mathf.Clamp(brain.Tier, 0, tierColors.Length - 1) : 1;
                    c = tierColors[t];
                    glow = 1.2f + 0.6f * t + (mind.StareProgress > 0f && mind.StareProgress < 1f ? 1.5f * Mathf.Abs(Mathf.Sin(Time.time * 10f)) : 0f);
                }
                _mat.SetColor("_BaseColor", c);
                _mat.EnableKeyword("_EMISSION");
                _mat.SetColor("_EmissionColor", c * glow);
            }
        }
    }
}
