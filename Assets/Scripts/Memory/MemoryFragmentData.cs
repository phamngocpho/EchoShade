using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// MANH NHO (kich ban v3.1 muc 3.1 + 7): du lieu mot manh - id, man, ten, ghi chu hien khi nhat.
    /// Vi tri nam o vat the MemoryFragment trong scene (nhin thay va sua bang Inspector).
    /// Trang thai DA NHAT luu o MemoryStore (ngoai du lieu tua) nen tua khong hoan tac.
    /// </summary>
    [CreateAssetMenu(menuName = "EchoShade/Memory Fragment", fileName = "MF_")]
    public class MemoryFragmentData : ScriptableObject
    {
        public string id = "L1_A";
        public int levelIndex = 1;
        public string displayName = "Tờ ghi chú cũ";
        [TextArea(2, 5)] public string note;
        [Tooltip("Thoai NHIP rieng khi nhat (tuy chon). De trong = chi hien ghi chu.")]
        [TextArea] public string nhipLine;
    }
}
