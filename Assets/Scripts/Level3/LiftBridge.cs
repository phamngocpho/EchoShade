using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// CAU NANG (Man 3, lan gan G10-14). Co tai tren ban dap => cau ha xuong (xoay ve nam ngang) va giu nguyen.
    /// Khong co tai => cau dung dung, lan gan la khe ho qua kenh (roi = mat mau, ve thung thu).
    /// Trang thai theo ban dap (thoi gian thuc), khong theo thuoc: day la "neo co hieu luc o tuong lai".
    /// </summary>
    public class LiftBridge : MonoBehaviour
    {
        public LoadPedal pedal;
        public Transform deck;           // pivot o dau trai cua cau
        public Collider deckCollider;
        public float raisedAngle = 80f;  // goc khi dung len
        public float speed = 70f;        // do / giay
        [Header("Goi y ngo cut (v2)")]
        public float hintAtTime = 9.4f;
        public string hint = "Cái cầu cần một thứ nặng đặt ở xa hơn.";

        public bool IsDown { get; private set; }
        float _angle;
        bool _hinted;

        void Start() { _angle = raisedAngle; Apply(); }

        void Update()
        {
            float want = pedal && pedal.IsLoaded ? 0f : raisedAngle;
            _angle = Mathf.MoveTowards(_angle, want, speed * Time.deltaTime);
            Apply();
            var clock = LevelClock.Instance;
            if (!_hinted && clock && clock.CurrentTime >= hintAtTime && !(pedal && pedal.IsLoaded))
            {
                _hinted = true;
                if (HUD.Instance) HUD.Instance.ShowHint(hint, 7f);
            }
        }

        void Apply()
        {
            if (deck) deck.localRotation = Quaternion.Euler(0f, 0f, _angle);
            IsDown = _angle < 0.5f;
            if (deckCollider) deckCollider.enabled = IsDown;
        }
    }
}
