using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EchoShade
{
    /// <summary>
    /// Doc phim cho Khang. Ho tro ca Input System moi (Keyboard.current) va Input cu.
    /// A/Trai = lui (tua), D/Phai = tien, W/Len = lan xa, S/Xuong = lan gan,
    /// Space = nhay (sat vat thap = nhay qua), E = nhat/dat, R = ve thung thu, Esc = tam dung (M = ve menu khi tam dung).
    /// </summary>
    public static class KhangInput
    {
        // ---- Dieu khien gia lap (chi dung cho kiem thu tu dong: SimDriver). Khong anh huong khi choi binh thuong. ----
        public static bool SimActive;
        public static float SimHorizontal;
        static int _fFar = -1, _fNear = -1, _fJump = -1, _fInteract = -1, _fRestart = -1;
        public static void SimPress(string key)
        {
            int f = Time.frameCount + 1; // coroutine chay sau Update => hieu luc o khung ke tiep
            switch (key) { case "far": _fFar = f; break; case "near": _fNear = f; break; case "jump": _fJump = f; break; case "interact": _fInteract = f; break; case "restart": _fRestart = f; break; }
        }
        static bool Sim(int f) => SimActive && f == Time.frameCount;

        public static float Horizontal
        {
            get
            {
                if (SimActive) return SimHorizontal;
                float h = 0f;
#if ENABLE_INPUT_SYSTEM
                var k = Keyboard.current;
                if (k != null)
                {
                    if (k.aKey.isPressed || k.leftArrowKey.isPressed) h -= 1f;
                    if (k.dKey.isPressed || k.rightArrowKey.isPressed) h += 1f;
                }
#elif ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) h -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
#endif
                return h;
            }
        }

#if ENABLE_INPUT_SYSTEM
        static bool Down(Key a, Key b = Key.None)
        {
            var k = Keyboard.current;
            if (k == null) return false;
            return k[a].wasPressedThisFrame || (b != Key.None && k[b].wasPressedThisFrame);
        }
        public static bool FarLane => Sim(_fFar) || Down(Key.W, Key.UpArrow);
        public static bool NearLane => Sim(_fNear) || Down(Key.S, Key.DownArrow);
        public static bool Jump => Sim(_fJump) || Down(Key.Space);
        public static bool Interact => Sim(_fInteract) || Down(Key.E);
        public static bool Pause => Down(Key.Escape);
        public static bool Restart => Sim(_fRestart) || Down(Key.R);
        public static bool Menu => Down(Key.M);
#else
        public static bool FarLane => Sim(_fFar) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        public static bool NearLane => Sim(_fNear) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        public static bool Jump => Sim(_fJump) || Input.GetKeyDown(KeyCode.Space);
        public static bool Interact => Sim(_fInteract) || Input.GetKeyDown(KeyCode.E);
        public static bool Pause => Input.GetKeyDown(KeyCode.Escape);
        public static bool Restart => Sim(_fRestart) || Input.GetKeyDown(KeyCode.R);
        public static bool Menu => Input.GetKeyDown(KeyCode.M);
#endif
    }
}
