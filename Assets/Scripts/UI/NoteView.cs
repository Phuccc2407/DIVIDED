using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NoteView : View
{
    [SerializeField] private RawImage noteImage;
    bool justOpened;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<NoteView>();
    }

    void Update()
    {
        if (justOpened)
        {
            justOpened = false;
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Close();
        }
    }

    void Close()
    {
        var manager = InstanceHandler.GetInstance<GameViewManager>();
        manager.HideView<NoteView>();

        PlayerInputBlocker.SetBlocked(false);
    }

    public override void OnShow()
    {
        justOpened = true;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
    }

    public override void OnHide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    public void SetImage(Texture tex)
    {
        noteImage.texture = tex;
    }
}