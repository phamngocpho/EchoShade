using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace EchoShade
{
    /// <summary>
    /// Doc phim cho Khang. Ho tro ca Input System moi (Keyboard.current) va Input cu.
    /// A/Trai = lui (tua), D/Phai = tien, W/Len = lan xa, S/Xuong = lan gan,
    /// Space = nhay, E = nhat/dat, Esc = tam dung.
    /// </summary>
    public static class KhangInput
    {
        public static float Horizontal
        {
            get
            {
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
        public static bool FarLane => Down(Key.W, Key.UpArrow);
        public static bool NearLane => Down(Key.S, Key.DownArrow);
        public static bool Jump => Down(Key.Space);
        public static bool Interact => Down(Key.E);
        public static bool Pause => Down(Key.Escape);
#else
        public static bool FarLane => Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        public static bool NearLane => Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow);
        public static bool Jump => Input.GetKeyDown(KeyCode.Space);
        public static bool Interact => Input.GetKeyDown(KeyCode.E);
        public static bool Pause => Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
