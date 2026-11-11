using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Noi model 3D cua Khang (Meshy, da rig) voi KhangController.
    ///  - Tham so Animator "Speed" = toc do ngang thuc te cua Khang (don vi/giay), do tu thay doi vi tri X.
    ///  - Idle (< walkThreshold): dung yen => thoi gian man dung => tu the dung yen (state Idle co speed 0).
    ///  - Walk / Run: toc do phat clip ty le voi toc do di de chan khong bi truot.
    ///  - Quay mat theo huong di (+X / -X) cho goc nhin ngang 2.5D. Di sang trai (tua nguoc) => quay mat sang trai.
    ///  - Dich chuyen tuc thi (hoi sinh, teleport) khong tinh la chay.
    /// Chi la phan hinh anh: khong dong vao collider, CharacterController hay logic thoi gian.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class KhangAnimator : MonoBehaviour
    {
        public KhangController khang;
        public Animator animator;
        [Tooltip("Pivot de xoay mat (dat o chan Khang).")]
        public Transform pivot;

        [Tooltip("Toc do di (don vi/giay) ung voi clip Walk phat 1x.")]
        public float walkAnimSpeedAt = 2.8f;
        [Tooltip("Toc do di (don vi/giay) ung voi clip Run phat 1x.")]
        public float runAnimSpeedAt = 5f;
        public float runThreshold = 3.5f;
        public float turnSpeed = 900f;
        [Tooltip(">= 0: ep gia tri Speed (de test / chup man hinh).")]
        public float debugSpeedOverride = -1f;

        public float Speed { get; private set; }

        static readonly int SpeedId = Animator.StringToHash("Speed");
        Vector3 _last;
        float _yaw = 90f;

        void Awake()
        {
            if (!khang) khang = GetComponentInParent<KhangController>();
            if (!animator) animator = GetComponentInChildren<Animator>();
            if (!pivot) pivot = transform;
            _last = khang ? khang.transform.position : transform.position;
            if (animator) animator.applyRootMotion = false;
        }

        void LateUpdate()
        {
            if (!khang || !animator) return;
            float dt = Time.deltaTime;
            if (dt <= 0f) return; // tam dung (Esc)

            Vector3 p = khang.transform.position;
            float raw = Mathf.Abs(p.x - _last.x) / dt;
            _last = p;
            if (raw > 20f) raw = 0f; // teleport / hoi sinh
            Speed = Mathf.Lerp(Speed, raw, 1f - Mathf.Exp(-15f * dt));
            if (Speed < 0.02f) Speed = 0f;
            if (debugSpeedOverride >= 0f) Speed = debugSpeedOverride;

            animator.SetFloat(SpeedId, Speed);
            float rate = Speed >= runThreshold ? Speed / runAnimSpeedAt : Speed / walkAnimSpeedAt;
            animator.speed = Speed < 0.05f ? 1f : Mathf.Clamp(rate, 0.4f, 2.5f);

            float targetYaw = khang.Facing >= 0f ? 90f : -90f;
            _yaw = Mathf.MoveTowardsAngle(_yaw, targetYaw, turnSpeed * dt);
            pivot.localRotation = Quaternion.Euler(0f, _yaw, 0f);
        }
    }
}
