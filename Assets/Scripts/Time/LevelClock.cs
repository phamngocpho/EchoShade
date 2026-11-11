using System;
using UnityEngine;

namespace EchoShade
{
    /// <summary>Huong chay cua thoi gian trong man / Direction of level time.</summary>
    public enum TimeDirection { Frozen, Forward, Backward }

    /// <summary>
    /// THUOC THOI GIAN CUA MAN (level time ruler).
    ///
    /// Quy tac thoi gian / Time rule:
    ///  - Thoi gian KHONG chay theo dong ho thuc. No chi phu thuoc vi tri X cua Khang.
    ///    Level time is NOT real time: it is a pure function of Khang's X position.
    ///  - X tu levelStartX -> levelEndX duoc anh xa tuyen tinh sang 0 -> duration (giay).
    ///  - Di sang phai  = thoi gian chay thuan (Forward).
    ///  - Di sang trai  = thoi gian tua nguoc (Backward).
    ///  - Dung yen / nhay / doi lan = X khong doi => thoi gian dung (Frozen).
    ///
    /// Tua hop le / Valid rewind:
    ///  - Lui lien tuc it nhat minValidRewind giay (mac dinh 2 s) roi di tien tro lai.
    ///  - Moi lan hop le: RewindCount++ va ban su kien OnValidRewind (sau nay dung de nang cap bot NHIP).
    /// </summary>
    [DefaultExecutionOrder(-100)] // cap nhat truoc moi TimelineObject
    public class LevelClock : MonoBehaviour
    {
        public static LevelClock Instance { get; private set; }

        [Header("Binding")]
        [Tooltip("Khang. Thoi gian doc tu transform.position.x cua doi tuong nay.")]
        public Transform player;

        [Header("Ruler (X -> seconds)")]
        public float levelStartX = -14f;
        public float levelEndX = 14f;
        [Min(0.01f)] public float duration = 10f;

        [Header("Rewind detection")]
        [Tooltip("So giay phai lui lien tuc de tinh la 1 lan tua hop le.")]
        public float minValidRewind = 2f;
        [Tooltip("Neu bat: dung yen giua chung se lam mat chuoi lui. Mac dinh tat: dung yen chi tam dung chuoi.")]
        public bool freezeBreaksRewind = false;
        [Tooltip("Thay doi thoi gian nho hon nguong nay (giay) duoc xem la dung yen.")]
        public float epsilon = 0.0005f;

        /// <summary>Thoi gian hien tai cua man (giay), 0..duration.</summary>
        public float CurrentTime { get; private set; }
        /// <summary>0..1</summary>
        public float Normalized => duration > 0f ? CurrentTime / duration : 0f;
        public TimeDirection Direction { get; private set; } = TimeDirection.Frozen;
        public int RewindCount { get; private set; }
        /// <summary>Thoi gian lon nhat Khang da dat toi tu lan reset gan nhat (vd: da qua moc t = 6 chua).</summary>
        public float MaxTimeReached { get; private set; }
        /// <summary>So giay da lui trong chuoi hien tai (de UI hien thi).</summary>
        public float CurrentRewindAmount => _rewinding ? Mathf.Max(0f, _rewindStartTime - CurrentTime) : 0f;
        public bool CurrentRewindQualified => _rewinding && CurrentRewindAmount >= minValidRewind;

        /// <summary>Ban ra khi co 1 lan tua hop le. Tham so = RewindCount moi.</summary>
        public event Action<int> OnValidRewind;
        /// <summary>Ban ra moi khi thoi gian thay doi. Tham so = CurrentTime.</summary>
        public event Action<float> OnTimeChanged;

        bool _rewinding;
        float _rewindStartTime;

        void Awake()
        {
            Instance = this;
            CurrentTime = XToTime(player ? player.position.x : levelStartX);
            MaxTimeReached = CurrentTime;
        }

        /// <summary>
        /// Goi sau khi dich chuyen Khang (hoi sinh o hom thu / checkpoint).
        /// Dat lai thoi gian theo X moi, KHONG tinh la tua nguoc, xoa chuoi lui dang do.
        /// </summary>
        public void ResetTracking()
        {
            _rewinding = false;
            CurrentTime = XToTime(player ? player.position.x : levelStartX);
            MaxTimeReached = CurrentTime;
            Direction = TimeDirection.Frozen;
            OnTimeChanged?.Invoke(CurrentTime);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public float XToTime(float x)
        {
            float u = Mathf.InverseLerp(levelStartX, levelEndX, x);
            return u * duration;
        }

        public float TimeToX(float t)
        {
            return Mathf.Lerp(levelStartX, levelEndX, duration > 0f ? t / duration : 0f);
        }

        void LateUpdate()
        {
            if (!player) return;
            Tick(XToTime(player.position.x));
        }

        void Tick(float newTime)
        {
            float prev = CurrentTime;
            float delta = newTime - prev;

            TimeDirection dir;
            if (delta > epsilon) dir = TimeDirection.Forward;
            else if (delta < -epsilon) dir = TimeDirection.Backward;
            else dir = TimeDirection.Frozen;

            switch (dir)
            {
                case TimeDirection.Backward:
                    if (!_rewinding)
                    {
                        _rewinding = true;
                        _rewindStartTime = prev; // bat dau chuoi lui tu thoi diem truoc khi lui
                    }
                    break;

                case TimeDirection.Forward:
                    if (_rewinding)
                    {
                        // Ket thuc chuoi lui bang viec di tien => kiem tra hop le
                        float amount = _rewindStartTime - prev;
                        _rewinding = false;
                        if (amount >= minValidRewind)
                        {
                            RewindCount++;
                            OnValidRewind?.Invoke(RewindCount);
                        }
                    }
                    break;

                case TimeDirection.Frozen:
                    if (_rewinding && freezeBreaksRewind) _rewinding = false;
                    break;
            }

            Direction = dir;
            if (dir != TimeDirection.Frozen)
            {
                CurrentTime = newTime;
                if (CurrentTime > MaxTimeReached) MaxTimeReached = CurrentTime;
                OnTimeChanged?.Invoke(CurrentTime);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            // Ve thuoc thoi gian trong Scene view: moi giay 1 vach
            Gizmos.color = Color.cyan;
            Vector3 a = new Vector3(levelStartX, 0.05f, transform.position.z);
            Vector3 b = new Vector3(levelEndX, 0.05f, transform.position.z);
            Gizmos.DrawLine(a, b);
            int ticks = Mathf.FloorToInt(duration);
            for (int i = 0; i <= ticks; i++)
            {
                float x = TimeToX(i);
                Gizmos.DrawLine(new Vector3(x, 0.05f, transform.position.z), new Vector3(x, 0.35f, transform.position.z));
            }
        }
#endif
    }
}
