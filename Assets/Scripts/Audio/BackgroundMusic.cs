using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public sealed class BackgroundMusic : MonoBehaviour
{
    [Serializable]
    private struct LevelMusic
    {
        [Min(0)] public int levelIndex;
        public AudioClip clip;
    }

    private static BackgroundMusic instance;

    [Header("Scene music")]
    [SerializeField] private AudioClip lobbyMusic;
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private List<LevelMusic> levelMusic = new List<LevelMusic>();
    [SerializeField] private string lobbySceneName = "LobbySample";

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
        SceneManager.sceneLoaded += OnSceneLoaded;
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

        PlaySceneMusic(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (settings != null)
            settings.SettingsChanged -= RefreshVolume;

        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (instance == this)
            instance = null;
    }

    public static void PlayLevelMusic(int levelIndex)
    {
        if (instance == null)
        {
            Debug.LogWarning("Cannot play level music because BackgroundMusic is not present.");
            return;
        }

        foreach (var entry in instance.levelMusic)
        {
            if (entry.levelIndex != levelIndex)
                continue;

            instance.SetClip(entry.clip);
            return;
        }

        Debug.LogWarning($"No background music is configured for level {levelIndex}.", instance);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlaySceneMusic(scene.name);
    }

    private void PlaySceneMusic(string sceneName)
    {
        if (sceneName == lobbySceneName || sceneName == "NetworkedScene")
            SetClip(lobbyMusic);
        else
            SetClip(gameMusic);
    }

    private void SetClip(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning($"No background music clip is configured for scene {SceneManager.GetActiveScene().name}.", this);
            return;
        }

        if (audioSource.clip == clip && audioSource.isPlaying)
            return;

        audioSource.Stop();
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void RefreshVolume()
    {
        audioSource.volume = settings != null ? settings.MusicVolume : 1f;
    }
}
