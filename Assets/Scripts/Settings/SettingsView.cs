using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class SettingsView : MonoBehaviour
{
    private SettingsManager settings;
    private GameObject panel;
    private Slider masterSlider;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Slider sensitivitySlider;
    private Toggle fullscreenToggle;
    private bool isOpen;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateView()
    {
        if (FindFirstObjectByType<SettingsView>() != null)
            return;

        var gameObject = new GameObject(nameof(SettingsView));
        gameObject.AddComponent<SettingsView>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        settings = SettingsManager.Instance;
        BuildUi();
        SetOpen(false);
    }

    private void Update()
    {
        if (!PlayerController.IsLocalPlayerSpawned)
        {
            if (isOpen)
                SetOpen(false);

            return;
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SetOpen(!isOpen);
    }

    private void LateUpdate()
    {
        if (PlayerController.IsLocalPlayerSpawned)
            ApplyCursorState();
    }

    private void OnDestroy()
    {
        if (settings != null)
            settings.SettingsChanged -= RefreshControls;
    }

    private void BuildUi()
    {
        var canvasObject = new GameObject("SettingsCanvas");
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        panel = CreatePanel(canvasObject.transform);
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(36, 36, 30, 24);
        layout.spacing = 12;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        CreateLabel(panel.transform, "SETTINGS");
        masterSlider = CreateSlider(panel.transform, "Master volume", 0f, 1f);
        musicSlider = CreateSlider(panel.transform, "Music volume", 0f, 1f);
        sfxSlider = CreateSlider(panel.transform, "SFX volume", 0f, 1f);
        sensitivitySlider = CreateSlider(panel.transform, "Mouse sensitivity", 0.05f, 4f);
        fullscreenToggle = CreateToggle(panel.transform, "Fullscreen");

        var resetButton = CreateButton(panel.transform, "Reset defaults");
        resetButton.onClick.AddListener(settings.ResetToDefaults);
        var closeButton = CreateButton(panel.transform, "Close (Esc)");
        closeButton.onClick.AddListener(() => SetOpen(false));

        masterSlider.onValueChanged.AddListener(settings.SetMasterVolume);
        musicSlider.onValueChanged.AddListener(settings.SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(settings.SetSfxVolume);
        sensitivitySlider.onValueChanged.AddListener(settings.SetMouseSensitivity);
        fullscreenToggle.onValueChanged.AddListener(settings.SetFullscreen);
        settings.SettingsChanged += RefreshControls;
        RefreshControls();
    }

    private void SetOpen(bool open)
    {
        if (open && !PlayerController.IsLocalPlayerSpawned)
            return;

        isOpen = open;
        if (panel != null)
            panel.SetActive(open);

        PlayerInputBlocker.SetBlocked(open);
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpen;
    }

    private void RefreshControls()
    {
        if (settings == null)
            return;

        masterSlider.SetValueWithoutNotify(settings.MasterVolume);
        musicSlider.SetValueWithoutNotify(settings.MusicVolume);
        sfxSlider.SetValueWithoutNotify(settings.SfxVolume);
        sensitivitySlider.SetValueWithoutNotify(settings.MouseSensitivity);
        fullscreenToggle.SetIsOnWithoutNotify(settings.Fullscreen);
    }

    private static GameObject CreatePanel(Transform parent)
    {
        var panel = new GameObject("Panel", typeof(Image));
        panel.transform.SetParent(parent, false);
        var image = panel.GetComponent<Image>();
        image.color = new Color(0.05f, 0.05f, 0.08f, 0.96f);

        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(420f, 440f);
        return panel;
    }

    private static void CreateLabel(Transform parent, string text)
    {
        var label = CreateText(parent, text, 24);
        label.alignment = TextAnchor.MiddleCenter;
    }

    private static Slider CreateSlider(Transform parent, string label, float min, float max)
    {
        CreateText(parent, label, 16);
        var sliderObject = new GameObject(label + " Slider", typeof(Slider));
        sliderObject.transform.SetParent(parent, false);
        var slider = sliderObject.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = min;
        sliderObject.AddComponent<LayoutElement>().preferredHeight = 24f;

        var background = CreateImage(sliderObject.transform, "Background", new Color(0.12f, 0.12f, 0.16f, 1f));
        Stretch(background.rectTransform);
        background.rectTransform.sizeDelta = new Vector2(0f, 8f);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());
        var fill = CreateImage(fillArea.transform, "Fill", new Color(0.25f, 0.65f, 1f, 1f));
        Stretch(fill.rectTransform);

        var handle = CreateImage(sliderObject.transform, "Handle", Color.white);
        handle.rectTransform.sizeDelta = new Vector2(18f, 18f);
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        return slider;
    }

    private static Toggle CreateToggle(Transform parent, string label)
    {
        var toggleObject = new GameObject(label, typeof(Toggle));
        toggleObject.transform.SetParent(parent, false);
        var toggle = toggleObject.GetComponent<Toggle>();
        toggleObject.AddComponent<LayoutElement>().preferredHeight = 28f;
        var background = CreateImage(toggleObject.transform, "Checkbox", new Color(0.12f, 0.12f, 0.16f, 1f));
        background.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        background.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        background.rectTransform.pivot = new Vector2(0f, 0.5f);
        background.rectTransform.sizeDelta = new Vector2(22f, 22f);
        var checkmark = CreateImage(background.transform, "Checkmark", new Color(0.25f, 0.65f, 1f, 1f));
        Stretch(checkmark.rectTransform, 4f);
        toggle.graphic = checkmark;
        toggle.targetGraphic = background;

        var text = CreateText(toggleObject.transform, label, 16);
        text.rectTransform.offsetMin = new Vector2(32f, 0f);
        text.rectTransform.offsetMax = new Vector2(0f, 0f);
        return toggle;
    }

    private static Button CreateButton(Transform parent, string label)
    {
        var buttonObject = new GameObject(label, typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.24f, 1f);
        buttonObject.AddComponent<LayoutElement>().preferredHeight = 34f;
        CreateText(buttonObject.transform, label, 16).alignment = TextAnchor.MiddleCenter;
        return buttonObject.GetComponent<Button>();
    }

    private static Text CreateText(Transform parent, string content, int fontSize)
    {
        var textObject = new GameObject(content + " Text", typeof(Text));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.color = Color.white;
        text.raycastTarget = false;
        textObject.AddComponent<LayoutElement>().preferredHeight = fontSize + 8f;
        return text;
    }

    private static Image CreateImage(Transform parent, string name, Color color)
    {
        var imageObject = new GameObject(name, typeof(Image));
        imageObject.transform.SetParent(parent, false);
        var image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static void Stretch(RectTransform rectTransform, float inset = 0f)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(inset, inset);
        rectTransform.offsetMax = new Vector2(-inset, -inset);
    }
}
