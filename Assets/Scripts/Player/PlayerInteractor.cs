using PurrNet;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : NetworkBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float distance = 3f;
    [SerializeField] private LayerMask interactableLayer;

    InteractTextView interactTextView;
    GameViewManager gameViewManager;

    IInteractable current;
    Outline currentOutline;

    protected override void OnSpawned()
    {
        if (isOwner)
        {
            interactTextView = InstanceHandler.GetInstance<InteractTextView>();
            gameViewManager = InstanceHandler.GetInstance<GameViewManager>();
        }
    }

    void Update()
    {
        if (!isOwner || PlayerInputBlocker.IsBlocked)
            return;

        CheckInteractable();

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            current?.Interact();
        }
    }

    void CheckInteractable()
    {
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, distance, interactableLayer))
        {
            if (hit.collider.TryGetComponent(out IInteractable interactable))
            {
                if (interactable != current)
                {
                    ClearCurrent();

                    current = interactable;

                    // UI
                    interactTextView.SetText("Interact");
                    gameViewManager.ShowView<InteractTextView>(false);

                    // Outline
                    if (hit.collider.TryGetComponent(out Outline outline))
                    {
                        currentOutline = outline;
                        currentOutline.enabled = true;
                    }
                }
                return;
            }
        }

        ClearCurrent();
    }

    void ClearCurrent()
    {
        if (current != null)
        {
            gameViewManager.HideView<InteractTextView>();
            current = null;
        }

        if (currentOutline != null)
        {
            currentOutline.enabled = false;
            currentOutline = null;
        }
    }
}