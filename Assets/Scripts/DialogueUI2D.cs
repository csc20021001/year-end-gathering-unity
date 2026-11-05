using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace YearEnd
{
    public sealed class DialogueUI2D : MonoBehaviour
    {
        public static DialogueUI2D Active { get; private set; }
        public DialogueScript2D script;
        private GameObject overlay;
        private Image leftPortrait, rightPortrait;
        private Text nameLabel, bodyText, continueHint;
        private Font runtimeFont;
        private readonly List<DialoguePage> pages = new List<DialoguePage>();
        private int pageIndex;
        private int shownCharacters;
        private Coroutine typing;
        private AudioSource typingAudio;
        private Action onFinished;
        private bool running;
        private string currentEvent;

        private struct DialoguePage
        {
            public DialogueSpeaker2D speaker;
            public string text;
            public DialoguePage(DialogueSpeaker2D speaker, string text)
            { this.speaker = speaker; this.text = text; }
        }

        public static DialogueUI2D Ensure(SceneWorld2D scene)
        {
            if (scene == null || scene.hud == null) return null;
            Canvas canvas = scene.hud.GetComponent<Canvas>();
            if (canvas == null) return null;
            DialogueUI2D component = canvas.GetComponent<DialogueUI2D>();
            if (component == null) component = canvas.gameObject.AddComponent<DialogueUI2D>();
            component.EnsureVisuals(canvas.transform);
            return component;
        }


        private void Awake()
        {
            Active = this;
            script = Resources.Load<DialogueScript2D>("DialogueScriptV1");

            typingAudio = gameObject.AddComponent<AudioSource>();
            typingAudio.playOnAwake = false;
            typingAudio.loop = false;
            typingAudio.spatialBlend = 0f;
        }


        private static RectTransform NewRect(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            RectTransform r = obj.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(.5f,.5f);
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }
        private static Image NewImage(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            Image image = NewRect(name, parent, pos, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
        private Text NewText(string name, Transform parent, Vector2 pos, Vector2 size,
            int fontSize, Color color, TextAnchor alignment = TextAnchor.UpperLeft)
        {
            Text t = NewRect(name, parent, pos, size).gameObject.AddComponent<Text>();
            t.font = runtimeFont;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = alignment;
            t.supportRichText = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.resizeTextForBestFit = false;
            t.raycastTarget = false;
            return t;
        }
        private void EnsureVisuals(Transform canvas)
        {
            if (overlay != null) return;
            runtimeFont = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft JhengHei", "Noto Sans CJK TC", "PingFang TC", "Arial" }, 32);
            if (runtimeFont == null)
                runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var root = NewRect("Dialogue Overlay [runtime; script in Resources/DialogueScriptV1]",
                canvas, Vector2.zero, new Vector2(1600, 900));
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            overlay = root.gameObject;
            root.SetAsLastSibling();

            Image dark = NewImage("Dim scene behind dialogue", root, Vector2.zero,
                new Vector2(1600, 900), new Color(.055f, .065f, .055f, .40f));
            dark.raycastTarget = true;  

            Image upper = NewImage("Soft parchment portrait backdrop", root,
                new Vector2(0, -230), new Vector2(1515, 675), new Color(.91f,.82f,.63f,.97f));
            NewImage("Inset border top", upper.transform,
                new Vector2(0, 0), new Vector2(1490, 650), new Color(.97f,.88f,.70f,1));

            leftPortrait = NewImage("Protagonist Portrait [left]", root,
                new Vector2(-490, -62), new Vector2(395, 460), Color.white);
            rightPortrait = NewImage("NPC Portrait [right]", root,
                new Vector2(490, -62), new Vector2(395, 460), Color.white);
            leftPortrait.preserveAspect = true;
            rightPortrait.preserveAspect = true;

            Image frame = NewImage("Dialogue gold border", root,
                new Vector2(0, -308), new Vector2(1505, 255), new Color(.61f,.44f,.21f,1));
            Image inner = NewImage("Dialogue blue grey parchment", frame.transform,
                Vector2.zero, new Vector2(1485, 233), new Color(.73f,.78f,.80f,1));
            nameLabel = NewText("Speaker name", inner.transform,
                new Vector2(-570, 85), new Vector2(290, 37), 26,
                new Color(.20f,.23f,.24f,1));
            bodyText = NewText("Dialogue text - typewriter", inner.transform,
                new Vector2(0, -12), new Vector2(1350, 147), 29,
                new Color(.15f,.18f,.20f,1));
            bodyText.lineSpacing = 1.18f;
            continueHint = NewText("Click to continue hint", inner.transform,
                new Vector2(560, -99), new Vector2(280, 30), 18,
                new Color(.32f,.37f,.38f,1), TextAnchor.MiddleRight);

            Button button = inner.gameObject.AddComponent<Button>();
            inner.raycastTarget = true;
            button.targetGraphic = inner;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(Advance);
            overlay.SetActive(false);
        }

        public bool Play(string eventId, Action afterDialogue = null, bool fromFinalMinigame = false)
        {
            if (running || overlay == null || script == null) return false;
            var sequence = script.Find(eventId);
            if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
            {
                Debug.LogWarning("Missing dialogue event: " + eventId);
                return false;
            }
            var session = GameSession2D.Instance;
            if (session == null || !session.BeginDialogue(fromFinalMinigame)) return false;
            pages.Clear();
            foreach (var line in sequence.lines)
            {
                if (line == null || string.IsNullOrEmpty(line.text)) continue;
                AddPaginatedLine(line.speaker, line.text);
            }
            if (pages.Count == 0)
            {
                session.EndDialogue();
                return false;
            }
            onFinished = afterDialogue;
            currentEvent = eventId;
            running = true;
            pageIndex = 0;
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            ShowPage();
            return true;
        }

        private void AddPaginatedLine(DialogueSpeaker2D speaker, string content)
        {
            int pos = 0;
            while (pos < content.Length)
            {
                int left = 1, right = content.Length - pos, best = 1;
                while (left <= right)
                {
                    int mid = left + (right - left) / 2;
                    if (Fits(content.Substring(pos, mid)))
                    {
                        best = mid;
                        left = mid + 1;
                    }
                    else right = mid - 1;
                }
                if (best < content.Length - pos)
                {
                    int boundary = -1;
                    int minLength = Mathf.Max(1, Mathf.FloorToInt(best * .65f));
                    for (int i = best - 1; i >= minLength - 1; i--)
                    {
                        char c = content[pos + i];
                        if ("。！？!?；;，,、\n".IndexOf(c) >= 0)
                        { boundary = i + 1; break; }
                    }
                    if (boundary > 0) best = boundary;
                }
                if (pos + best < content.Length && best > 1 &&
                    char.IsHighSurrogate(content[pos + best - 1]) &&
                    char.IsLowSurrogate(content[pos + best])) best--;
                pages.Add(new DialoguePage(speaker, content.Substring(pos, best)));
                pos += best;
            }
        }
        private bool Fits(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            var rect = bodyText.rectTransform.rect;
            var settings = bodyText.GetGenerationSettings(new Vector2(rect.width, 0));
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            float measured = bodyText.cachedTextGeneratorForLayout.GetPreferredHeight(text, settings)
                             / bodyText.pixelsPerUnit;
            return measured <= rect.height - 4f;
        }

        private void ShowPage()
        {
            if (typingAudio != null)
                typingAudio.Stop();
            if (typing != null) { StopCoroutine(typing); typing = null; }
            var page = pages[pageIndex];
            bool protagonist = page.speaker == DialogueSpeaker2D.Protagonist;
            bool thoughts = currentEvent == "Opening" || currentEvent.StartsWith("Final_", StringComparison.Ordinal);
            nameLabel.text = protagonist && thoughts ? "主角（內心）" : script.Name(page.speaker);
            leftPortrait.gameObject.SetActive(protagonist);
            rightPortrait.gameObject.SetActive(!protagonist);
            Image portrait = protagonist ? leftPortrait : rightPortrait;
            portrait.sprite = script.Portrait(page.speaker);
            portrait.enabled = portrait.sprite != null;
            bodyText.text = "";
            shownCharacters = 0;
            continueHint.text = "";
            typing = StartCoroutine(TypePage(page.text));
        }

        private IEnumerator TypePage(string text)
        {
            float cps = script != null
                ? Mathf.Max(5f, script.charactersPerSecond)
                : 28f;

            while (shownCharacters < text.Length)
            {
                int next = shownCharacters + 1;

                if (char.IsHighSurrogate(text[shownCharacters]) &&
                    next < text.Length &&
                    char.IsLowSurrogate(text[next]))
                {
                    next++;
                }

                shownCharacters = next;
                bodyText.text = text.Substring(0, shownCharacters);

                char c = text[shownCharacters - 1];

                if (typingAudio != null &&
                    script != null &&
                    script.typingSound != null &&
                    !char.IsWhiteSpace(c) &&
                    !char.IsPunctuation(c) &&
                    shownCharacters %
                        Mathf.Max(1, script.typingSoundEveryNCharacters) == 0)
                {
                    typingAudio.PlayOneShot(
                        script.typingSound,
                        script.typingSoundVolume
                    );
                }

                float delay = 1f / cps;

                if ("。！？!?".IndexOf(c) >= 0)
                {
                    delay += script != null
                        ? script.sentencePause : .22f;
                }
                else if ("，,、；;：:".IndexOf(c) >= 0)
                {
                    delay += script != null
                        ? script.commaPause : .10f;
                }

                yield return new WaitForSecondsRealtime(delay);
            }

            if (typingAudio != null)
                typingAudio.Stop();

            continueHint.text = "點擊對話框繼續 ▸";
            typing = null;
        }

        private void Advance()
        {
            if (!running || typing != null) return;  
            if (pageIndex + 1 < pages.Count) { pageIndex++; ShowPage(); }
            else Finish();
        }
        private void Finish()
        {
            if (!running) return;
            if (typingAudio != null)
                typingAudio.Stop();
            if (typing != null) { StopCoroutine(typing); typing = null; }
            running = false;
            overlay.SetActive(false);
            pages.Clear();
            var callback = onFinished;
            onFinished = null;
            GameSession2D.Instance?.EndDialogue();
            callback?.Invoke();
        }
        private void OnDestroy()
        {
            if (Active == this) Active = null;
            if (running) GameSession2D.Instance?.EndDialogue();
            if (runtimeFont != null && runtimeFont.name != "LegacyRuntime.ttf") Destroy(runtimeFont);
        }
    }
}
