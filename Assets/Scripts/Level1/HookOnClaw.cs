using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 1 - cai moc. Khi chua bi nhat no la VAT THEO THOI GIAN:
    ///   t < luc kep  => nam tren mai (restPosition)
    ///   t >= luc kep => nam trong cang may (bi mang di)
    /// Tua ve truoc luc kep => moc tro lai mai. Khi Khang cam moc (HoldableItem.Held) no roi khoi dong thoi gian
    /// va di theo Khang ke ca khi tua.
    /// </summary>
    [RequireComponent(typeof(HoldableItem))]
    public class HookOnClaw : TimelineObject
    {
        public ClawRail claw;
        public Vector3 restPosition;

        public override void Evaluate(float t)
        {
            if (claw && claw.HookTakenAt(t))
                transform.position = claw.ClawPositionAt(t) + claw.gripOffset;
            else
                transform.position = restPosition;
            transform.rotation = Quaternion.identity;
        }
    }
}
