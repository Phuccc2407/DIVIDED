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

    [Header("Shared music configuration")]
    [Tooltip("Shared playlist for both scenes. Edit its Level Music entries (puzzle 3 = index 2, puzzle 4 = index 3).")]
    [SerializeField] private BackgroundMusicPlaylist playlist;
    [SerializeField, HideInInspector] private AudioClip lobbyMusic;
    [SerializeField, HideInInspector] private AudioClip gameMusic;
    [SerializeField, HideInInspector] private List<LevelMusic> levelMusic = new List<LevelMusic>();
    [SerializeField] private string lobbySceneName = "LobbySample";

    private AudioSource audioSource;
    private SettingsManager settings;
    private int requestedLevelIndex = -1;
    private readonly HashSet<object> pauseOwners = new HashSet<object>();
    private static readonly object endingPauseOwner = new object();

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
        audioSource.spatialBlend = 0f;
        audioSource.dopplerLevel = 0f;
        settings = SettingsManager.Instance;
        RefreshVolume();
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

        if (requestedLevelIndex < 0) PlaySceneMusic(SceneManager.GetActiveScene().name);
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

        instance.requestedLevelIndex = levelIndex;
        if (instance.playlist != null)
        {
            if (instance.playlist.TryGetLevelClip(levelIndex, out var clip))
            {
                instance.SetClip(clip);
                return;
            }
        }
        else
        {
            foreach (var entry in instance.levelMusic)
            {
                if (entry.levelIndex != levelIndex) continue;
                instance.SetClip(entry.clip);
                return;
            }
        }

        Debug.LogWarning($"No background music is configured for level {levelIndex}.", instance);
    }

    public static void PauseForEnding()
    {
        SetPaused(endingPauseOwner, true);
    }

    // Independent owners prevent a puzzle from accidentally resuming ending music.
    public static void SetPaused(object owner, bool paused)
    {
        if (instance == null || instance.audioSource == null || owner == null) return;
        int before = instance.pauseOwners.Count;
        if (paused) instance.pauseOwners.Add(owner);
        else instance.pauseOwners.Remove(owner);
        if (before == 0 && instance.pauseOwners.Count > 0) instance.audioSource.Pause();
        else if (before > 0 && instance.pauseOwners.Count == 0) instance.audioSource.UnPause();
    }

    public static void PlayHubMusic()
    {
        if (instance == null) return;
        instance.requestedLevelIndex = -1;
        instance.SetClip(instance.playlist != null ? instance.playlist.gameMusic : instance.gameMusic);
    }

    public static void ResumeAfterEnding()
    {
        SetPaused(endingPauseOwner, false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        requestedLevelIndex = -1;
        PlaySceneMusic(scene.name);
    }

    private void PlaySceneMusic(string sceneName)
    {
        if (sceneName == lobbySceneName || sceneName == "NetworkedScene")
            SetClip(playlist != null ? playlist.lobbyMusic : lobbyMusic);
        else
            SetClip(playlist != null ? playlist.gameMusic : gameMusic);
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
        if (pauseOwners.Count > 0) audioSource.Pause();
    }

    private void RefreshVolume()
    {
        audioSource.volume = settings != null ? settings.MusicVolume : 1f;
    }
}
