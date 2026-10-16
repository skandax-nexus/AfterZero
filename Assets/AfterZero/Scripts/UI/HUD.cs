using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AfterZero
{
    /// <summary>
    /// Runtime-built HUD (no prefabs needed): state indicator, objective, prompt, subtitles,
    /// toasts, loop bar, crisis timer, journal, documents, choices, revelations, pause and fades.
    /// All public entry points are static and create the HUD on first use.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        static HUD instance;

        public static readonly Color Accent = new Color(0.45f, 0.95f, 1f);
        public static readonly Color AccentWarm = new Color(1f, 0.78f, 0.45f);
        public static readonly Color Dim = new Color(1f, 1f, 1f, 0.45f);
        public static readonly Color PanelColor = new Color(0.02f, 0.04f, 0.07f, 0.82f);

        Font font;
        RectTransform safeRoot;
        Rect lastSafeArea;
        Canvas canvas;
        RectTransform root;
        GameObject gameplayGroup;

        Text objectiveText, promptText, toastText, subtitleText, timerText, loopText, echoHint;
        Text[] stateTexts = new Text[3];
        Image[] stateBars = new Image[3];
        Image loopFill, fade, crosshair;
        GameObject titleCard, journalPanel, documentPanel, confirmPanel, revelationPanel, pausePanel;
        GameObject choicePanel, settingsPanel;
        Text choiceTitle;
        readonly System.Collections.Generic.List<Button> choiceButtons = new System.Collections.Generic.List<Button>();
        Action<int> choiceCallback;
        Image detectFill;
        Text detectLabel;
        Text titleSmall, titleBig, titleLoc, journalBody, documentTitle, documentBody, confirmBody, revelationTitle, revelationBody;

        float toastUntil;
        int menuOpenedFrame = -1;
        Action confirmYes, confirmNo;
        bool journalOpen, documentOpen, confirmOpen, pauseOpen, revelationOpen, choiceOpen;
        Coroutine subtitleRoutine;
        readonly Queue<SubtitleLine> subtitleQueue = new Queue<SubtitleLine>();

        public static bool Exists => instance != null;

        static HUD I
        {
            get
            {
                if (instance == null) Create();
                return instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { instance = null; }

        static void Create()
        {
            var go = new GameObject("[AfterZero HUD]");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<HUD>();
            instance.Build();
        }

        // =====================================================================
        // Public API
        // =====================================================================

        public static void SetObjective(string text)
        {
            I.objectiveText.text = string.IsNullOrEmpty(text) ? "" : "OBJECTIVE\n<size=30><b>" + text + "</b></size>";
        }

        public static void SetPrompt(string text) { I.promptText.text = text ?? ""; }

        public static void Toast(string text, float seconds)
        {
            var h = I;
            h.toastText.text = text;
            h.toastUntil = Time.unscaledTime + seconds;
        }

        public static string TimerLabel = "SECOND BELL IN";

        public static void SetTimer(float seconds)
        {
            var h = I;
            if (seconds < 0f) { h.timerText.text = ""; return; }
            int s = Mathf.Max(0, Mathf.CeilToInt(seconds));
            string label = string.Format("{0}  {1}:{2:00}", TimerLabel, s / 60, s % 60);
            h.timerText.text = seconds < 30f ? "<b>" + label + "</b>" : label;
            h.timerText.color = seconds < 30f ? new Color(1f, 0.45f, 0.35f) : AccentWarm;
        }

        public static void ShowTitleCard(string small, string big, string location)
        {
            var h = I;
            h.titleSmall.text = small;
            h.titleBig.text = big.ToUpperInvariant();
            h.titleLoc.text = location;
            h.StartCoroutine(h.TitleRoutine());
        }

        public static void ShowRevelation(string title, string body)
        {
            var h = I;
            h.revelationTitle.text = title.ToUpperInvariant();
            h.revelationBody.text = body;
            h.StartCoroutine(h.RevelationRoutine());
        }

        public static void ShowDocument(string title, string body)
        {
            var h = I;
            if (h.documentOpen) return;
            h.documentTitle.text = title;
            h.documentBody.text = body + (AZPlatform.UseTouchControls ? "\n\n<color=#7FE9FF>Tap to close</color>" : "\n\n<color=#7FE9FF>[E] / [Esc]  close</color>");
            h.documentPanel.SetActive(true);
            h.documentOpen = true;
            h.menuOpenedFrame = Time.frameCount;
            GameInput.PushLock();
        }

        /// <summary>Multiple-choice panel (dialogue, deduction). Disabled options are shown greyed out.</summary>
        public static void ShowChoices(string title, string[] options, bool[] enabled, Action<int> onPick)
        {
            var h = I;
            h.choiceTitle.text = title;
            for (int i = 0; i < h.choiceButtons.Count; i++)
            {
                var b = h.choiceButtons[i];
                bool show = i < options.Length;
                b.gameObject.SetActive(show);
                if (!show) continue;
                b.GetComponentInChildren<Text>().text = (i + 1) + ".  " + options[i];
                bool on = enabled == null || enabled[i];
                b.interactable = on;
                b.GetComponentInChildren<Text>().color = on ? Color.white : new Color(1f, 1f, 1f, 0.35f);
            }
            h.choiceCallback = onPick;
            h.choicePanel.SetActive(true);
            h.choiceOpen = true;
            h.menuOpenedFrame = Time.frameCount;
        }

        void PickChoice(int i)
        {
            if (!choiceOpen || !choiceButtons[i].interactable) return;
            choiceOpen = false;
            choicePanel.SetActive(false);
            var cb = choiceCallback;
            choiceCallback = null;
            cb?.Invoke(i);
        }

        public static void Confirm(string text, Action yes, Action no)
        {
            var h = I;
            if (h.confirmOpen) return;
            h.confirmBody.text = AZPlatform.UseTouchControls ? text : text + "\n\n<b>[Z] Yes</b>        [X] No";
            h.confirmYes = yes;
            h.confirmNo = no;
            h.confirmPanel.SetActive(true);
            h.confirmOpen = true;
            h.menuOpenedFrame = Time.frameCount;
            GameInput.PushLock();
        }

        internal static void EnqueueSubtitles(IEnumerable<SubtitleLine> lines, bool interrupt)
        {
            var h = I;
            if (interrupt) { h.subtitleQueue.Clear(); if (h.subtitleRoutine != null) h.StopCoroutine(h.subtitleRoutine); h.subtitleRoutine = null; }
            foreach (var l in lines) if (l != null && !string.IsNullOrEmpty(l.text)) h.subtitleQueue.Enqueue(l);
            if (h.subtitleRoutine == null) h.subtitleRoutine = h.StartCoroutine(h.SubtitleRoutine());
        }

        internal static IEnumerator Fade(float to, float seconds)
        {
            var h = I;
            float from = h.fade.color.a;
            float t = 0f;
            h.fade.raycastTarget = to > 0.5f;
            while (t < seconds)
            {
                t += Time.unscaledDeltaTime;
                h.fade.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds)));
                yield return null;
            }
            h.fade.color = new Color(0f, 0f, 0f, to);
        }

        // =====================================================================
        // Update
        // =====================================================================

        void Update()
        {
            ApplySafeArea();
            bool inGameplay = PlayerController.Instance != null;
            gameplayGroup.SetActive(inGameplay);

            if (toastText.text.Length > 0 && Time.unscaledTime > toastUntil) toastText.text = "";

            if (inGameplay) UpdateStateIndicator();
            if (inGameplay) UpdateDetection();
            UpdateLoopBar();
            HandleMenus(inGameplay);
        }

        void UpdateStateIndicator()
        {
            var tsm = TemporalStateManager.Instance;
            for (int i = 0; i < 3; i++)
            {
                var s = (TimeState)i;
                bool current = tsm != null && tsm.Current == s;
                bool allowed = tsm == null || tsm.IsAllowed(s);
                stateTexts[i].text = allowed ? (current ? "<b>" + s.Label() + "</b>" : s.Label()) : "<color=#FFFFFF44>" + s.ToString().ToUpperInvariant() + " · locked</color>";
                stateTexts[i].fontSize = current ? 26 : 20;
                stateTexts[i].color = current ? StateColor(s) : Dim;
                stateBars[i].enabled = current;
                stateBars[i].color = StateColor(s);
            }
            echoHint.text = tsm == null || AZPlatform.UseTouchControls ? "" : "[1][2][3] / [Q][R] shift state     hold [F] Echo Sense     [Tab] journal";
        }

        void UpdateDetection()
        {
            float d = KeeperPatrol.MaxDetection();
            detectFill.transform.parent.gameObject.SetActive(d > 0.01f);
            detectFill.fillAmount = d;
            detectFill.color = d >= 1f ? new Color(1f, 0.3f, 0.25f) : new Color(1f, 0.8f, 0.4f);
            detectLabel.text = d >= 1f ? "SEEN - BREAK LINE OF SIGHT" : d > 0.35f ? "SUSPICIOUS" : "NOTICED";
        }

        public static Color StateColor(TimeState s) =>
            s == TimeState.Past ? new Color(1f, 0.8f, 0.5f) : s == TimeState.Present ? new Color(0.6f, 0.85f, 1f) : new Color(0.85f, 0.5f, 1f);

        void UpdateLoopBar()
        {
            var lm = LoopManager.Instance;
            bool show = lm != null && lm.Running && PlayerController.Instance != null;
            loopFill.transform.parent.gameObject.SetActive(show);
            if (!show) { loopText.text = ""; return; }
            loopFill.fillAmount = lm.Normalized;
            int remain = Mathf.CeilToInt(lm.loopDuration - lm.Elapsed);
            loopText.text = string.Format("RECURRENCE {0:00}   ·   resets in {1:00}s", lm.LoopCount + 1, remain);
        }

        void HandleMenus(bool inGameplay)
        {
            // The button that opened a panel must not also close it on the same frame.
            if (Time.frameCount <= menuOpenedFrame + 1) return;
            if (choiceOpen) return;
            if (confirmOpen)
            {
                if (GameInput.Choice1.WasPressedThisFrame()) CloseConfirm(true);
                else if (GameInput.Choice2.WasPressedThisFrame() || GameInput.Pause.WasPressedThisFrame()) CloseConfirm(false);
                return;
            }
            if (documentOpen)
            {
                if (GameInput.Interact.WasPressedThisFrame() || GameInput.Pause.WasPressedThisFrame() || GameInput.Journal.WasPressedThisFrame())
                {
                    documentOpen = false;
                    documentPanel.SetActive(false);
                    GameInput.PopLock();
                }
                return;
            }
            if (!inGameplay) return;

            if (GameInput.Journal.WasPressedThisFrame() && !pauseOpen) ToggleJournal();
            if (GameInput.Pause.WasPressedThisFrame())
            {
                if (journalOpen) ToggleJournal();
                else TogglePause();
            }
        }

        void CloseConfirm(bool yes)
        {
            confirmOpen = false;
            confirmPanel.SetActive(false);
            GameInput.PopLock();
            var a = yes ? confirmYes : confirmNo;
            confirmYes = confirmNo = null;
            a?.Invoke();
        }

        void ToggleJournal()
        {
            journalOpen = !journalOpen;
            journalPanel.SetActive(journalOpen);
            if (journalOpen)
            {
                GameInput.PushLock();
                var sb = new System.Text.StringBuilder();
                var entries = WorldState.Journal;
                if (entries.Count == 0) sb.Append("<i>No evidence yet. Observe. Everything that repeats is a clue; everything that doesn't is a bigger one.</i>");
                for (int i = entries.Count - 1; i >= 0; i--)
                    sb.Append("<b><color=#7FE9FF>").Append(entries[i].title).Append("</color></b>\n").Append(entries[i].body).Append("\n\n");
                journalBody.text = sb.ToString();
            }
            else GameInput.PopLock();
        }

        void TogglePause()
        {
            pauseOpen = !pauseOpen;
            pausePanel.SetActive(pauseOpen);
            if (!pauseOpen && settingsPanel != null) settingsPanel.SetActive(false);
            Time.timeScale = pauseOpen ? 0f : 1f;
            AudioListener.pause = pauseOpen;
            if (pauseOpen) GameInput.PushLock(); else GameInput.PopLock();
        }

        // =====================================================================
        // Routines
        // =====================================================================

        IEnumerator TitleRoutine()
        {
            var cg = titleCard.GetComponent<CanvasGroup>();
            titleCard.SetActive(true);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime) { cg.alpha = t; yield return null; }
            cg.alpha = 1f;
            yield return new WaitForSecondsRealtime(2.6f);
            for (float t = 1; t > 0f; t -= Time.unscaledDeltaTime) { cg.alpha = t; yield return null; }
            titleCard.SetActive(false);
        }

        IEnumerator RevelationRoutine()
        {
            while (revelationOpen) yield return null;
            revelationOpen = true;
            AudioCues.Play(Cue.Revelation);
            var cg = revelationPanel.GetComponent<CanvasGroup>();
            revelationPanel.SetActive(true);
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime * 1.5f) { cg.alpha = t; yield return null; }
            cg.alpha = 1f;
            yield return new WaitForSecondsRealtime(5.5f);
            for (float t = 1; t > 0f; t -= Time.unscaledDeltaTime) { cg.alpha = t; yield return null; }
            revelationPanel.SetActive(false);
            revelationOpen = false;
        }

        IEnumerator SubtitleRoutine()
        {
            while (subtitleQueue.Count > 0)
            {
                var l = subtitleQueue.Dequeue();
                subtitleText.text = string.IsNullOrEmpty(l.speaker) ? l.text : "<b><color=#7FE9FF>" + l.speaker.ToUpperInvariant() + "</color></b>   " + l.text;
                yield return new WaitForSeconds(l.duration);
            }
            subtitleText.text = "";
            subtitleRoutine = null;
        }

        // =====================================================================
        // Construction
        // =====================================================================

        void Build()
        {
            GameInput.Ensure();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            // Everything lives inside the device safe area (notches, rounded corners, home bar).
            root = NewRect("SafeArea", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            safeRoot = root;
            ApplySafeArea();

            EnsureEventSystem();

            gameplayGroup = NewRect("Gameplay", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var g = (RectTransform)gameplayGroup.transform;

            // Crosshair
            crosshair = NewImage("Crosshair", g, new Color(1f, 1f, 1f, 0.7f));
            Place(crosshair.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6, 6));

            // Objective (top-left)
            objectiveText = NewText("Objective", g, 20, TextAnchor.UpperLeft, new Color(0.9f, 0.97f, 1f));
            Place(objectiveText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -40), new Vector2(760, 120));
            var objLine = NewImage("ObjLine", g, Accent);
            Place(objLine.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(32, -40), new Vector2(3, 70));

            // State indicator (top-right)
            for (int i = 0; i < 3; i++)
            {
                stateTexts[i] = NewText("State" + i, g, 20, TextAnchor.MiddleRight, Dim);
                Place(stateTexts[i].rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-48, -44 - i * 40), new Vector2(360, 36));
                stateBars[i] = NewImage("StateBar" + i, g, Accent);
                Place(stateBars[i].rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -44 - i * 40), new Vector2(4, 30));
            }

            // Prompt (centre, below crosshair)
            promptText = NewText("Prompt", g, 24, TextAnchor.MiddleCenter, Color.white);
            Place(promptText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -70), new Vector2(900, 40));
            AddShadow(promptText);

            // Toast (upper centre)
            toastText = NewText("Toast", g, 24, TextAnchor.MiddleCenter, AccentWarm);
            Place(toastText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(1300, 60));
            AddShadow(toastText);

            // Timer
            timerText = NewText("Timer", g, 30, TextAnchor.MiddleCenter, AccentWarm);
            Place(timerText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -60), new Vector2(800, 50));
            AddShadow(timerText);

            // Loop bar (bottom)
            var loopBg = NewImage("LoopBar", g, new Color(1f, 1f, 1f, 0.12f));
            Place(loopBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 40), new Vector2(700, 4));
            loopFill = NewImage("LoopFill", loopBg.rectTransform, AccentWarm);
            loopFill.type = Image.Type.Filled;
            loopFill.fillMethod = Image.FillMethod.Horizontal;
            loopFill.sprite = WhiteSprite();
            Stretch(loopFill.rectTransform);
            loopText = NewText("LoopText", g, 16, TextAnchor.MiddleCenter, Dim);
            Place(loopText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 58), new Vector2(700, 24));

            // Controls hint
            echoHint = NewText("Hint", g, 15, TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.35f));
            Place(echoHint.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 20), new Vector2(900, 24));

            // Subtitles (always visible, also in menus)
            subtitleText = NewText("Subtitles", root, 26, TextAnchor.LowerCenter, Color.white);
            Place(subtitleText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 90), new Vector2(1400, 110));
            AddShadow(subtitleText);

            // Title card
            titleCard = NewPanel("TitleCard", root, new Color(0, 0, 0, 0)).gameObject;
            titleCard.AddComponent<CanvasGroup>();
            titleSmall = NewText("Small", titleCard.transform, 22, TextAnchor.MiddleCenter, Accent);
            Place(titleSmall.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1200, 40));
            titleBig = NewText("Big", titleCard.transform, 64, TextAnchor.MiddleCenter, Color.white);
            Place(titleBig.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1700, 100));
            AddShadow(titleBig);
            var bar = NewImage("Bar", titleCard.transform, Accent);
            Place(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(420, 2));
            titleLoc = NewText("Loc", titleCard.transform, 24, TextAnchor.MiddleCenter, Dim);
            Place(titleLoc.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -75), new Vector2(1200, 40));
            titleCard.SetActive(false);

            // Revelation (clue & payoff)
            revelationPanel = NewPanel("Revelation", root, new Color(0f, 0f, 0f, 0.55f)).gameObject;
            revelationPanel.GetComponent<Image>().raycastTarget = false; // never swallow touches
            revelationPanel.AddComponent<CanvasGroup>();
            var revTag = NewText("Tag", revelationPanel.transform, 20, TextAnchor.MiddleCenter, Accent);
            revTag.text = "REVELATION";
            Place(revTag.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 120), new Vector2(800, 30));
            revelationTitle = NewText("Title", revelationPanel.transform, 46, TextAnchor.MiddleCenter, Color.white);
            Place(revelationTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1600, 80));
            revelationBody = NewText("Body", revelationPanel.transform, 28, TextAnchor.UpperCenter, new Color(0.9f, 0.95f, 1f));
            Place(revelationBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1300, 200));
            revelationPanel.SetActive(false);

            // Journal
            journalPanel = NewPanel("Journal", root, PanelColor).gameObject;
            var jt = NewText("Title", journalPanel.transform, 34, TextAnchor.UpperLeft, Accent);
            jt.text = "CASE JOURNAL  ·  ARIN";
            Place(jt.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(1200, 50));
            journalBody = NewText("Body", journalPanel.transform, 22, TextAnchor.UpperLeft, Color.white);
            Place(journalBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -150), new Vector2(1200, 820));
            journalBody.verticalOverflow = VerticalWrapMode.Truncate;
            journalPanel.SetActive(false);

            // Document
            documentPanel = NewPanel("Document", root, new Color(0.03f, 0.03f, 0.04f, 0.9f)).gameObject;
            var paper = NewImage("Paper", documentPanel.transform, new Color(0.93f, 0.9f, 0.82f, 1f));
            Place(paper.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 760));
            documentTitle = NewText("Title", paper.transform, 32, TextAnchor.UpperCenter, new Color(0.15f, 0.12f, 0.1f));
            Place(documentTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -50), new Vector2(780, 50));
            documentBody = NewText("Body", paper.transform, 24, TextAnchor.UpperLeft, new Color(0.18f, 0.15f, 0.12f));
            Place(documentBody.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -130), new Vector2(760, 590));
            documentPanel.SetActive(false);

            // Confirm
            confirmPanel = NewPanel("Confirm", root, new Color(0, 0, 0, 0.6f)).gameObject;
            var cbox = NewImage("Box", confirmPanel.transform, PanelColor);
            Place(cbox.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 420));
            confirmBody = NewText("Body", cbox.transform, 28, TextAnchor.MiddleCenter, Color.white);
            Stretch(confirmBody.rectTransform, 40);
            confirmBody.rectTransform.offsetMin = new Vector2(40, 110);
            confirmPanel.SetActive(false);

            // Pause
            pausePanel = NewPanel("Pause", root, PanelColor).gameObject;
            var pt = NewText("Title", pausePanel.transform, 48, TextAnchor.MiddleCenter, Color.white);
            pt.text = "PAUSED";
            Place(pt.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(800, 80));
            MakeButton(pausePanel.transform, "Resume", new Vector2(0, 60), TogglePause);
            MakeButton(pausePanel.transform, "Restart level", new Vector2(0, -20), () => { TogglePause(); GameFlow.RestartLevel(); });
            MakeButton(pausePanel.transform, "Main menu", new Vector2(0, -100), () => { TogglePause(); GameFlow.LoadScene(LevelCatalog.MainMenu); });
            MakeButton(pausePanel.transform, "Settings", new Vector2(0, -180), () => OpenSettings(true));
            MakeButton(pausePanel.transform, "Quit", new Vector2(0, -260), Application.Quit);
            pausePanel.SetActive(false);

            // Choice panel (dialogue + deduction).
            choicePanel = NewPanel("Choices", root, new Color(0f, 0f, 0f, 0.55f)).gameObject;
            var cbx = NewImage("Box", choicePanel.transform, PanelColor);
            Place(cbx.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(1300, 560));
            choiceTitle = NewText("Title", cbx.transform, 28, TextAnchor.UpperCenter, Accent);
            Place(choiceTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(1200, 90));
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var b = MakeButton(cbx.transform, "Option", new Vector2(0, 130 - i * 84), () => PickChoice(idx));
                ((RectTransform)b.transform).sizeDelta = new Vector2(1180, 70);
                var t = b.GetComponentInChildren<Text>();
                t.alignment = TextAnchor.MiddleLeft;
                t.fontSize = 24;
                t.rectTransform.offsetMin = new Vector2(24, 0);
                choiceButtons.Add(b);
            }
            choicePanel.SetActive(false);

            BuildSettings();

            // Detection meter (top centre, under the timer).
            var dBack = NewImage("Detection", g, new Color(0f, 0f, 0f, 0.5f));
            Place(dBack.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -112), new Vector2(320, 10));
            detectFill = NewImage("Fill", dBack.transform, Color.white);
            detectFill.type = Image.Type.Filled;
            detectFill.fillMethod = Image.FillMethod.Horizontal;
            detectFill.sprite = WhiteSprite();
            Stretch(detectFill.rectTransform);
            detectLabel = NewText("Label", dBack.transform, 16, TextAnchor.MiddleCenter, Color.white);
            Place(detectLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0, -6), new Vector2(500, 24));
            dBack.gameObject.SetActive(false);

            // Touch: tap anywhere on a document / journal to close it, tap Yes/No on confirmations.
            TapToClose(documentPanel, () => { documentOpen = false; documentPanel.SetActive(false); GameInput.PopLock(); });
            TapToClose(journalPanel, ToggleJournal);
            MakeButton(cbox.transform, "Yes", new Vector2(-150, -150), () => { if (confirmOpen) CloseConfirm(true); });
            MakeButton(cbox.transform, "No", new Vector2(150, -150), () => { if (confirmOpen) CloseConfirm(false); });
            MobileControls.Ensure(gameplayGroup.GetComponent<RectTransform>(), this);
            if (AZPlatform.UseTouchControls)
            {
                subtitleText.fontSize = 32;
                promptText.fontSize = 30;
                objectiveText.fontSize = 24;
            }

            // Fade (top-most)
            fade = NewImage("Fade", root, new Color(0, 0, 0, 0));
            Stretch(fade.rectTransform);
            fade.raycastTarget = false;
        }

        // ---------- settings ----------
        readonly System.Collections.Generic.List<Action> settingsRefresh = new System.Collections.Generic.List<Action>();

        public static void OpenSettings(bool open)
        {
            var h = I;
            h.settingsPanel.SetActive(open);
            foreach (var r in h.settingsRefresh) r();
        }

        void BuildSettings()
        {
            settingsPanel = NewPanel("Settings", root, new Color(0.02f, 0.03f, 0.05f, 0.96f)).gameObject;
            var title = NewText("Title", settingsPanel.transform, 42, TextAnchor.MiddleCenter, Color.white);
            title.text = "SETTINGS";
            Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -70), new Vector2(800, 60));
            float y = 250f;
            Row("Difficulty", ref y, () => GameSettings.DifficultyLabel(GameSettings.Difficulty),
                () => GameSettings.Difficulty = (Difficulty)(((int)GameSettings.Difficulty + 2) % 3),
                () => GameSettings.Difficulty = (Difficulty)(((int)GameSettings.Difficulty + 1) % 3));
            Row("Look sensitivity", ref y, () => GameSettings.LookSensitivity.ToString("0.0") + "x",
                () => GameSettings.LookSensitivity -= 0.1f, () => GameSettings.LookSensitivity += 0.1f);
            Row("Invert look", ref y, () => GameSettings.InvertY ? "ON" : "OFF",
                () => GameSettings.InvertY = !GameSettings.InvertY, () => GameSettings.InvertY = !GameSettings.InvertY);
            Row("Graphics", ref y, () => GameSettings.Quality < 0 ? "AUTO" : new[] { "LOW (30 fps, battery saver)", "MEDIUM", "HIGH" }[GameSettings.Quality],
                () => GameSettings.Quality = GameSettings.Quality <= -1 ? 2 : GameSettings.Quality - 1,
                () => GameSettings.Quality = GameSettings.Quality >= 2 ? -1 : GameSettings.Quality + 1);
            Row("Touch layout", ref y, () => GameSettings.LeftHanded ? "LEFT-HANDED" : "RIGHT-HANDED",
                () => GameSettings.LeftHanded = !GameSettings.LeftHanded, () => GameSettings.LeftHanded = !GameSettings.LeftHanded);
            Row("Vibration", ref y, () => GameSettings.Vibration ? "ON" : "OFF",
                () => GameSettings.Vibration = !GameSettings.Vibration, () => GameSettings.Vibration = !GameSettings.Vibration);
            Row("Subtitle size", ref y, () => Mathf.RoundToInt(GameSettings.SubtitleScale * 100f) + "%",
                () => GameSettings.SubtitleScale -= 0.1f, () => GameSettings.SubtitleScale += 0.1f);
            MakeButton(settingsPanel.transform, "Back", new Vector2(0, y - 40f), () => OpenSettings(false));
            settingsPanel.SetActive(false);
            GameSettings.Changed += () =>
            {
                foreach (var r in settingsRefresh) r();
                subtitleText.fontSize = Mathf.RoundToInt((AZPlatform.UseTouchControls ? 32 : 26) * GameSettings.SubtitleScale);
            };
            subtitleText.fontSize = Mathf.RoundToInt((AZPlatform.UseTouchControls ? 32 : 26) * GameSettings.SubtitleScale);
        }

        void Row(string label, ref float y, Func<string> value, Action dec, Action inc)
        {
            var l = NewText(label, settingsPanel.transform, 24, TextAnchor.MiddleRight, HUD.Dim);
            l.text = label.ToUpperInvariant();
            Place(l.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-330, y), new Vector2(360, 60));
            var minus = MakeButton(settingsPanel.transform, "<", new Vector2(-250, y), () => dec());
            ((RectTransform)minus.transform).sizeDelta = new Vector2(80, 60);
            var v = NewText("Value", settingsPanel.transform, 22, TextAnchor.MiddleCenter, Color.white);
            Place(v.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(110, y), new Vector2(620, 60));
            var plus = MakeButton(settingsPanel.transform, ">", new Vector2(470, y), () => inc());
            ((RectTransform)plus.transform).sizeDelta = new Vector2(80, 60);
            settingsRefresh.Add(() => v.text = value());
            y -= 82f;
        }

        void TapToClose(GameObject panel, UnityEngine.Events.UnityAction action)
        {
            var b = panel.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { if (Time.frameCount > menuOpenedFrame + 1) action(); });
        }

        void ApplySafeArea()
        {
            if (safeRoot == null) return;
            Rect sa = Screen.safeArea;
            if (sa == lastSafeArea || Screen.width <= 0 || Screen.height <= 0) return;
            lastSafeArea = sa;
            safeRoot.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            safeRoot.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            safeRoot.offsetMin = safeRoot.offsetMax = Vector2.zero;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("[EventSystem]");
            DontDestroyOnLoad(es);
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        // ---------- UI helpers (also used by the main menu) ----------

        static Sprite whiteSprite;
        public static Sprite WhiteSprite()
        {
            if (whiteSprite == null)
                whiteSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            return whiteSprite;
        }

        public static RectTransform NewRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
            return rt;
        }

        public Text NewText(string name, Transform parent, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Image NewImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var i = go.AddComponent<Image>();
            i.color = color;
            i.raycastTarget = false;
            return i;
        }

        public static RectTransform NewPanel(string name, Transform parent, Color color)
        {
            var img = NewImage(name, parent, color);
            Stretch(img.rectTransform);
            img.raycastTarget = color.a > 0.01f;
            return img.rectTransform;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset); rt.offsetMax = new Vector2(-inset, -inset);
        }

        static void AddShadow(Text t)
        {
            var s = t.gameObject.AddComponent<Shadow>();
            s.effectColor = new Color(0, 0, 0, 0.8f);
            s.effectDistance = new Vector2(2, -2);
        }

        public Button MakeButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var img = NewImage(label, parent, new Color(0.45f, 0.95f, 1f, 0.08f));
            img.raycastTarget = true;
            Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(460, 64));
            var b = img.gameObject.AddComponent<Button>();
            var colors = b.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            colors.selectedColor = new Color(1.6f, 1.6f, 1.6f, 1f);
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            b.colors = colors;
            b.onClick.AddListener(onClick);
            var edge = NewImage("Edge", img.transform, Accent);
            Place(edge.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(3, 64));
            var t = NewText("Label", img.transform, 26, TextAnchor.MiddleCenter, Color.white);
            t.text = label.ToUpperInvariant();
            Stretch(t.rectTransform);
            return b;
        }

        /// <summary>Lets other UI (main menu) build with the HUD's font and helpers.</summary>
        public static HUD Shared => I;
    }
}
