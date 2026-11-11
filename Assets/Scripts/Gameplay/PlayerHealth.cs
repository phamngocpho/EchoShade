using System;
using UnityEngine;

namespace EchoShade
{
    /// <summary>
    /// Mau cua Khang (mac dinh 3).
    ///  - Cham cang may / bi tam ton roi trung / roi khoi man = -1 HP.
    ///  - Roi khoi man: -1 HP va ve hom thu (checkpoint) dang kich hoat.
    ///  - Het mau: ve hom thu gan nhat, hoi day mau, NHIP tier -1 (khong duoi tier luc vao man).
    ///  - Ve hom thu = dat lai thoi gian theo vi tri hom thu (vi thoi gian = ham cua X).
    ///    Vat neo da dat van giu nguyen.
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
            if (IsInvulnerable && !respawnToCheckpoint) return false;
            HP = Mathf.Max(0, HP - amount);
            _invUntil = Time.time + invulnerableTime;
            Debug.Log($"[Khang] -{amount} HP ({source}). HP = {HP}");
            OnHPChanged?.Invoke(HP);

            if (HP <= 0)
            {
                Deaths++;
                HP = maxHP;
                if (brain) brain.LowerTierOnRespawn();
                Respawn();
                if (HUD.Instance) HUD.Instance.ShowSubtitle("Khang kiệt sức... quay về hòm thư.", 3f, false);
                OnHPChanged?.Invoke(HP);
            }
            else if (respawnToCheckpoint)
            {
                Respawn();
            }
            return true;
        }

        public void Respawn()
        {
            var m = Mailbox.Active;
            Vector3 p = m ? m.SpawnPoint : _startPos;
            _khang.Teleport(p);
            if (clock) clock.ResetTracking();
            OnRespawned?.Invoke();
        }
    }
}
