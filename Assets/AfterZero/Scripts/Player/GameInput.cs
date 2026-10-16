using UnityEngine;
using UnityEngine.InputSystem;

namespace AfterZero
{
    /// <summary>
    /// Code-defined Input System actions (keyboard/mouse + gamepad), so the project needs no
    /// .inputactions asset. Keyboard: WASD, mouse, Space jump, Shift sprint, C/Ctrl crouch,
    /// E interact, 1/2/3 Past/Present/Future, Q/R cycle state, hold F Echo Sense, Tab journal, Esc pause.
    /// </summary>
    public static class GameInput
    {
        public static InputAction Move { get; private set; }
        public static InputAction Look { get; private set; }
        public static InputAction Jump { get; private set; }
        public static InputAction Sprint { get; private set; }
        public static InputAction Crouch { get; private set; }
        public static InputAction Interact { get; private set; }
        public static InputAction StatePrev { get; private set; }
        public static InputAction StateNext { get; private set; }
        public static InputAction StatePast { get; private set; }
        public static InputAction StatePresent { get; private set; }
        public static InputAction StateFuture { get; private set; }
        public static InputAction Journal { get; private set; }
        public static InputAction Pause { get; private set; }
        public static InputAction Choice1 { get; private set; }
        public static InputAction Choice2 { get; private set; }
        public static InputAction Echo { get; private set; }
        public static InputAction Hum { get; private set; }

        static int lockCount;
        static bool created;

        /// <summary>True while a UI (journal, choice, pause, cinematic) owns the controls.</summary>
        public static bool GameplayLocked => lockCount > 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { lockCount = 0; }

        /// <summary>Clears all UI locks (used after a scene load).</summary>
        public static void ResetLocks() { lockCount = 0; UpdateCursor(); }

        public static void PushLock() { lockCount++; UpdateCursor(); }
        public static void PopLock() { lockCount = Mathf.Max(0, lockCount - 1); UpdateCursor(); }

        public static void UpdateCursor()
        {
            bool free = lockCount > 0 || AZPlatform.UseTouchControls;
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        public static void Ensure()
        {
            if (created) return;
            created = true;

            Move = new InputAction("Move", InputActionType.Value);
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            Move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            Move.AddBinding("<Gamepad>/leftStick");

            Look = new InputAction("Look", InputActionType.Value);
            Look.AddBinding("<Mouse>/delta");
            Look.AddBinding("<Gamepad>/rightStick").WithProcessor("scaleVector2(x=12,y=12)");

            Jump = Button("Jump", "<Keyboard>/space", "<Gamepad>/buttonSouth");
            Sprint = Button("Sprint", "<Keyboard>/leftShift", "<Gamepad>/leftStickPress");
            Crouch = Button("Crouch", "<Keyboard>/c", "<Gamepad>/buttonEast");
            Crouch.AddBinding("<Keyboard>/leftCtrl");
            Interact = Button("Interact", "<Keyboard>/e", "<Gamepad>/buttonWest");
            Interact.AddBinding("<Mouse>/leftButton");
            StatePrev = Button("StatePrev", "<Keyboard>/q", "<Gamepad>/leftShoulder");
            StateNext = Button("StateNext", "<Keyboard>/r", "<Gamepad>/rightShoulder");
            StatePast = Button("StatePast", "<Keyboard>/1", "<Gamepad>/dpad/left");
            StatePresent = Button("StatePresent", "<Keyboard>/2", "<Gamepad>/dpad/up");
            StateFuture = Button("StateFuture", "<Keyboard>/3", "<Gamepad>/dpad/right");
            Journal = Button("Journal", "<Keyboard>/tab", "<Gamepad>/select");
            Pause = Button("Pause", "<Keyboard>/escape", "<Gamepad>/start");
            Choice1 = Button("Choice1", "<Keyboard>/f1", "<Gamepad>/buttonWest");
            Choice1.AddBinding("<Keyboard>/z");
            Choice2 = Button("Choice2", "<Keyboard>/f2", "<Gamepad>/buttonNorth");
            Choice2.AddBinding("<Keyboard>/x");
            Echo = Button("Echo", "<Keyboard>/f", "<Gamepad>/rightStickPress");
            Hum = Button("Hum", "<Keyboard>/h", "<Gamepad>/buttonNorth");

            foreach (var a in new[] { Move, Look, Jump, Sprint, Crouch, Interact, StatePrev, StateNext, StatePast,
                                      StatePresent, StateFuture, Journal, Pause, Choice1, Choice2, Echo, Hum })
                a.Enable();
        }

        static InputAction Button(string name, string kb, string pad)
        {
            var a = new InputAction(name, InputActionType.Button);
            a.AddBinding(kb);
            a.AddBinding(pad);
            return a;
        }
    }
}
