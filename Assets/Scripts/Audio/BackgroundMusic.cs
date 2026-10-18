using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public sealed class BackgroundMusic : MonoBehaviour
{
    private static BackgroundMusic instance;

    private AudioSource audioSource;
    private SettingsManager settings;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        settings = SettingsManager.Instance;
    }

    private void Start()
    {
        if (settings == null)
            settings = SettingsManager.Instance;

        if (settings != null)
        {
            settings.SettingsChanged += RefreshVolume;
            RefreshVolume();
        }

        if (audioSource.clip != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private void OnDestroy()
    {
        if (settings != null)
            settings.SettingsChanged -= RefreshVolume;

        if (instance == this)
            instance = null;
    }

    private void RefreshVolume()
    {
        audioSource.volume = settings != null ? settings.MusicVolume : 1f;
    }
}
