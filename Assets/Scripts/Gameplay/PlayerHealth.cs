using System;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Mau cua Khang (mac dinh 3) - v2 muc 3.3.
    ///  - Mat 1 mau khi: bi tau can, vat roi, roi khoi man, tia do, cang / vat tha cua NHIP. Anh vong KHONG gay mat mau.
    ///  - Roi khoi man: -1 mau va ve thung thu dang kich hoat.
    ///  - Het mau (hoac phim R - tu ve thung thu): ve thung thu gan nhat, day mau, NHIP bac -1
    ///    (khong duoi bac vao man) va QUEN quyet dinh vua hoc gan nhat. Anh vong bien mat.
    ///  - Ve thung thu = dat lai thoi gian theo vi tri thung thu. Vat neo da dat van giu nguyen.
    /// </summary>
    [RequireComponent(typeof(KhangController))]
    public class PlayerHealth : MonoBehaviour
    {
        public static PlayerHealth Instance { get; private set; }

        public int maxHP = 3;
        public float invulnerableTime = 1f;
        public LevelClock clock;
        public NhipBrain brain;

        public int HP { get; private set; }
        public int Deaths { get; private set; }
        public event Action<int> OnHPChanged;
        public event Action OnRespawned;
        /// <summary>(nguon, thoi gian man luc trung don)</summary>
        public event Action<string, float> OnDamaged;

        KhangController _khang;
        CharacterController _cc;
        Vector3 _startPos;
        float _invUntil = -1f;

        public Bounds Bounds => _cc ? _cc.bounds : new Bounds(transform.position, Vector3.one);
        public bool IsInvulnerable => Time.time < _invUntil;

        void Awake()
        {
            Instance = this;
            _khang = GetComponent<KhangController>();
            _cc = GetComponent<CharacterController>();
            _startPos = transform.position;
            HP = maxHP;
        }

        void Start()
        {
            if (!clock) clock = LevelClock.Instance;
            if (!brain) brain = NhipBrain.Instance;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Tru mau. respawnToCheckpoint = true khi roi khoi man (luon ap dung, bo qua bat tu).</summary>
        public bool Damage(int amount, string source, bool respawnToCheckpoint = false)
        {
            if (Mailbox.LevelCompleted) return false;
            if (IsInvulnerable && !respawnToCheckpoint) return false;
            HP = Mathf.Max(0, HP - amount);
            _invUntil = Time.time + invulnerableTime;
            Debug.Log($"[Khang] -{amount} HP ({source}). HP = {HP}");
            OnHPChanged?.Invoke(HP);
            OnDamaged?.Invoke(source, clock ? clock.CurrentTime : 0f);

            if (HP <= 0) ReturnToMailbox(false);
            else if (respawnToCheckpoint) Respawn();
            return true;
        }

        /// <summary>Het mau (voluntary=false) hoac phim R (voluntary=true): ve thung thu voi cung cai gia.</summary>
        public void ReturnToMailbox(bool voluntary)
        {
            Deaths++;
            HP = maxHP;
            if (brain) { brain.LowerTierOnRespawn(); brain.ForgetLatest(); }
            Respawn();
            if (HUD.Instance)
                HUD.Instance.ShowSubtitle(voluntary ? "Khang quay về hòm thư. NHỊP quên điều vừa học." : "Khang kiệt sức... quay về hòm thư. NHỊP quên điều vừa học.", 3f, false);
            OnHPChanged?.Invoke(HP);
        }

        public void Respawn()
        {
            var m = Mailbox.Active;
            Vector3 p = m ? m.SpawnPoint : _startPos;
            _khang.Teleport(p);
            if (clock) clock.ResetTracking();
            if (KhangTrail.Instance) KhangTrail.Instance.Clear();
            _invUntil = Time.time + invulnerableTime;
            OnRespawned?.Invoke();
        }
    }
}
