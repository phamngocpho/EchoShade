using UnityEngine;

namespace EchoShade
{
    /// <summary>Nguon cua mot vong do bao truoc (don cua NHIP danh vao mot diem: vi tri + lan).</summary>
    public interface IWarnable
    {
        bool WarnVisible { get; }
        Vector3 WarnPosition { get; }
    }

    /// <summary>
    /// VONG DO BAO TRUOC (v2 muc 3.4 bac 2, man 1-5). Vong phang tren san, nhap nhay, dat tai diem don se danh.
    /// Chi hien khi nguon (ClawRail, HangingSign, AmbushStrike...) bao WarnVisible. Mesh vong tu tao, khong can asset.
    /// </summary>
    [DefaultExecutionOrder(150)]
    public class WarningRing : MonoBehaviour
    {
        public MonoBehaviour source;
        public float radius = 0.75f, width = 0.12f;
        public Color color = new Color(1f, 0.1f, 0.08f);
        IWarnable _src;
        MeshRenderer _mr;
        Material _mat;

        void Awake()
        {
            _src = source as IWarnable;
            var mf = gameObject.GetComponent<MeshFilter>(); if (!mf) mf = gameObject.AddComponent<MeshFilter>();
            _mr = gameObject.GetComponent<MeshRenderer>(); if (!_mr) _mr = gameObject.AddComponent<MeshRenderer>();
            mf.sharedMesh = BuildRing(radius, width, 48);
            _mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _mr.receiveShadows = false;
            _mat = _mr.material;
        }

        static Mesh BuildRing(float r, float w, int seg)
        {
            var m = new Mesh { name = "WarningRing" };
            var v = new Vector3[seg * 2 + 4 * 2]; var tris = new System.Collections.Generic.List<int>();
            for (int i = 0; i < seg; i++)
            {
                float a = i * Mathf.PI * 2f / seg;
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v[i * 2] = d * (r - w); v[i * 2 + 1] = d * r;
                int j = (i + 1) % seg;
                tris.AddRange(new[] { i * 2, j * 2, i * 2 + 1, i * 2 + 1, j * 2, j * 2 + 1 });
            }
            // dau X o giua
            int o = seg * 2; float c = r * 0.45f, t = w * 0.6f;
            v[o] = new Vector3(-c, 0, -c + t); v[o + 1] = new Vector3(-c + t, 0, -c); v[o + 2] = new Vector3(c, 0, c - t); v[o + 3] = new Vector3(c - t, 0, c);
            v[o + 4] = new Vector3(-c, 0, c - t); v[o + 5] = new Vector3(-c + t, 0, c); v[o + 6] = new Vector3(c, 0, -c + t); v[o + 7] = new Vector3(c - t, 0, -c);
            tris.AddRange(new[] { o, o + 3, o + 1, o, o + 2, o + 3, o + 4, o + 5, o + 7, o + 4, o + 7, o + 6 });
            m.vertices = v; m.triangles = tris.ToArray(); m.RecalculateNormals(); m.RecalculateBounds();
            return m;
        }

        void LateUpdate()
        {
            bool on = _src != null && _src.WarnVisible;
            if (_mr.enabled != on) _mr.enabled = on;
            if (!on) return;
            var p = _src.WarnPosition; p.y += 0.03f;
            transform.position = p;
            transform.rotation = Quaternion.identity;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 7f);
            transform.localScale = Vector3.one * (1f + 0.12f * pulse);
            if (_mat)
            {
                _mat.SetColor("_BaseColor", color);
                _mat.EnableKeyword("_EMISSION");
                _mat.SetColor("_EmissionColor", color * (1.5f + 2.5f * pulse));
            }
        }
    }
}
