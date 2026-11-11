using System.Collections;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 1 - day cap qua hem (diem moc o t = 8 s).
    /// Co moc trong tay: di toi diem moc (di sang phai) hoac bam E => Khang mac moc vao cap va truot qua hem.
    /// Khong co moc: buoc vao hem la roi (KillZone: -1 HP, ve hom thu).
    /// Khi truot, X tang nen thoi gian van chay thuan theo dung quy tac.
    /// </summary>
    public class CableCrossing : MonoBehaviour
    {
        public HoldableItem requiredItem;
        public KhangController khang;
        public float hookPointX = 8.4f;
        public float zoneHalfWidth = 0.5f;
        public float landingX = 11.2f;
        public float crossDuration = 1.0f;
        public float sag = 0.35f;

        public bool IsCrossing { get; private set; }
        public int Crossings { get; private set; }

        bool HasHook => requiredItem && requiredItem.State == HoldableItem.ItemState.Held;
        bool InZone => khang && Mathf.Abs(khang.transform.position.x - hookPointX) <= zoneHalfWidth;

        void OnEnable() { KhangInteractor.InteractOverride += OnInteract; }
        void OnDisable() { KhangInteractor.InteractOverride -= OnInteract; }

        bool OnInteract()
        {
            if (IsCrossing || !HasHook || !InZone) return false;
            StartCoroutine(Cross());
            return true;
        }

        void Update()
        {
            if (IsCrossing || !HasHook || !InZone || !khang.IsGrounded) return;
            var clock = LevelClock.Instance;
            bool movingRight = KhangInput.Horizontal > 0.1f || (clock && clock.Direction == TimeDirection.Forward);
            if (movingRight) StartCoroutine(Cross());
        }

        IEnumerator Cross()
        {
            IsCrossing = true;
            var cc = khang.GetComponent<CharacterController>();
            khang.enabled = false;
            cc.enabled = false;
            Vector3 a = khang.transform.position;
            Vector3 b = new Vector3(landingX, a.y, a.z);
            float u = 0f;
            while (u < 1f)
            {
                u = Mathf.Min(1f, u + Time.deltaTime / crossDuration);
                Vector3 p = Vector3.Lerp(a, b, Mathf.SmoothStep(0, 1, u));
                p.y -= Mathf.Sin(u * Mathf.PI) * sag;
                khang.transform.position = p;
                yield return null;
            }
            khang.enabled = true;
            khang.Teleport(b);
            Crossings++;
            IsCrossing = false;
            Debug.Log("[Level1] Khang da qua hem bang day cap.");
        }
    }
}
