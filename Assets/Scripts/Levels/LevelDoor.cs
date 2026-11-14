using UnityEngine;

public class LevelDoor : MonoBehaviour, IInteractable, IInteractableAvailability
{
    [SerializeField, Min(0)] private int levelIndex;
    [SerializeField] private LevelSelectionManager levelSelectionManager;

    public bool CanInteract => levelSelectionManager != null && !levelSelectionManager.IsLevelCompleted(levelIndex);

    public void Interact()
    {
        if (levelSelectionManager != null && !CanInteract) return;
        if (levelSelectionManager == null)
        {
            Debug.LogError($"Level door {name} has no LevelSelectionManager assigned.", this);
            return;
        }

        levelSelectionManager.RequestLevel(levelIndex);
    }

    public void Highlight(bool state)
    {
    }
}
