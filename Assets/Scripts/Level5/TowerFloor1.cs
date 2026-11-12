using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// MAN 5 - TANG 1 "HANH LANG CAM KET" (G0-G12). (Chuyen tu nga ba + khe gia cua Man 3 cu.)
    ///
    /// Bo tri: nga ba G2 (chi o day moi doi lan duoc, hai lan bi bo tuong thap ngan cach toi G9,6).
    ///   Lan gan G9 = CUA GIA. Lan xa G9 = CUA LEN TANG THAT, bi THANH CHAN khoa luc moi vao.
    /// Luat (v2):
    ///  1. Dung im o cua gia du lau (stareSeconds giay thuc) => mat thap quay sang lan gan. Chua chot gi.
    ///  2. Roi lui mot mach VE NGA BA (lan tua hop le, bac +1) => NHIP CAM KET lan gan: thanh chan roi cua that
    ///     sang lan gan, cua that mo. Quyet dinh la danh sach, KHONG la ham cua thoi gian => tua khong xoa.
    ///  3. Neu di lan xa vao hanh lang truoc (t >= farSeenFromTime) roi tua => NHIP hoc LAN XA: thanh chan giu cua that,
    ///     dung o cua gia khong lua duoc nua. Man khong khoa cung: het mau / phim R => bac -1, quen quyet dinh moi nhat.
    /// LOI GIAI: nhat thung o G2 -> lan gan toi cua gia G9, dung im -> lui mot mach ve G2 (bac 2)
    ///   -> dung im o nga ba, sang lan xa (W) -> di lan xa qua cua that G9, len cau thang toi tang 2.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class TowerFloor1 : MonoBehaviour
    {
        public static TowerFloor1 Instance { get; private set; }

        public LevelClock clock;
        public NhipBrain brain;
        public KhangController khang;
        public PlayerHealth health;

        public float junctionTime = 2f, junctionHalfWidth = 0.8f;
        public float doorTime = 9f, doorHalfWidth = 0.9f;
        public float gateUntilTime = 9.7f;
        public float stareSeconds = 2f;
        public float junctionTolerance = 0.7f;
        public float farSeenFromTime = 3f;
        [Tooltip("Lan tua bat dau sau thoi diem nay thuoc tang 2 => tang 1 bo qua.")]
        public float floorEndTime = 11f;
        [Tooltip("Tren cau thang (G9,4-12,1) khong doi lan duoc.")]
        public float stairsFrom = 9.4f, stairsTo = 12.1f;

        [Header("Vat the")]
        public Transform barrier;
        public Transform realDoorPanel;
        public float nearZ = 0f, farZ = 2.2f;
        public float barrierSpeed = 5f, doorOpenHeight = 3f;

        public readonly List<KhangController.Lane> decisions = new List<KhangController.Lane>();
        public bool Fooled => decisions.Count > 0 && decisions[decisions.Count - 1] == KhangController.Lane.Near;
        public bool KnowsFar => decisions.Count > 0 && decisions[decisions.Count - 1] == KhangController.Lane.Far;
        public KhangController.Lane GuardLane => Fooled ? KhangController.Lane.Near : KhangController.Lane.Far;
        public float StareProgress { get; private set; }
        public bool StaredThisPass { get; private set; }
        public bool SawFarThisPass { get; private set; }
        public string LastEvent { get; private set; } = "";

        float _stare, _hintUntil;
        Vector3 _barrierBase, _doorBase;

        float JX => clock.TimeToX(junctionTime);
        float DoorX => clock.TimeToX(doorTime);

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; if (clock) clock.OnValidRewind -= OnRewind; if (brain) brain.OnForget -= OnForget; if (khang) khang.laneSwitchGate = null; }

        void Start()
        {
            if (!clock) clock = LevelClock.Instance;
            if (!brain) brain = NhipBrain.Instance;
            if (!khang) khang = FindAnyObjectByType<KhangController>();
            if (!health) health = PlayerHealth.Instance;
            clock.OnValidRewind += OnRewind;
            brain.OnForget += OnForget;
            khang.laneSwitchGate = CanSwitch;
            if (barrier) _barrierBase = barrier.position;
            if (realDoorPanel) _doorBase = realDoorPanel.position;
            if (barrier) barrier.position = new Vector3(_barrierBase.x, _barrierBase.y, GuardZ);
        }

        float GuardZ => GuardLane == KhangController.Lane.Near ? nearZ : farZ;

        bool CanSwitch(KhangController.Lane target)
        {
            float tt = clock.CurrentTime;
            if (tt >= stairsFrom && tt <= stairsTo) return false;
            if (tt > gateUntilTime) return true;
            if (Mathf.Abs(khang.transform.position.x - JX) <= junctionHalfWidth) return true;
            if (HUD.Instance && Time.unscaledTime > _hintUntil)
            {
                HUD.Instance.ShowSubtitle("Ở hành lang này chỉ đổi làn được tại ngã ba (G2).", 2.5f, false);
                _hintUntil = Time.unscaledTime + 3f;
            }
            return false;
        }

        void Update()
        {
            float t = clock.CurrentTime;
            bool near = khang.CurrentLane == KhangController.Lane.Near;
            bool atFake = near && !khang.IsSwitchingLane && Mathf.Abs(khang.transform.position.x - DoorX) <= doorHalfWidth;
            if (atFake && clock.Direction == TimeDirection.Frozen && !StaredThisPass)
            {
                _stare += Time.deltaTime;
                if (_stare >= stareSeconds)
                {
                    StaredThisPass = true; LastEvent = "stared";
                    Say(KnowsFar ? "Tôi đã biết cửa thật ở làn xa. Đứng ở đây không lừa được tôi." : "Làn gần... cửa này? Tôi sẽ ghi nơi anh đứng.", 4f);
                }
            }
            else if (!StaredThisPass) _stare = 0f;
            StareProgress = StaredThisPass ? 1f : Mathf.Clamp01(_stare / stareSeconds);

            if (!near && t >= farSeenFromTime && t < gateUntilTime && !Fooled && !SawFarThisPass)
            {
                SawFarThisPass = true;
                Say("Một bước chân ở làn xa. Tôi đang nhìn.", 3f);
            }

            if (barrier)
            {
                var want = new Vector3(_barrierBase.x, _barrierBase.y, GuardZ);
                barrier.position = Vector3.MoveTowards(barrier.position, want, barrierSpeed * Time.deltaTime);
            }
            if (realDoorPanel)
            {
                bool open = GuardLane == KhangController.Lane.Near;
                realDoorPanel.position = Vector3.MoveTowards(realDoorPanel.position, _doorBase + (open ? Vector3.up * doorOpenHeight : Vector3.zero), 4f * Time.deltaTime);
            }
        }

        void OnRewind(int count)
        {
            if (clock.LastRewindFrom > floorEndTime) return; // lan tua o tang 2
            float turn = clock.LastRewindTo;
            if (SawFarThisPass && !Fooled)
            {
                if (!KnowsFar) Push(KhangController.Lane.Far, "Tôi thấy anh ở làn xa. Tôi sẽ canh cửa thật.");
                else Say("Lại là làn xa. Tôi vẫn canh ở đó.", 3f);
            }
            else if (StaredThisPass)
            {
                if (KnowsFar) { LastEvent = "refused"; Say("Tôi đã học làn xa rồi. Đứng ở cửa giả không làm tôi đổi ý.", 4f); }
                else if (turn <= junctionTime + junctionTolerance) Push(KhangController.Lane.Near, "Đã chốt: anh đi cửa làn gần. Tôi chặn làn gần.");
                else { LastEvent = "notJunction"; Say("Anh quay đầu quá sớm.", 3f); if (HUD.Instance) HUD.Instance.ShowHint("Lùi một mạch về tận ngã ba (G2) rồi mới đi tiếp.", 5f); }
            }
            ClearPass();
        }

        void ClearPass() { StaredThisPass = false; SawFarThisPass = false; _stare = 0f; StareProgress = 0f; }

        void Push(KhangController.Lane lane, string line)
        {
            decisions.Add(lane);
            brain.memory.Add(new NhipMemoryEntry { zone = "L5_Cua", lane = (int)lane, stopX = khang.transform.position.x, stopTime = clock.CurrentTime, rewindIndex = clock.RewindCount });
            LastEvent = "decide" + lane;
            Debug.Log($"[NHIP/L5] Cam ket lan {lane}. So quyet dinh = {decisions.Count}");
            Say(line, 4f);
        }

        void OnForget()
        {
            ClearPass();
            var mb = Mailbox.Active;
            bool onFloor1 = !mb || clock.XToTime(mb.transform.position.x) < floorEndTime;
            if (!onFloor1 || decisions.Count == 0) return;
            var lost = decisions[decisions.Count - 1];
            decisions.RemoveAt(decisions.Count - 1);
            LastEvent = "forgot" + lost;
            Debug.Log($"[NHIP/L5] Quen quyet dinh {lost}");
            Say("...Dữ liệu bị mất. Tôi quên mất điều vừa quyết.", 4f);
        }

        void Say(string line, float s) { if (HUD.Instance) HUD.Instance.ShowSubtitle(line, s, true); }
    }
}
