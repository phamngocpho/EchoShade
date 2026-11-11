using System.Collections;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 2 - can cua o t = 9 (lan xa). Dat thung (E) len can => thung thanh VAT NEO
    /// (ghi lai thoi diem dat, khong bi tua xoa) va cua mo vinh vien.
    /// Vi vi tri = thoi gian, Khang chi toi duoc can khi t ~ 9 nen dieu kien "t >= 9" tu nhien dung.
    /// </summary>
    public class DoorLever : MonoBehaviour
    {
        public HoldableItem crate;
        public Transform door;
        public Transform handle;
        public float leverX = 11.2f, laneZ = 2.2f, padTopY = 0.15f;
        public float acceptDX = 0.95f;
        public float doorOpenHeight = 3.2f;

        public bool IsOpen { get; private set; }
        public float OpenedAtTime { get; private set; } = -1f;

        void Update()
        {
            if (IsOpen || !crate || crate.State != HoldableItem.ItemState.Anchored) return;
            Vector3 p = crate.transform.position;
            if (Mathf.Abs(p.x - leverX) > acceptDX || Mathf.Abs(p.z - laneZ) > 0.8f) return;
            // Snap thung len giua can, mo cua
            float half = crate.GetComponent<Renderer>().bounds.extents.y;
            crate.transform.position = new Vector3(leverX, padTopY + half, laneZ);
            OpenedAtTime = crate.AnchorTime;
            IsOpen = true;
            Debug.Log($"[Level2] Thung neo tren can (t = {crate.AnchorTime:0.00}). Cua mo.");
            if (HUD.Instance) HUD.Instance.ShowSubtitle("Cần cửa đã hạ. Cửa mở.", 3f, false);
            StartCoroutine(OpenDoor());
        }

        IEnumerator OpenDoor()
        {
            if (handle) handle.localRotation = Quaternion.Euler(0f, 0f, -50f);
            var col = door ? door.GetComponent<Collider>() : null;
            if (col) col.enabled = false;
            if (!door) yield break;
            Vector3 a = door.position, b = a + Vector3.up * doorOpenHeight;
            for (float u = 0; u < 1f; u += Time.deltaTime / 0.6f)
            {
                door.position = Vector3.Lerp(a, b, u);
                yield return null;
            }
            door.position = b;
        }
    }
}
