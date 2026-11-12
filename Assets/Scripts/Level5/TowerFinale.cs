using System.Collections;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// MAN 5 - TANG 2 "PHONG LICH" (G12-G30) va LUA CHON KET (v2 muc 5).
    ///  - G28: khe GIA o lan gan (khong nuot thu), khe THAT o lan xa. Nhan E truoc khe that de bo thu.
    ///  - Khe that chi mo khi mat thap dang nhin Anh vong (bong dang di): "Mat thap dang nhin anh" neu khong co bong.
    ///    => loi giai bat buoc di qua bac 3 (v2: "Loi giai bat buoc di qua bac 3").
    ///  - O CHAN BONG G25 lan gan (ke vang + bien): dat thung o day thi bong bi neo chan => KET LANG.
    ///    Khong dat (bong di het quy dao) => KET VONG. Ca hai cach deu qua man an toan vi bong van la moi cua tia do.
    /// LOI GIAI: vao phong voi thung -> di lan gan G16->G27, ne vong do G18 bang lan xa (bac 2, tia khoa chua bat)
    ///   -> (tuy chon) dat thung o o vang G25 -> lui mot mach ve G16 (bac 3: bong xuat hien o G16, tia khoa lan gan)
    ///   -> doi sang lan xa, di toi G28, nhan E bo thu vao khe that.
    /// </summary>
    public class TowerFinale : MonoBehaviour
    {
        public static TowerFinale Instance { get; private set; }
        public NhipBrain brain;
        public KhangController khang;
        public NhipEyeView eye;
        public float slotTime = 28f, slotHalfWidth = 0.9f;
        public float nearZ = 0f, farZ = 2.2f;
        public Renderer realSlotGlow, fakeSlotGlow;
        public Transform realSlotShutter;
        public float choiceTime = 25f;
        public Renderer choiceMark;
        public float outroSeconds = 4.5f;
        public Renderer[] towerRings;

        public bool Delivered { get; private set; }
        public bool EndingVong { get; private set; }
        public bool SlotOpen => GhostPlayer.Instance && GhostPlayer.Instance.Active && !Delivered;
        LevelClock _clock;
        Material _real, _fake, _choice;
        Vector3 _shutter0;
        bool _toldChoice, _toldNear;
        float _sayUntil;

        void Awake() { Instance = this; }
        void OnDestroy() { if (Instance == this) Instance = null; }
        void OnEnable() { KhangInteractor.InteractOverride += OnInteract; }
        void OnDisable() { KhangInteractor.InteractOverride -= OnInteract; }

        void Start()
        {
            _clock = LevelClock.Instance;
            if (!brain) brain = NhipBrain.Instance;
            if (!khang) khang = FindAnyObjectByType<KhangController>();
            if (realSlotGlow) _real = realSlotGlow.material;
            if (fakeSlotGlow) _fake = fakeSlotGlow.material;
            if (choiceMark) _choice = choiceMark.material;
            if (realSlotShutter) _shutter0 = realSlotShutter.localPosition;
        }

        bool AtSlot(float laneZ) => khang && _clock && Mathf.Abs(khang.transform.position.x - _clock.TimeToX(slotTime)) <= slotHalfWidth && Mathf.Abs(khang.transform.position.z - laneZ) < 0.6f;

        bool OnInteract()
        {
            if (Delivered) return true;
            if (AtSlot(nearZ)) { Say("Khe này bị hàn kín. Đây là khe giả — nó không nuốt thư.", 3f, false); return true; }
            if (!AtSlot(farZ)) return false;
            if (!SlotOpen) { Say("Mắt tháp đang nhìn anh. Khe thật chỉ mở khi NHỊP nhìn chỗ khác.", 3.5f, false); if (HUD.Instance) HUD.Instance.ShowHint("Tạo Ảnh vọng (lùi một mạch ở tầng 2 khi NHỊP bậc 2) để kéo mắt tháp đi.", 6f); return true; }
            StartCoroutine(Deliver());
            return true;
        }

        IEnumerator Deliver()
        {
            Delivered = true;
            var g = GhostPlayer.Instance;
            bool blocked = g && g.PathBlockedAhead();
            EndingVong = !blocked;
            Debug.Log($"[Man 5] Thu vao khe that. Bong bi neo chan = {blocked} => {(EndingVong ? "KET VONG" : "KET LANG")}");
            if (brain) brain.tierCap = brain.Tier;
            if (eye) eye.off = true;
            Say("...Một lá thư gửi cho tôi? ‘Giờ đã đủ đúng rồi, NHỊP. Nghỉ đi.’ ...Đã đọc. Ngừng quan sát.", 6f, true);
            GameFlow.Ensure().SetEnding(EndingVong);
            if (g && !blocked) g.playbackBoost = 3f;
            float t0 = Time.time;
            while (Time.time - t0 < outroSeconds)
            {
                float u = (Time.time - t0) / outroSeconds;
                if (towerRings != null)
                    for (int i = 0; i < towerRings.Length; i++)
                        if (towerRings[i]) towerRings[i].enabled = u < 1f - (i + 1f) / (towerRings.Length + 1f);
                yield return null;
            }
            Mailbox.CompleteExternally(gameObject.scene.name, EndingVong ? "KẾT VỌNG" : "KẾT LẶNG");
        }

        void Update()
        {
            if (!_clock || !khang) return;
            float t = _clock.CurrentTime;
            var it = khang.GetComponent<KhangInteractor>();
            if (!_toldChoice && t >= 16f && t < 20f && khang.transform.position.y > 2f)
            {
                _toldChoice = true;
                if (HUD.Instance) HUD.Instance.ShowHint("Ô vàng ở G25: trước khi lùi, đặt thùng lên ô ở làn anh đang đi thì Ảnh vọng sẽ bị chặn tại đó (Kết Lặng). Để trống thì bóng đi hết đường (Kết Vọng).", 9f);
            }
            if (!_toldNear && Mathf.Abs(t - choiceTime) < 0.6f && it && it.Held)
            {
                _toldNear = true;
                Say("Đặt thùng ở đây thì bóng của anh sẽ đứng lại. Tôi sẽ không có gì để học.", 4f, true);
            }
            if (_real)
            {
                Color c = Delivered ? new Color(0.4f, 1f, 0.5f) : SlotOpen ? new Color(1f, 0.85f, 0.4f) : new Color(1f, 0.2f, 0.15f);
                _real.EnableKeyword("_EMISSION"); _real.SetColor("_BaseColor", c);
                _real.SetColor("_EmissionColor", c * (SlotOpen ? 1.5f + Mathf.Abs(Mathf.Sin(Time.time * 3f)) : 1.2f));
            }
            if (_fake) { _fake.EnableKeyword("_EMISSION"); _fake.SetColor("_EmissionColor", new Color(1f, 0.8f, 0.4f) * 0.3f); }
            if (realSlotShutter)
                realSlotShutter.localPosition = Vector3.MoveTowards(realSlotShutter.localPosition, _shutter0 + (SlotOpen || Delivered ? Vector3.up * 0.5f : Vector3.zero), Time.deltaTime);
            if (_choice)
            {
                bool filled = false;
                foreach (var h in HoldableItem.All)
                    if (h && h.State == HoldableItem.ItemState.Anchored && Mathf.Abs(_clock.XToTime(h.transform.position.x) - choiceTime) < 0.6f ) filled = true;
                Color c = filled ? new Color(0.5f, 0.9f, 1f) : new Color(1f, 0.85f, 0.2f);
                _choice.EnableKeyword("_EMISSION"); _choice.SetColor("_BaseColor", c);
                _choice.SetColor("_EmissionColor", c * (1f + 0.8f * Mathf.Abs(Mathf.Sin(Time.time * 2f))));
            }
        }

        void Say(string line, float s, bool nhip)
        {
            if (Time.unscaledTime < _sayUntil && !nhip) return;
            _sayUntil = Time.unscaledTime + 1f;
            if (HUD.Instance) HUD.Instance.ShowSubtitle(line, s, nhip);
        }
    }
}
