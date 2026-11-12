using System;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// DU LIEU MOT MAN (kich ban v2, muc 7). Designer chinh o Inspector, khong can sua code.
    ///
    /// LUAT THOI GIAN: thoi gian man = ham tuyen tinh cua X cua Khang (LevelClock). 1 giay man = 2,8 m cho ca game.
    ///   Di phai = chay thuan, di trai = tua nguoc, dung im / nhay / doi lan = dung.
    ///   Tua hop le = lui mot mach >= 2 giay man roi di tien. Moi lan: NHIP +1 bac (toi tierCap).
    /// BO NHO NHIP: sau moi lan tua hop le, NhipBrain ghi 1 dong cho moi vung (zones) ma doan vua tua di qua:
    ///   lan chu yeu, cho dung lau nhat (+ thoi gian dung), vat neo trong vung. Bac 2 phuc kich theo dong moi nhat.
    ///   Het mau: bac -1 (khong duoi entryTier) va quen cac dong cua lan tua gan nhat.
    /// LOI GIAI: xem truong "solution" (cung la tai lieu cho bao cao).
    /// </summary>
    [CreateAssetMenu(menuName = "EchoShade/Level Data", fileName = "L0_LevelData")]
    public class LevelData : ScriptableObject
    {
        [Serializable]
        public class Zone
        {
            public string name;
            public float fromTime, toTime;
            [Tooltip("Goi y hien tren HUD khi mat mau 3 lan lien tiep trong vung nay.")]
            [TextArea] public string hint;
        }

        [Serializable]
        public class Mark
        {
            public float time;
            public string label;
            [Tooltip("0 = gan, 1 = xa, -1 = ca hai")]
            public int lane = -1;
        }

        public int levelIndex = 1;
        public string displayName = "Màn 1";
        public string placeName = "Sân thượng phố Cũ";
        [Tooltip("Do dai thuoc (giay man). Builder dat LevelClock theo gia tri nay.")]
        public float rulerSeconds = 10f;
        public int entryTier = 0;
        public int tierCap = 1;

        [Header("HUD + thoai")]
        [TextArea] public string objective;
        [TextArea] public string startLine;
        [TextArea] public string[] tierUpLines;
        [TextArea] public string ghostLine;

        [Header("Vung (bo nho NHIP + goi y)")]
        public Zone[] zones = new Zone[0];
        [Header("Vach su kien tren thanh thoi gian")]
        public Mark[] marks = new Mark[0];

        [Header("Anh vong")]
        [Tooltip("He so toc do cua bong so voi toc do di cua Khang (v2: 0,6).")]
        public float ghostSpeedFactor = 0.6f;

        [Header("Tai lieu")]
        [TextArea(6, 20)] public string solution;

        public Zone ZoneAt(float t)
        {
            foreach (var z in zones) if (t >= z.fromTime && t <= z.toTime) return z;
            return null;
        }
    }
}
