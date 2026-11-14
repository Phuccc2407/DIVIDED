using TMPro;
using UnityEngine;

// Keep this component on the fixed panel root; only hide its visual/collider.
public class GlassBridgePanel : MonoBehaviour
{
    [SerializeField] private Collider surface;
    [SerializeField] private GameObject visual;
    [SerializeField] private TMP_Text answerLabel;
    [SerializeField] private ParticleSystem breakEffect;
    private bool broken;
    [Header("Glass break SFX (optional)")]
    [SerializeField] private AudioClip[] breakSounds;
    [SerializeField, Range(0f, 1f)] private float breakVolume = 0.8f;
    private AudioSource breakSource;

    public bool IsUnder(Vector3 foot)
    {
        return !broken && surface != null && surface.enabled
            && surface.Raycast(new Ray(foot + Vector3.up * 0.18f, Vector3.down), out _, 0.4f);
    }

    public void ShowAnswer(string answer)
    {
        if (answerLabel != null) answerLabel.text = answer;
    }

    public void SetBroken(bool value, bool playSound = true)
    {
        if (value && !broken && playSound && breakSounds != null && breakSounds.Length > 0)
        {
            var clip = breakSounds[Random.Range(0, breakSounds.Length)];
            if (clip != null)
            {
                if (breakSource == null)
                {
                    var emitter = new GameObject("Glass break SFX");
                    emitter.transform.SetParent(transform, false);
                    breakSource = emitter.AddComponent<AudioSource>();
                    breakSource.playOnAwake = false;
                    breakSource.spatialBlend = 1f;
                    breakSource.minDistance = 3f;
                    breakSource.maxDistance = 35f;
                    breakSource.rolloffMode = AudioRolloffMode.Linear;
                    breakSource.dopplerLevel = 0f;
                }
                if (surface != null) breakSource.transform.position = surface.bounds.center;
                breakSource.PlayOneShot(clip, breakVolume * (SettingsManager.Instance != null ? SettingsManager.Instance.SfxVolume : 1f));
            }
        }
        if (value && !broken && breakEffect != null) breakEffect.Play();
        if (!value && breakEffect != null) breakEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        broken = value;
        if (surface != null) surface.enabled = !value;
        if (visual != null) visual.SetActive(!value);
        if (answerLabel != null) answerLabel.gameObject.SetActive(!value);
    }
}
