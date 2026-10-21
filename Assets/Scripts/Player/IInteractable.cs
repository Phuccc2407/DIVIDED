using UnityEngine;

public interface IInteractable
{
    void Interact();
    void Highlight(bool state);
}

public interface IInteractablePrompt
{
    string InteractionPrompt { get; }
}