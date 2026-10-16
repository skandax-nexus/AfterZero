using UnityEngine;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.UI;

namespace AfterZero
{
    /// <summary>
    /// On-screen touch controls built at runtime. The joystick and buttons drive a virtual gamepad
    /// through Input System on-screen controls, so every existing gamepad binding works unchanged.
    /// Layout (landscape): move stick bottom-left, look by dragging the right half, action buttons
    /// bottom-right, state buttons bottom-centre, system buttons top-centre.
    /// </summary>
    public class MobileControls : MonoBehaviour
    {
        public static MobileControls Instance { get; private set; }

        Text interactLabel;
        readonly System.Collections.Generic.List<(RectTransform rt, Vector2 min, Vector2 max, Vector2 pos)> layout =
            new System.Collections.Generic.List<(RectTransform, Vector2, Vector2, Vector2)>();

        void ApplyLayout()
        {
            bool mirror = GameSettings.LeftHanded;
            foreach (var (rt, min, max, pos) in layout)
            {
                if (rt == null) continue;
                if (!mirror) { rt.anchorMin = min; rt.anchorMax = max; rt.anchoredPosition = pos; continue; }
                rt.anchorMin = new Vector2(1f - max.x, min.y);
                rt.anchorMax = new Vector2(1f - min.x, max.y);
                rt.anchoredPosition = new Vector2(-pos.x, pos.y);
            }
        }

        public static void Ensure(RectTransform parent, HUD hud)
        {
            if (Instance != null || !AZPlatform.UseTouchControls) return;
            var go = new GameObject("TouchControls", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            HUD.Stretch((RectTransform)go.transform);
            Instance = go.AddComponent<MobileControls>();
            Instance.Build(hud);
        }

        void Build(HUD hud)
        {
            var root = (RectTransform)transform;

            // Look area: right 60% of the screen, behind the buttons.
            var look = HUD.NewImage("LookArea", root, new Color(1f, 1f, 1f, 0f));
            look.raycastTarget = true;
            look.rectTransform.anchorMin = new Vector2(0.4f, 0f);
            look.rectTransform.anchorMax = new Vector2(1f, 1f);
            look.rectTransform.offsetMin = look.rectTransform.offsetMax = Vector2.zero;
            look.gameObject.AddComponent<TouchLookArea>();

            // Move stick.
            var baseImg = HUD.NewImage("MoveStickBase", root, new Color(1f, 1f, 1f, 0.08f));
            HUD.Place(baseImg.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(230, 230), new Vector2(260, 260));
            baseImg.sprite = Circle();
            var knob = HUD.NewImage("MoveStick", baseImg.transform, new Color(0.45f, 0.95f, 1f, 0.35f));
            knob.sprite = Circle();
            knob.raycastTarget = true;
            HUD.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120, 120));
            knob.gameObject.SetActive(false);
            var stick = knob.gameObject.AddComponent<OnScreenStick>();
            stick.movementRange = 90f;
            stick.controlPath = "<Gamepad>/leftStick";
            knob.gameObject.SetActive(true);

            // Action buttons (bottom-right).
            interactLabel = Button(hud, root, "USE", new Vector2(1, 0), new Vector2(-170, 250), 150, "<Gamepad>/buttonWest", HUD.Accent);
            Button(hud, root, "JUMP", new Vector2(1, 0), new Vector2(-330, 130), 120, "<Gamepad>/buttonSouth", Color.white);
            Button(hud, root, "CROUCH", new Vector2(1, 0), new Vector2(-150, 90), 120, "<Gamepad>/buttonEast", Color.white);
            Button(hud, root, "ECHO", new Vector2(1, 0), new Vector2(-330, 290), 110, "<Gamepad>/rightStickPress", new Color(0.6f, 0.85f, 1f));
            Button(hud, root, "HUM", new Vector2(1, 0), new Vector2(-170, 420), 100, "<Gamepad>/buttonNorth", new Color(1f, 0.8f, 0.5f));

            // Remember the right-handed layout so it can be mirrored for left-handed players.
            for (int i = 0; i < root.childCount; i++)
            {
                var rt = (RectTransform)root.GetChild(i);
                layout.Add((rt, rt.anchorMin, rt.anchorMax, rt.anchoredPosition));
            }
            ApplyLayout();
            GameSettings.Changed += ApplyLayout;

            // State buttons (bottom-centre) — label + glyph, never colour alone.
            Button(hud, root, "<< PAST", new Vector2(0.5f, 0), new Vector2(-200, 70), 120, "<Gamepad>/dpad/left", HUD.StateColor(TimeState.Past));
            Button(hud, root, "[ NOW ]", new Vector2(0.5f, 0), new Vector2(0, 70), 120, "<Gamepad>/dpad/up", HUD.StateColor(TimeState.Present));
            Button(hud, root, "FUTURE >>", new Vector2(0.5f, 0), new Vector2(200, 70), 120, "<Gamepad>/dpad/right", HUD.StateColor(TimeState.Future));

            // System buttons (top-centre, clear of the objective and state indicator).
            Button(hud, root, "JOURNAL", new Vector2(0.5f, 1), new Vector2(-80, -60), 90, "<Gamepad>/select", HUD.Dim);
            Button(hud, root, "II", new Vector2(0.5f, 1), new Vector2(40, -60), 90, "<Gamepad>/start", HUD.Dim);
        }

        Text Button(HUD hud, RectTransform root, string label, Vector2 anchor, Vector2 pos, float size, string path, Color color)
        {
            var img = HUD.NewImage(label, root, new Color(color.r, color.g, color.b, 0.16f));
            img.sprite = Circle();
            img.raycastTarget = true;
            HUD.Place(img.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            img.gameObject.SetActive(false);
            var b = img.gameObject.AddComponent<OnScreenButton>();
            b.controlPath = path;
            img.gameObject.SetActive(true);
            var t = hud.NewText("Label", img.transform, size > 130 ? 26 : 20, TextAnchor.MiddleCenter, color);
            t.text = label;
            HUD.Stretch(t.rectTransform);
            return t;
        }

        void Update()
        {
            bool gameplay = PlayerController.Instance != null && !GameInput.GameplayLocked;
            for (int i = 0; i < transform.childCount; i++) transform.GetChild(i).gameObject.SetActive(gameplay || transform.GetChild(i).name == "JOURNAL" && PlayerController.Instance != null);
            if (interactLabel != null)
            {
                var it = PlayerController.Instance != null ? PlayerController.Instance.GetComponent<PlayerInteractor>() : null;
                interactLabel.text = it != null && it.Focus != null ? "USE" : it != null && it.Held != null ? "DROP" : "USE";
                var img = interactLabel.transform.parent.GetComponent<Image>();
                if (img != null) img.color = new Color(HUD.Accent.r, HUD.Accent.g, HUD.Accent.b, it != null && (it.Focus != null || it.Held != null) ? 0.45f : 0.12f);
            }
        }

        void OnDestroy()
        {
            GameSettings.Changed -= ApplyLayout;
            if (Instance == this) Instance = null;
        }

        static Sprite circle;
        static Sprite Circle()
        {
            if (circle != null) return circle;
            const int n = 128;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var px = new Color32[n * n];
            float r = n * 0.5f - 1f;
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n * 0.5f, n * 0.5f));
                float a = Mathf.Clamp01(r - d);
                float ring = d > r - 4f ? 1f : 0.55f;
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * ring * 255));
            }
            tex.SetPixels32(px);
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
            return circle;
        }
    }
}
