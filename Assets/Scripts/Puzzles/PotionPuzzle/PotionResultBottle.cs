using UnityEngine;

public class PotionResultBottle : MonoBehaviour, IInteractable
{
    [SerializeField] private PotionPuzzleManager puzzleManager;
    [SerializeField, Range(0, PotionRecipes.TotalPotions - 1)] private int bottleId;
    [SerializeField] private Renderer liquidRenderer;
    [SerializeField] private Color filledColor = Color.magenta;

    public void Interact()
    {
        if (puzzleManager == null)
        {
            Debug.LogError($"Result bottle {name} has no PotionPuzzleManager reference.", this);
            return;
        }

        puzzleManager.PourIntoBottle(bottleId);
    }

    public void SetFilledVisual(bool filled)
    {
        if (liquidRenderer == null)
            return;

        if (filled)
            liquidRenderer.material.color = filledColor;
    }

    public void Highlight(bool state)
    {
    }
}
