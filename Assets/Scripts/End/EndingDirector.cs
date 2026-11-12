using UnityEngine;

namespace EchoShade
{
    /// <summary>Dan canh man ket (chi hinh anh, thoi gian thuc).</summary>
    public class EndingDirector : MonoBehaviour
    {
        public enum Mode { Lang, Vong }
        public Mode mode;
        [Header("Chung")]
        public Renderer towerEye;
        public Renderer[] towerRings;
        [Header("Lang")]
        public Transform trainCar;
        public Vector3 trainFrom, trainTo;
        public float trainSeconds = 7f;
        public Transform ghostStill;
        [Header("Vong")]
        public Transform walker;
        public Animator walkerAnim;
        public float walkFromX = -6f, walkToX = 6f, walkSpeed = 1.6f;
        public Renderer[] cityLights;

        float _t;
        Material _eye;
        Renderer[] _ghostRs;
        Material[] _city;

        void OnEnable()
        {
            _t = 0f;
            if (towerEye) _eye = towerEye.material;
            if (ghostStill) _ghostRs = ghostStill.GetComponentsInChildren<Renderer>(true);
            if (cityLights != null) { _city = new Material[cityLights.Length]; for (int i = 0; i < cityLights.Length; i++) if (cityLights[i]) _city[i] = cityLights[i].material; }
        }

        void Update()
        {
            _t += Time.deltaTime;
            if (_eye)
            {
                float k = Mathf.Clamp01(1f - _t / 3f);
                Color c = Color.Lerp(new Color(0.2f, 0.22f, 0.25f), new Color(1f, 0.1f, 0.06f), k);
                _eye.SetColor("_BaseColor", c); _eye.EnableKeyword("_EMISSION"); _eye.SetColor("_EmissionColor", c * (0.1f + 3f * k));
            }
            if (towerRings != null)
                for (int i = 0; i < towerRings.Length; i++) if (towerRings[i]) towerRings[i].enabled = _t < 0.8f + i * 0.7f;

            if (mode == Mode.Lang)
            {
                if (trainCar) trainCar.position = Vector3.Lerp(trainFrom, trainTo, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((_t - 1f) / trainSeconds)));
                if (ghostStill)
                {
                    bool vis = _t < 4f || (_t < 6f && Mathf.Repeat(_t * 6f, 1f) > (_t - 4f) / 2f);
                    foreach (var r in _ghostRs) if (r) r.enabled = vis;
                }
            }
            else
            {
                if (walker)
                {
                    float len = walkToX - walkFromX;
                    float u = Mathf.PingPong(_t * walkSpeed, len);
                    float dir = Mathf.Repeat(_t * walkSpeed, 2f * len) < len ? 1f : -1f;
                    var p = walker.position; p.x = walkFromX + u; walker.position = p;
                    walker.rotation = Quaternion.Euler(0f, dir > 0 ? 90f : -90f, 0f);
                    if (walkerAnim) { walkerAnim.SetFloat("Speed", walkSpeed); walkerAnim.speed = Mathf.Clamp(walkSpeed / 2.8f, 0.4f, 2f); }
                }
                if (_city != null)
                {
                    float k = Mathf.Clamp01((_t - 1.5f) / 3f);
                    foreach (var m in _city) if (m) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.4f) * (0.6f + 1.6f * k)); }
                }
            }
        }
    }
}
