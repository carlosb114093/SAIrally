using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SanAndres
{
    /// <summary>
    /// Controles del juego. Funciona con el Input System nuevo (predeterminado en Unity 6)
    /// y con el Input Manager clásico, según lo que tenga activo el proyecto.
    /// Teclado: WASD / flechas, Espacio = freno de mano, Shift = turbo, R = reaparecer,
    /// T / Enter = reiniciar carrera, C = cámara, H = ayuda, M = silenciar motor, Esc = pausa.
    /// Control: gatillos = acelerar / frenar, stick izq. = girar, A = freno de mano,
    /// RB = turbo, Y = reaparecer, Start = pausa, Select = cámara.
    /// En los menús: flechas / WASD / cruceta / stick para moverse, Enter / Espacio / A para
    /// aceptar y Esc / Retroceso / B para volver.
    /// </summary>
    public static class GameInput
    {
        public static float Throttle()
        {
            float v = 0f;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.wKey.isPressed || k.upArrowKey.isPressed) v += 1f;
                if (k.sKey.isPressed || k.downArrowKey.isPressed) v -= 1f;
            }
            var g = Gamepad.current;
            if (g != null) v += g.rightTrigger.ReadValue() - g.leftTrigger.ReadValue();
#else
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v -= 1f;
#endif
            return Mathf.Clamp(v, -1f, 1f);
        }

        public static float Steer()
        {
            float v = 0f;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.dKey.isPressed || k.rightArrowKey.isPressed) v += 1f;
                if (k.aKey.isPressed || k.leftArrowKey.isPressed) v -= 1f;
            }
            var g = Gamepad.current;
            if (g != null) v += g.leftStick.ReadValue().x;
#else
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v -= 1f;
#endif
            return Mathf.Clamp(v, -1f, 1f);
        }

        public static bool Handbrake()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.spaceKey.isPressed) || (g != null && g.buttonSouth.isPressed);
#else
            return Input.GetKey(KeyCode.Space);
#endif
        }

        public static bool Boost()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && (k.leftShiftKey.isPressed || k.rightShiftKey.isPressed)) ||
                   (g != null && g.rightShoulder.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        public static bool RespawnPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.rKey.wasPressedThisFrame) || (g != null && g.buttonNorth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        public static bool RestartPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return (k != null && (k.tKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame));
#else
            return Input.GetKeyDown(KeyCode.T) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }

        public static bool CameraPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && k.cKey.wasPressedThisFrame) || (g != null && g.selectButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.C);
#endif
        }

        public static bool HelpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && (k.hKey.wasPressedThisFrame || k.f1Key.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.H) || Input.GetKeyDown(KeyCode.F1);
#endif
        }

        public static bool MutePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k.mKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.M);
#endif
        }
    

        // ------------------------------------------------------------ menús

        public static bool PausePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && (k.escapeKey.wasPressedThisFrame || k.pKey.wasPressedThisFrame)) ||
                   (g != null && g.startButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
        }

        public static bool MenuConfirm()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame)) ||
                   (g != null && g.buttonSouth.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool MenuBack()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current; var g = Gamepad.current;
            return (k != null && (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame)) ||
                   (g != null && (g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame));
#else
            return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace);
#endif
        }

        static int navFrame = -1;
        static Vector2Int navCache;
#if ENABLE_INPUT_SYSTEM
        static Vector2Int stickHeld;
        static float stickRepeat;
#endif

        /// <summary>Dirección de navegación del menú este cuadro (x: izq/der, y: arriba = +1).</summary>
        public static Vector2Int MenuNav()
        {
            if (navFrame == Time.frameCount) return navCache;
            navFrame = Time.frameCount;
            var d = Vector2Int.zero;
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) d.y += 1;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) d.y -= 1;
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) d.x += 1;
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) d.x -= 1;
            }
            var g = Gamepad.current;
            if (g != null)
            {
                if (g.dpad.up.wasPressedThisFrame) d.y += 1;
                if (g.dpad.down.wasPressedThisFrame) d.y -= 1;
                if (g.dpad.right.wasPressedThisFrame) d.x += 1;
                if (g.dpad.left.wasPressedThisFrame) d.x -= 1;

                // stick: un paso al inclinarlo y repetición si se mantiene
                Vector2 st = g.leftStick.ReadValue();
                var held = new Vector2Int(st.x > 0.6f ? 1 : st.x < -0.6f ? -1 : 0, st.y > 0.6f ? 1 : st.y < -0.6f ? -1 : 0);
                if (held != stickHeld) { stickHeld = held; stickRepeat = 0.4f; d += held; }
                else if (held != Vector2Int.zero)
                {
                    stickRepeat -= Time.unscaledDeltaTime;
                    if (stickRepeat <= 0f) { stickRepeat = 0.14f; d += held; }
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) d.y += 1;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) d.y -= 1;
            if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) d.x += 1;
            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) d.x -= 1;
#endif
            navCache = new Vector2Int(System.Math.Sign(d.x), System.Math.Sign(d.y));
            return navCache;
        }
    }
}
