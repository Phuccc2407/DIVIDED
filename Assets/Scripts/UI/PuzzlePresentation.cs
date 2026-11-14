using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-300)]
public class PuzzlePresentation : MonoBehaviour
{
    [Serializable]
    private class TextStyle
    {
        public TMP_FontAsset font;
        [Min(1f)] public float size;
        public TextStyle(float size) { this.size = size; }
    }

    [Header("Fonts and sizes")]
    [Tooltip("Shared font. Leave empty to keep the existing TMP font. Each style can override this.")]
    [SerializeField] private TMP_FontAsset defaultFont;
    [SerializeField] private TextStyle puzzleNumberStyle = new TextStyle(34f);
    [SerializeField] private TextStyle puzzleNameStyle = new TextStyle(60f);
    [SerializeField] private TextStyle endingTitleStyle = new TextStyle(68f);
    [SerializeField] private TextStyle endingMessageStyle = new TextStyle(30f);
    [SerializeField] private TextStyle creditsStyle = new TextStyle(32f);
    [SerializeField] private TextStyle buttonStyle = new TextStyle(28f);
    [Header("Puzzle banner (optional; generated automatically if empty)")]
    [SerializeField] private CanvasGroup banner;
    [SerializeField] private TMP_Text chapterLabel;
    [SerializeField] private TMP_Text chapterTitle;
    [SerializeField, Min(0.005f)] private float letterSeconds = 0.035f;
    [SerializeField, Min(0f)] private float holdSeconds = 1.8f;
    [SerializeField, Min(0.05f)] private float fadeSeconds = 0.7f;
    [SerializeField, Min(0f)] private float shakeAmount = 1.2f;
    [Header("Ending and credits")]
    [SerializeField] private CanvasGroup ending;
    [SerializeField] private TMP_Text endingTitle;
    [SerializeField] private TMP_Text endingSubtitle;
    [SerializeField] private RectTransform creditsViewport;
    [SerializeField] private TMP_Text credits;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button returnButton;
    [SerializeField] private string endingHeading = "DIVIDED";
    [SerializeField] private string endingMessage = "TOGETHER AT LAST";
    [SerializeField, TextArea(8, 30)] private string creditsText = "DIVIDED\n\nA cooperative puzzle experience\n\nCREATED BY\n[Your name / team]\n\nPROGRAMMING & GAME DESIGN\n[Your credits]\n\nART & ANIMATION\n[Asset creators]\n\nMUSIC & SOUND\n[Music and sound credits]\n\nSPECIAL THANKS\nEveryone who helped us find the way\n\nTHANK YOU FOR PLAYING";
    [SerializeField, Min(10f)] private float creditsPixelsPerSecond = 65f;
    [Header("Audio")]
    [SerializeField] private AudioSource effects;
    [SerializeField] private AudioSource music;
    [SerializeField] private AudioClip celebrationClip;
    [SerializeField] private AudioClip creditMusic;
    private readonly Queue<Notice> notices = new Queue<Notice>();
    private Coroutine noticeRoutine, endingRoutine;
    private bool currentCompleted, animatingBanner, endingRequested, endingActive, skipRequested;
    private bool previousBlocked;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;
    private Action returnAction;
    private bool typographyDirty;

    private struct Notice { public int number; public string title; public bool completed; }

    private void Awake()
    {
        EnsureUI();
        ApplyTypography();
        if (effects == null) effects = gameObject.AddComponent<AudioSource>();
        if (music == null) music = gameObject.AddComponent<AudioSource>();
        effects.playOnAwake = music.playOnAwake = false;
        effects.spatialBlend = music.spatialBlend = 0f;
        music.loop = true;
        banner.alpha = ending.alpha = 0f;
        banner.blocksRaycasts = false;
        ending.blocksRaycasts = ending.interactable = false;
        skipButton.onClick.AddListener(SkipCredits);
        returnButton.onClick.AddListener(ReturnFromEnding);
    }

    public void SetReturnAction(Action action) => returnAction = action;

    private void OnValidate() => typographyDirty = true;

    private void ApplyTypography()
    {
        typographyDirty = false;
        ApplyStyle(chapterLabel, puzzleNumberStyle);
        ApplyStyle(chapterTitle, puzzleNameStyle);
        ApplyStyle(endingTitle, endingTitleStyle);
        ApplyStyle(endingSubtitle, endingMessageStyle);
        ApplyStyle(credits, creditsStyle);
        if (skipButton != null)
            foreach (var label in skipButton.GetComponentsInChildren<TMP_Text>(true)) ApplyStyle(label, buttonStyle);
        if (returnButton != null)
            foreach (var label in returnButton.GetComponentsInChildren<TMP_Text>(true)) ApplyStyle(label, buttonStyle);
        if (credits != null && credits.gameObject.activeInHierarchy)
            credits.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(100f, credits.preferredHeight));
    }

    private void ApplyStyle(TMP_Text text, TextStyle style)
    {
        if (text == null || style == null) return;
        TMP_FontAsset font = style.font != null ? style.font : defaultFont;
        if (font != null && text.font != font)
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
        }
        text.enableAutoSizing = false;
        text.fontSize = Mathf.Max(1f, style.size);
        text.ForceMeshUpdate();
    }

    public void ShowChapter(int number, string title, bool completed)
    {
        if (endingActive) return;
        // An obsolete entrance card should not delay a completed-puzzle announcement.
        if (noticeRoutine != null && !currentCompleted)
        {
            StopCoroutine(noticeRoutine);
            noticeRoutine = null;
            banner.alpha = 0f;
            animatingBanner = false;
        }
        if (!completed)
        {
            int pending = notices.Count;
            for (int i = 0; i < pending; i++)
            {
                Notice previous = notices.Dequeue();
                if (previous.completed) notices.Enqueue(previous);
            }
        }
        notices.Enqueue(new Notice { number = number, title = title, completed = completed });
        if (noticeRoutine == null) noticeRoutine = StartCoroutine(PlayNotices());
    }

    public void ShowEnding()
    {
        if (endingRequested || endingActive) return;
        endingRequested = true;
        if (noticeRoutine == null) BeginEnding();
    }

    private IEnumerator PlayNotices()
    {
        while (notices.Count > 0)
        {
            Notice notice = notices.Dequeue();
            currentCompleted = notice.completed;
            chapterLabel.text = $"PUZZLE {notice.number}" + (notice.completed ? " CLEARED" : "");
            chapterTitle.text = notice.title;
            chapterLabel.maxVisibleCharacters = chapterTitle.maxVisibleCharacters = 0;
            banner.alpha = 1f;
            animatingBanner = true;
            if (notice.completed && celebrationClip != null) effects.PlayOneShot(celebrationClip);
            yield return Reveal(chapterLabel);
            yield return Reveal(chapterTitle);
            float until = Time.unscaledTime + holdSeconds;
            while (Time.unscaledTime < until) yield return null;
            float elapsed = 0f;
            while (elapsed < fadeSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                banner.alpha = 1f - Mathf.Clamp01(elapsed / fadeSeconds);
                yield return null;
            }
            animatingBanner = false;
            banner.alpha = 0f;
        }
        noticeRoutine = null;
        if (endingRequested) BeginEnding();
    }

    private IEnumerator Reveal(TMP_Text text)
    {
        text.ForceMeshUpdate();
        int count = text.textInfo.characterCount;
        float elapsed = 0f;
        while (text.maxVisibleCharacters < count)
        {
            elapsed += Time.unscaledDeltaTime;
            text.maxVisibleCharacters = Mathf.Min(count, Mathf.FloorToInt(elapsed / letterSeconds));
            yield return null;
        }
    }

    private void Update()
    {
        if (typographyDirty) ApplyTypography();
        if (endingActive)
        {
            PlayerInputBlocker.SetBlocked(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        var settings = SettingsManager.Instance;
        if (effects != null) effects.volume = settings != null ? settings.GetVolume(SettingsVolumeChannel.Sfx) : 1f;
        if (music != null) music.volume = settings != null ? settings.MusicVolume : 1f;
    }

    private void LateUpdate()
    {
        if (!animatingBanner) return;
        Shake(chapterLabel);
        Shake(chapterTitle);
    }

    private void Shake(TMP_Text text)
    {
        text.ForceMeshUpdate();
        var info = text.textInfo;
        for (int i = 0; i < Mathf.Min(text.maxVisibleCharacters, info.characterCount); i++)
        {
            var c = info.characterInfo[i];
            if (!c.isVisible) continue;
            var vertices = info.meshInfo[c.materialReferenceIndex].vertices;
            float t = Time.unscaledTime * 15f;
            Vector3 offset = new Vector3(Mathf.PerlinNoise(i * 0.79f, t) - 0.5f,
                Mathf.PerlinNoise(i * 0.79f + 20f, t) - 0.5f, 0f) * shakeAmount * 2f;
            for (int v = 0; v < 4; v++) vertices[c.vertexIndex + v] += offset;
        }
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    private void BeginEnding()
    {
        if (endingActive) return;
        endingActive = true;
        previousBlocked = PlayerInputBlocker.IsBlocked;
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        PlayerInputBlocker.SetBlocked(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        BackgroundMusic.PauseForEnding();
        if (creditMusic != null) { music.clip = creditMusic; music.Play(); }
        endingRoutine = StartCoroutine(PlayEnding());
    }

    private IEnumerator PlayEnding()
    {
        skipRequested = false;
        endingTitle.text = endingHeading;
        endingSubtitle.text = endingMessage;
        credits.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);
        returnButton.gameObject.SetActive(false);
        ending.blocksRaycasts = ending.interactable = true;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime)
        { ending.alpha = Mathf.Clamp01(t); yield return null; }
        ending.alpha = 1f;
        yield return new WaitForSecondsRealtime(3f);
        endingTitle.text = "CREDITS";
        endingSubtitle.text = "";
        credits.text = creditsText;
        credits.gameObject.SetActive(true);
        credits.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        float height = Mathf.Max(100f, credits.preferredHeight);
        credits.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        float startY = -creditsViewport.rect.height;
        float y = startY;
        skipButton.gameObject.SetActive(true);
        while (!skipRequested && y < Mathf.Max(100f, credits.preferredHeight) + 40f)
        {
            y += Time.unscaledDeltaTime * creditsPixelsPerSecond;
            credits.rectTransform.anchoredPosition = new Vector2(0f, y);
            yield return null;
        }
        credits.gameObject.SetActive(false);
        skipButton.gameObject.SetActive(false);
        endingTitle.text = endingHeading;
        endingSubtitle.text = "THANK YOU FOR PLAYING";
        returnButton.gameObject.SetActive(true);
        endingRoutine = null;
    }

    private void SkipCredits() => skipRequested = true;
    private void ReturnFromEnding() => returnAction?.Invoke();

    public void HideEnding()
    {
        if (endingRoutine != null) StopCoroutine(endingRoutine);
        endingRoutine = null;
        endingRequested = false;
        ending.alpha = 0f;
        ending.blocksRaycasts = ending.interactable = false;
        if (music != null) music.Stop();
        if (endingActive)
        {
            PlayerInputBlocker.SetBlocked(previousBlocked);
            Cursor.lockState = previousCursorLock;
            Cursor.visible = previousCursorVisible;
            BackgroundMusic.ResumeAfterEnding();
        }
        endingActive = false;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        notices.Clear();
        noticeRoutine = null;
        animatingBanner = false;
        if (banner != null) banner.alpha = 0f;
        if (ending != null) HideEnding();
    }

    private void EnsureUI()
    {
        if (banner != null && chapterLabel != null && chapterTitle != null && ending != null
            && endingTitle != null && endingSubtitle != null && creditsViewport != null
            && credits != null && skipButton != null && returnButton != null) return;
        var root = new GameObject("Puzzle Presentation Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        banner = Panel("Puzzle Banner", root.transform, new Color(0f, 0f, 0f, 0.6f));
        SetRect((RectTransform)banner.transform, new Vector2(0f, 0.35f), new Vector2(1f, 0.65f));
        chapterLabel = Label("Chapter", banner.transform, 34f);
        SetRect(chapterLabel.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.88f));
        chapterTitle = Label("Title", banner.transform, 60f);
        SetRect(chapterTitle.rectTransform, new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.6f));
        ending = Panel("Ending", root.transform, Color.black);
        SetRect((RectTransform)ending.transform, Vector2.zero, Vector2.one);
        endingTitle = Label("Ending Title", ending.transform, 68f);
        SetRect(endingTitle.rectTransform, new Vector2(0.05f, 0.77f), new Vector2(0.95f, 0.93f));
        endingSubtitle = Label("Ending Subtitle", ending.transform, 30f);
        SetRect(endingSubtitle.rectTransform, new Vector2(0.1f, 0.63f), new Vector2(0.9f, 0.77f));
        var viewport = new GameObject("Credits Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(ending.transform, false);
        creditsViewport = (RectTransform)viewport.transform;
        SetRect(creditsViewport, new Vector2(0.12f, 0.17f), new Vector2(0.88f, 0.72f));
        credits = Label("Scrolling Credits", creditsViewport, 32f);
        credits.alignment = TextAlignmentOptions.Top;
        credits.rectTransform.anchorMin = new Vector2(0f, 1f);
        credits.rectTransform.anchorMax = new Vector2(1f, 1f);
        credits.rectTransform.pivot = new Vector2(0.5f, 1f);
        credits.rectTransform.sizeDelta = new Vector2(0f, 1500f);
        credits.textWrappingMode = TextWrappingModes.Normal;
        skipButton = MakeButton("Skip credits", ending.transform, new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.12f));
        returnButton = MakeButton("Return to hub", ending.transform, new Vector2(0.35f, 0.39f), new Vector2(0.65f, 0.48f));
    }

    private static CanvasGroup Panel(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go.GetComponent<CanvasGroup>();
    }

    private static TMP_Text Label(string name, Transform parent, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1f, 0.94f, 0.78f);
        text.raycastTarget = false;
        return text;
    }

    private static Button MakeButton(string label, Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.17f);
        go.GetComponent<Button>().targetGraphic = go.GetComponent<Image>();
        SetRect((RectTransform)go.transform, min, max);
        var text = Label("Label", go.transform, 28f);
        text.text = label;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one);
        return go.GetComponent<Button>();
    }

    private static void SetRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min; rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
