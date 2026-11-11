using PurrNet;
using TMPro;
using UnityEngine;

public class MainGameView : View
{
    [Header("Player HUD References")]
    [SerializeField] private UnityEngine.UI.Slider staminaSlider;
    [SerializeField] private TMP_Text carriedItemText;
    [SerializeField] private TMP_Text carriedPromptText;

    public PlayerHUD PlayerHUD { get; private set; }

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        PlayerHUD = gameObject.AddComponent<PlayerHUD>();
        PlayerHUD.Initialize(staminaSlider, carriedItemText, carriedPromptText);
    }

    void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<MainGameView>();
    }

    public override void OnHide()
    {

    }

    public override void OnShow()
    {

    }
}
