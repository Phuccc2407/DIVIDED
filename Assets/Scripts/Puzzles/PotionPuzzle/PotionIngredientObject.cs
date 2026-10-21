using UnityEngine;

public class PotionIngredientObject : MonoBehaviour, IInteractable
{
    [SerializeField] private PotionPuzzleManager puzzleManager;
    [SerializeField] private PotionIngredient ingredient;

    public void Interact()
    {
        if (puzzleManager == null)
        {
            Debug.LogError($"Ingredient {name} has no PotionPuzzleManager reference.", this);
            return;
        }

        puzzleManager.AddIngredient(ingredient);
    }

    public void Highlight(bool state)
    {
    }
}
