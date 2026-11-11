
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class SettingsView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button closeButton;

    [Header("Animation")]
    [SerializeField] private float fadeDuration = 0.25f;

    private SettingsManager settings;
    private Tween currentTween;
    private bool isOpen;

    private void Awake()
    {
        settings = SettingsManager.Instance;

        if (settings == null)
        {
            Debug.LogError("SettingsView: SettingsManager instance not found!");
            enabled = false;
            return;
        }

        if (canvasGroup == null ||
            masterSlider == null ||
            musicSlider == null ||
            sfxSlider == null ||
            sensitivitySlider == null ||
            fullscreenToggle == null ||
            resetButton == null ||
            closeButton == null)
        {
            Debug.LogError(
                "SettingsView: Assign all UI references in the Inspector!"
            );

            enabled = false;
            return;
        }

        // Initial UI state.
        isOpen = false;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // Connect UI events.
        masterSlider.onValueChanged.AddListener(settings.SetMasterVolume);
        musicSlider.onValueChanged.AddListener(settings.SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(settings.SetSfxVolume);
        sensitivitySlider.onValueChanged.AddListener(
            settings.SetMouseSensitivity
        );
        fullscreenToggle.onValueChanged.AddListener(settings.SetFullscreen);

        resetButton.onClick.AddListener(settings.ResetToDefaults);
        closeButton.onClick.AddListener(CloseSettings);

        settings.SettingsChanged += RefreshControls;

        RefreshControls();
        ApplyCursorState();
    }

    private void Update()
    {
        if (!PlayerController.IsLocalPlayerSpawned)
        {
            if (isOpen)
                SetOpen(false);

            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SetOpen(!isOpen);
        }
    }

    private void LateUpdate()
    {
        ApplyCursorState();
    }

    public void ToggleSettings()
    {
        SetOpen(!isOpen);
    }

    public void CloseSettings()
    {
        SetOpen(false);
    }

    private void SetOpen(bool open)
    {
        if (open && !PlayerController.IsLocalPlayerSpawned)
            return;

        if (isOpen == open)
            return;

        isOpen = open;

        // Stop any ongoing fade before starting another.
        currentTween?.Kill();

        if (open)
        {
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            PlayerInputBlocker.SetBlocked(true);

            currentTween = canvasGroup
                .DOFade(1f, fadeDuration)
                .SetUpdate(true);
        }
        else
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            PlayerInputBlocker.SetBlocked(false);

            currentTween = canvasGroup
                .DOFade(0f, fadeDuration)
                .SetUpdate(true);
        }

        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        Cursor.lockState = isOpen
            ? CursorLockMode.None
            : CursorLockMode.Locked;

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

    private void OnDestroy()
    {
        currentTween?.Kill();

        if (settings != null)
        {
            settings.SettingsChanged -= RefreshControls;

            if (masterSlider != null)
                masterSlider.onValueChanged.RemoveListener(
                    settings.SetMasterVolume
                );

            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveListener(
                    settings.SetMusicVolume
                );

            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(
                    settings.SetSfxVolume
                );

            if (sensitivitySlider != null)
                sensitivitySlider.onValueChanged.RemoveListener(
                    settings.SetMouseSensitivity
                );

            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveListener(
                    settings.SetFullscreen
                );

            if (resetButton != null)
                resetButton.onClick.RemoveListener(
                    settings.ResetToDefaults
                );

            if (closeButton != null)
                closeButton.onClick.RemoveListener(CloseSettings);
        }

        PlayerInputBlocker.SetBlocked(false);

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
