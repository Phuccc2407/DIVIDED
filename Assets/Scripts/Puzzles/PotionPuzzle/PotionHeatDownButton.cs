using UnityEngine;

// Kept for compatibility with Unity-generated project files. Do not add this component to the scene.
public class PotionHeatDownButton : MonoBehaviour, IInteractable
{
    [SerializeField] private PotionCookingStation cookingStation;

    public void Interact()
    {
        if (cookingStation == null)
        {
            Debug.LogError($"Heat down button {name} has no PotionCookingStation reference.", this);
            return;
        }

        cookingStation.ReduceHeat();
    }

    public void Highlight(bool state)
    {
    }
}
