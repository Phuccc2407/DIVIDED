using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class TheatreRoom : MonoBehaviour
{
    public TheatreMotion leftCurtain = new TheatreMotion();
    public TheatreMotion rightCurtain = new TheatreMotion();
    public Light pointLight;
    public TheatreButton startButton;
    public TheatreButton[] answers = new TheatreButton[4];
    public TheatreActor mystery;
    public TheatreActor[] references = new TheatreActor[4];
    public TMP_Text statusDisplay;
    [Header("Status display typography")]
    [Tooltip("TMP Font Asset for the countdown and instructions. Leave empty to keep the assigned font.")]
    [SerializeField] private TMP_FontAsset statusFont;
    [SerializeField, Min(1f)] private float statusFontSize = 36f;
    private bool statusStyleDirty;
    [Header("Audio")]
    public AudioSource effects;
    public AudioSource music;
    [FormerlySerializedAs("roundMusic")]
    public AudioClip tensionMusic;
    [SerializeField, Range(0f, 1f)] private float tensionMusicVolume = 0.65f;
    public AudioClip openSound, closeSound, liftSound, pressSound, correctSound,
        wrongSound, timeoutSound, swapSound, completedSound;

    private void Awake()
    {
        ApplyStatusStyle();
        if (effects == null)
        {
            var source = new GameObject("Theatre SFX");
            source.transform.SetParent(transform, false);
            source.transform.position = statusDisplay != null ? statusDisplay.transform.position
                : pointLight != null ? pointLight.transform.position : transform.position;
            effects = source.AddComponent<AudioSource>();
            effects.playOnAwake = false;
            effects.loop = false;
            effects.spatialBlend = 1f;
            effects.dopplerLevel = 0f;
            effects.minDistance = 3f;
            effects.maxDistance = 35f;
            effects.rolloffMode = AudioRolloffMode.Linear;
        }
        if (music == null || music == effects)
        {
            var source = new GameObject("Theatre Tension Music");
            source.transform.SetParent(transform, false);
            source.transform.position = effects.transform.position;
            music = source.AddComponent<AudioSource>();
            music.spatialBlend = 1f;
            music.dopplerLevel = 0f;
            music.minDistance = 3f;
            music.maxDistance = 35f;
            music.rolloffMode = AudioRolloffMode.Linear;
        }
        music.playOnAwake = false;
        music.loop = true;
        music.Stop();
        UpdateMusicVolume();
    }
    private void OnValidate() => statusStyleDirty = true;

    private void LateUpdate()
    {
        if (statusStyleDirty) ApplyStatusStyle();
        UpdateMusicVolume();
    }

    private void UpdateMusicVolume()
    {
        if (music != null) music.volume = tensionMusicVolume
            * (SettingsManager.Instance != null ? SettingsManager.Instance.MusicVolume : 1f);
    }

    private void OnDisable() => StopMusic();

    private void ApplyStatusStyle()
    {
        statusStyleDirty = false;
        if (statusDisplay == null) return;
        if (statusFont != null && statusDisplay.font != statusFont)
        {
            statusDisplay.font = statusFont;
            statusDisplay.fontSharedMaterial = statusFont.material;
        }
        statusDisplay.enableAutoSizing = false;
        statusDisplay.fontSize = Mathf.Max(1f, statusFontSize);
        statusDisplay.ForceMeshUpdate();
    }

    public bool IsValid
    {
        get
        {
            if (!leftCurtain.IsValid || !rightCurtain.IsValid || pointLight == null
                || pointLight.type != LightType.Point || startButton == null || !startButton.IsValid
                || mystery == null || !mystery.IsValid || answers.Length != 4 || references.Length != 4) return false;
            for (int i = 0; i < 4; i++)
                if (answers[i] == null || !answers[i].IsValid || references[i] == null || !references[i].IsValid) return false;
            return true;
        }
    }

    public void Sound(AudioClip clip)
    {
        if (effects == null || clip == null) return;
        float volume = SettingsManager.Instance != null
            ? SettingsManager.Instance.GetVolume(SettingsVolumeChannel.Sfx) : 1f;
        effects.PlayOneShot(clip, volume);
    }

    public void StopMusic()
    {
        if (music != null) music.Stop();
    }

    public void StartMusic(float elapsed = 0f)
    {
        if (music == null || tensionMusic == null) return;
        music.clip = tensionMusic;
        music.loop = true;
        if (tensionMusic.length > 0f) music.time = Mathf.Max(0f, elapsed) % tensionMusic.length;
        UpdateMusicVolume();
        music.Play();
    }
}
