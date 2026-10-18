using System;
using UnityEngine;

public enum SettingsVolumeChannel
{
    Music,
    Sfx
}

public sealed class SettingsManager : MonoBehaviour
{
    public const string MasterVolumeKey = "settings.master_volume";
    public const string MusicVolumeKey = "settings.music_volume";
    public const string SfxVolumeKey = "settings.sfx_volume";
    public const string MouseSensitivityKey = "settings.mouse_sensitivity";
    public const string FullscreenKey = "settings.fullscreen";

    public static SettingsManager Instance { get; private set; }

    public event Action SettingsChanged;

    public float MasterVolume { get; private set; }
    public float MusicVolume { get; private set; }
    public float SfxVolume { get; private set; }
    public float MouseSensitivity { get; private set; }
    public bool Fullscreen { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        if (Instance != null)
            return;

        var gameObject = new GameObject(nameof(SettingsManager));
        gameObject.AddComponent<SettingsManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        Load();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        AudioListener.volume = MasterVolume;
        SaveFloat(MasterVolumeKey, MasterVolume);
        NotifyChanged();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        SaveFloat(MusicVolumeKey, MusicVolume);
        NotifyChanged();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        SaveFloat(SfxVolumeKey, SfxVolume);
        NotifyChanged();
    }

    public void SetMouseSensitivity(float value)
    {
        MouseSensitivity = Mathf.Clamp(value, 0.05f, 1f);
        SaveFloat(MouseSensitivityKey, MouseSensitivity);
        NotifyChanged();
    }

    public void SetFullscreen(bool value)
    {
        Fullscreen = value;
        Screen.fullScreen = value;
        PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
        PlayerPrefs.Save();
        NotifyChanged();
    }

    public float GetVolume(SettingsVolumeChannel channel)
    {
        return channel == SettingsVolumeChannel.Music ? MusicVolume : SfxVolume;
    }

    public void ResetToDefaults()
    {
        SetMasterVolume(1f);
        SetMusicVolume(1f);
        SetSfxVolume(1f);
        SetMouseSensitivity(0.5f);
        SetFullscreen(Screen.fullScreen);
    }

    private void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        MusicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        SfxVolume = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
        MouseSensitivity = Mathf.Clamp(PlayerPrefs.GetFloat(MouseSensitivityKey, 0.5f), 0.05f, 4f);
        Fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        AudioListener.volume = MasterVolume;
        Screen.fullScreen = Fullscreen;
    }

    private static void SaveFloat(string key, float value)
    {
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();
    }

    private void NotifyChanged()
    {
        SettingsChanged?.Invoke();
    }
}
