using UnityEngine;

public class PotionCookingStartButton : MonoBehaviour, IInteractable
{
    [SerializeField] private PotionPuzzleManager puzzleManager;

    public void Interact()
    {
        if (puzzleManager == null)
        {
            Debug.LogError($"Cooking start button {name} has no PotionPuzzleManager reference.", this);
            return;
        }

        puzzleManager.StartCooking();
    }

    public void Highlight(bool state)
    {
    }
}
