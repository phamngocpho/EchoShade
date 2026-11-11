using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 1 - tam ton roi o t = 1.5 s, chan LAN GAN ngay gan diem xuat phat.
    /// Doan tu hom thu dau den day ngan hon 2 s nen lui lai o day KHONG BAO GIO tinh la tua hop le
    /// (NHIP khong tang cap). Loi giai: dung lai, len lan xa (W), di qua, xuong lan gan (S).
    /// Dang roi = nguy hiem (-1 HP neu trung). Da nam xuong = vat can (collider bat).
    /// </summary>
    public class FallingSheet : TimelineMover
    {
        public Hazard hazard;
        Collider _solid;

        public override void Evaluate(float t)
        {
            base.Evaluate(t);
            if (!_solid) _solid = GetComponent<Collider>();
            bool landed = t >= endTime;
            if (_solid) _solid.enabled = landed;
            if (hazard) hazard.active = t > startTime && !landed;
        }
    }
}
