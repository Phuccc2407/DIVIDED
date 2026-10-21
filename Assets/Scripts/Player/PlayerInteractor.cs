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
    PlayerCarryController carryController;

    protected override void OnSpawned()
    {
        if (isOwner)
        {
            interactTextView = InstanceHandler.GetInstance<InteractTextView>();
            gameViewManager = InstanceHandler.GetInstance<GameViewManager>();
            carryController = GetComponent<PlayerCarryController>();
        }
    }

    public class PlayerCarryController : MonoBehaviour
    {
        [SerializeField] private Transform carryPoint;

        private IPotionCarryable carriedItem;
        private Transform carriedTransform;

        public bool IsCarrying => carriedItem != null;

        public bool TryPickUp(IPotionCarryable item)
        {
            if (item == null || IsCarrying)
                return false;

            if (carryPoint == null)
            {
                Debug.LogError($"Player {name} has no carry point.", this);
                return false;
            }

            carriedItem = item;
            carriedTransform = item.CarryTransform;
            carriedTransform.SetParent(carryPoint, false);
            carriedTransform.localPosition = Vector3.zero;
            carriedTransform.localRotation = Quaternion.identity;
            item.SetCarried(true);
            return true;
        }

        public void Drop()
        {
            if (!IsCarrying)
                return;

            carriedTransform.SetParent(null, true);
            carriedTransform.position = carryPoint.position + carryPoint.forward * 0.75f;
            carriedItem.SetCarried(false);
            carriedItem = null;
            carriedTransform = null;
        }
    }

    void Update()
    {
        if (!isOwner || PlayerInputBlocker.IsBlocked)
            return;

        if (carryController == null)
            carryController = GetComponent<PlayerCarryController>();

        CheckInteractable();

        if (Keyboard.current.qKey.wasPressedThisFrame && carryController != null && carryController.IsCarrying)
        {
            carryController.Drop();
        }
        else if (Keyboard.current.qKey.wasPressedThisFrame
            && current is PotionCookingStation cookingStation)
        {
            cookingStation.ReduceHeat();
        }
        else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (current is IPotionCarryable carryable && carryController != null)
                carryController.TryPickUp(carryable);
            else
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
                    interactTextView.SetText(GetInteractionPrompt(interactable));
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

    private static string GetInteractionPrompt(IInteractable interactable)
    {
        if (interactable is IInteractablePrompt prompt)
            return prompt.InteractionPrompt;

        return "E: Interact";
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