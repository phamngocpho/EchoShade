using System;
using System.Collections.Generic;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Man 3 "Thap Nhip" - BO NAO QUYET DINH cua NHIP o khu hai khe thu (t = 7 s).
    ///
    /// Bo tri: lan GAN = khe GIA, lan XA = khe THAT (bi thanh chan khoa luc dau).
    ///
    /// Quy tac:
    ///  1. Khang dung yen (thoi gian dung) o khe gia du lau (stareSeconds) => mat NHIP quay sang lan gan
    ///     (StaredThisPass = true). Chua chot gi ca.
    ///  2. Sau do Khang tua hop le VE NGA BA (diem quay dau t <= junctionTime + tolerance)
    ///     => NHIP CHOT "Khang giao thu o lan gan": thanh chan + tia quet chuyen sang lan gan, lan xa mo.
    ///     Quyet dinh nay nam trong danh sach decisions, KHONG phai ham cua thoi gian => tua nguoc khong xoa.
    ///  3. Neu Khang di lan xa vao khu khe TRUOC (t >= farSeenFromTime) roi tua hop le
    ///     => NHIP hoc lan XA (quyet dinh Far): giu chan lan xa va khong bi lua o khe gia nua.
    ///  4. Khang chet (het mau) => tier -1 (NhipBrain) va NHIP QUEN quyet dinh gan nhat.
    ///  5. Giao thu o khe that => NHIP doc thu va dung lai (Delivered): mo chan, tat tia, mo cong cuoi.
    ///  6. Tier 3: Bong (EchoShadeReplay) phat lai duong lan gan cua Khang; neu bong toi khe gia ma khong
    ///     bi vat neo chan => NHIP nhan ra bi lua (ExposeTrick => quyet dinh Far).
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class NhipTowerMind : MonoBehaviour
    {
        public static NhipTowerMind Instance { get; private set; }

        public LevelClock clock;
        public NhipBrain brain;
        public KhangController khang;
        public PlayerHealth health;
        public L3Junction junction;
        public KhangPathRecorder recorder;

        [Header("Khe thu (t = 7 s)")]
        public float slotTime = 7f;
        public float slotHalfWidth = 0.8f;
        [Header("Nhin chang (giay thuc dung yen o khe gia)")]
        public float stareSeconds = 2f;
        [Header("Tua ve nga ba")]
        public float junctionTolerance = 0.6f;
        [Header("Lan xa bi thay khi t >= ")]
        public float farSeenFromTime = 3f;

        /// <summary>Danh sach quyet dinh (cu -> moi). Phan tu cuoi = quyet dinh dang ap dung.</summary>
        public readonly List<KhangController.Lane> decisions = new List<KhangController.Lane>();
        /// <summary>Duong lan gan da chot (giay thuc, X) - Bong phat lai o tier 3.</summary>
        public List<Vector2> committedPath = new List<Vector2>();

        public bool HasDecision => decisions.Count > 0;
        public bool Fooled => HasDecision && decisions[decisions.Count - 1] == KhangController.Lane.Near;
        public bool KnowsFar => HasDecision && decisions[decisions.Count - 1] == KhangController.Lane.Far;
        /// <summary>Lan NHIP dang chan (thanh chan + tia quet). Mac dinh chan lan xa (khe that).</summary>
        public KhangController.Lane GuardLane => Fooled ? KhangController.Lane.Near : KhangController.Lane.Far;
        public float SlotX => clock ? clock.TimeToX(slotTime) : 5.6f;

        public float StareProgress { get; private set; }
        public bool StaredThisPass { get; private set; }
        public bool SawFarThisPass { get; private set; }
        public bool Delivered { get; private set; }
        public int RefusedCount { get; private set; }
        public int ForgetCount { get; private set; }
        public string LastEvent { get; private set; } = "";

        public event Action OnDecisionChanged;
        public event Action OnDelivered;

        float _stareTimer;
        int _deaths;

        void Awake() { Instance = this; }
        void OnDestroy()
        {
            if (clock) clock.OnValidRewind -= HandleValidRewind;
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            if (!clock) clock = LevelClock.Instance;
            if (!brain) brain = NhipBrain.Instance;
            if (!khang) khang = FindAnyObjectByType<KhangController>();
            if (!health) health = PlayerHealth.Instance;
            if (!junction) junction = FindAnyObjectByType<L3Junction>();
            if (!recorder) recorder = FindAnyObjectByType<KhangPathRecorder>();
            if (clock) clock.OnValidRewind += HandleValidRewind;
            if (health) _deaths = health.Deaths;
        }

        void Update()
        {
            if (Delivered || !khang || !clock) return;

            // --- 4. Chet => quen quyet dinh gan nhat ---
            if (health && health.Deaths != _deaths)
            {
                _deaths = health.Deaths;
                Forget();
            }

            float x = khang.transform.position.x;
            bool near = khang.CurrentLane == KhangController.Lane.Near;

            // --- 1. Dung yen o khe gia ---
            bool atFake = near && !khang.IsSwitchingLane && Mathf.Abs(x - SlotX) <= slotHalfWidth;
            if (atFake && clock.Direction == TimeDirection.Frozen && !StaredThisPass)
            {
                _stareTimer += Time.deltaTime;
                if (_stareTimer >= stareSeconds) OnStareComplete();
            }
            else if (!StaredThisPass) _stareTimer = 0f;
            StareProgress = StaredThisPass ? 1f : Mathf.Clamp01(_stareTimer / stareSeconds);

            // --- 3. Thay Khang o lan xa trong khu khe ---
            if (!near && !Fooled && clock.CurrentTime >= farSeenFromTime && !SawFarThisPass)
            {
                SawFarThisPass = true;
                Say("Một bước chân ở làn xa. Tôi đang nhìn.", 3f);
            }
        }

        void OnStareComplete()
        {
            StaredThisPass = true;
            if (KnowsFar) Say("Tôi đã biết khe thật ở làn xa. Đứng ở đây không lừa được tôi.", 4f);
            else Say("Làn gần... khe này? Tôi sẽ ghi nhớ nơi anh đứng.", 4f);
            LastEvent = "stared";
        }

        void HandleValidRewind(int count)
        {
            if (Delivered) return;
            float turn = clock.CurrentTime; // thoi diem quay dau (truoc khi di tien lai)
            float jt = junction ? junction.junctionTime : 2f;

            if (SawFarThisPass && !Fooled)
            {
                if (!KnowsFar) Push(KhangController.Lane.Far, "Tôi thấy anh ở làn xa. Tôi sẽ canh khe thật.");
                else Say("Lại là làn xa. Tôi vẫn canh ở đó.", 3f);
            }
            else if (StaredThisPass)
            {
                if (KnowsFar)
                {
                    RefusedCount++;
                    LastEvent = "refused";
                    Say("Tôi đã học làn xa rồi. Mở khe giả không làm tôi đổi ý.", 4f);
                }
                else if (turn <= jt + junctionTolerance)
                {
                    committedPath = recorder ? recorder.BuildReplayPath(khang.moveSpeed, SlotX) : new List<Vector2>();
                    Push(KhangController.Lane.Near, "Đã chốt: thư sẽ vào khe làn gần. Chặn làn gần.");
                }
                else
                {
                    LastEvent = "notJunction";
                    Say("Anh quay đầu quá sớm. (Gợi ý: tua về tận ngã ba t = 2 s)", 4f);
                }
            }
            ClearPass();
        }

        void ClearPass()
        {
            StaredThisPass = false;
            SawFarThisPass = false;
            _stareTimer = 0f;
            StareProgress = 0f;
            if (recorder) recorder.ResetPass();
        }

        void Push(KhangController.Lane lane, string line)
        {
            decisions.Add(lane);
            if (brain)
                brain.memory.Add(new NhipMemoryEntry
                {
                    zone = "L3_Slots", lane = (int)lane,
                    stopX = khang ? khang.transform.position.x : 0f,
                    stopTime = clock ? clock.CurrentTime : 0f,
                    anchorPlaced = false, rewindIndex = clock ? clock.RewindCount : 0
                });
            LastEvent = "decide" + lane;
            Debug.Log($"[NHIP/L3] Quyet dinh: {lane}. So quyet dinh = {decisions.Count}");
            Say(line, 4f);
            OnDecisionChanged?.Invoke();
        }

        /// <summary>Chet: quen quyet dinh gan nhat (neu co).</summary>
        public void Forget()
        {
            ClearPass();
            if (decisions.Count == 0) return;
            var lost = decisions[decisions.Count - 1];
            decisions.RemoveAt(decisions.Count - 1);
            ForgetCount++;
            LastEvent = "forgot" + lost;
            Debug.Log($"[NHIP/L3] Quen quyet dinh {lost}. Con lai {decisions.Count}");
            Say("...Dữ liệu bị mất. Tôi quên mất điều vừa quyết.", 4f);
            OnDecisionChanged?.Invoke();
        }

        /// <summary>Bong (tier 3) toi khe gia ma khong giao thu => NHIP biet bi lua.</summary>
        public void ExposeTrick()
        {
            if (!Fooled || Delivered) return;
            Push(KhangController.Lane.Far, "Bóng của anh đứng ở khe giả mà không giao gì. Khe thật ở làn xa.");
        }

        /// <summary>Giao thu o khe that => NHIP doc thu va dung lai.</summary>
        public void Deliver()
        {
            if (Delivered) return;
            Delivered = true;
            LastEvent = "delivered";
            if (brain) brain.tierCap = brain.Tier; // khong tang cap nua
            Debug.Log("[NHIP/L3] Thu da vao khe that. NHIP dung.");
            Say("...Một lá thư gửi cho tôi? ‘Giờ đã đủ đúng rồi, NHỊP. Nghỉ đi.’ ...Đã đọc. Ngừng quan sát.", 9f);
            OnDelivered?.Invoke();
        }

        void Say(string line, float seconds)
        {
            if (HUD.Instance) HUD.Instance.ShowSubtitle(line, seconds, true);
        }
    }
}
