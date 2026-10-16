using UnityEngine;
using UnityEngine.UI;

namespace AfterZero
{
    /// <summary>Title screen: New Game, Continue, level select, quit. Built at runtime.</summary>
    public class MainMenu : MonoBehaviour
    {
        GameObject levelPanel;

        void Start()
        {
            GameInput.Ensure();
            GameInput.PushLock();
            Time.timeScale = 1f;
            var hud = HUD.Shared;
            StartCoroutine(ScreenFader.FadeIn(1.2f));

            var canvasGo = new GameObject("MenuCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            var tag = hud.NewText("Tag", root, 24, TextAnchor.MiddleLeft, HUD.Accent);
            tag.text = "AFTERZERO  ·  GAME 01";
            HUD.Place(tag.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(140, 250), new Vector2(900, 40));
            var title = hud.NewText("Title", root, 96, TextAnchor.MiddleLeft, Color.white);
            title.text = "THE FIRST CYCLE";
            HUD.Place(title.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(140, 170), new Vector2(1400, 130));
            var sub = hud.NewText("Sub", root, 26, TextAnchor.MiddleLeft, HUD.Dim);
            sub.text = "A city repeats a catastrophe. A watch that doesn't tick. A letter in your own handwriting.";
            var diff = hud.NewText("Difficulty", root, 20, TextAnchor.MiddleLeft, HUD.Accent);
            HUD.Place(diff.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(140, 40), new Vector2(1400, 30));
            diff.text = "Difficulty: " + GameSettings.DifficultyLabel(GameSettings.Difficulty) + "   (change in Settings)";
            GameSettings.Changed += () => { if (diff != null) diff.text = "Difficulty: " + GameSettings.DifficultyLabel(GameSettings.Difficulty) + "   (change in Settings)"; };
            HUD.Place(sub.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(140, 90), new Vector2(1400, 40));

            float y = -20f;
            if (SaveSystem.HasSave)
            {
                Left(hud.MakeButton(root, "Continue", new Vector2(0, y), GameFlow.Continue));
                y -= 80f;
            }
            Left(hud.MakeButton(root, "New game", new Vector2(0, y), GameFlow.NewGame)); y -= 80f;
            Left(hud.MakeButton(root, "Select level", new Vector2(0, y), () => levelPanel.SetActive(!levelPanel.activeSelf))); y -= 80f;
            Left(hud.MakeButton(root, "Settings", new Vector2(0, y), () => HUD.OpenSettings(true))); y -= 80f;
            Left(hud.MakeButton(root, "Quit", new Vector2(0, y), Application.Quit));

            levelPanel = new GameObject("Levels", typeof(RectTransform));
            levelPanel.transform.SetParent(root, false);
            HUD.Place((RectTransform)levelPanel.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-120, 0), new Vector2(620, 800));
            for (int i = 0; i < LevelCatalog.Scenes.Length; i++)
            {
                int idx = i;
                var b = hud.MakeButton(levelPanel.transform, string.Format("{0:00}  {1}", i + 1, LevelCatalog.Titles[i]), new Vector2(0, 300 - i * 84), () => GameFlow.LoadLevel(idx));
                ((RectTransform)b.transform).sizeDelta = new Vector2(600, 64);
                b.GetComponentInChildren<Text>().fontSize = 20;
            }
            levelPanel.SetActive(false);
        }

        void OnDestroy() { GameInput.PopLock(); }

        static void Left(Button b)
        {
            var rt = (RectTransform)b.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(140, rt.anchoredPosition.y);
        }

        void Update()
        {
            // The menu always owns the cursor (a scene-load reset may have cleared our lock).
            if (!GameInput.GameplayLocked) GameInput.PushLock();

            // Slow drift of the menu camera over the city.
            var cam = Camera.main;
            if (cam != null) cam.transform.Rotate(0f, 2f * Time.deltaTime, 0f, Space.World);
        }
    }
}
