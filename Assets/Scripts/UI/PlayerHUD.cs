using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    private Slider staminaSlider;
    private TMP_Text carriedItemText;
    private TMP_Text carriedPromptText;

    public void Initialize(
        Slider staminaSlider,
        TMP_Text carriedItemText,
        TMP_Text carriedPromptText)
    {
        this.staminaSlider = staminaSlider;
        this.carriedItemText = carriedItemText;
        this.carriedPromptText = carriedPromptText;
    }

    public void SetStamina(float normalized)
    {
        if (staminaSlider != null)
            staminaSlider.value = Mathf.Clamp01(normalized);
    }

    public void SetCarriedItem(string itemName, string prompt = "")
    {
        if (carriedItemText != null)
            carriedItemText.text = string.IsNullOrEmpty(itemName) ? "Empty" : itemName;

        if (carriedPromptText != null)
            carriedPromptText.text = prompt;
    }

}
