using PurrNet;
using UnityEngine;

public class NoteItem : MonoBehaviour, IInteractable
{
    [SerializeField] private Texture noteTexture;

    public void Interact()
    {
        var view = InstanceHandler.GetInstance<NoteView>();
        var manager = InstanceHandler.GetInstance<GameViewManager>();

        view.SetImage(noteTexture);
        manager.ShowView<NoteView>(true);

        PlayerInputBlocker.SetBlocked(true);
        Debug.Log("Show NoteView");
    }

    public void Highlight(bool state)
    {
        // optional
    }
}

public static class PlayerInputBlocker
{
    public static bool IsBlocked { get; private set; }

    public static void SetBlocked(bool state)
    {
        IsBlocked = state;
    }
}