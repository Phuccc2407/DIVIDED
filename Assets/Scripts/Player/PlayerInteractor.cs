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
    string currentPrompt;
    PlayerCarryController carryController;
    public Vector3 InteractionOrigin => cam != null ? cam.transform.position : transform.position;

    protected override void OnSpawned()
    {
        if (isOwner)
        {
            interactTextView = InstanceHandler.GetInstance<InteractTextView>();
            gameViewManager = InstanceHandler.GetInstance<GameViewManager>();
            carryController = GetComponent<PlayerCarryController>();
            if (carryController == null)
                carryController = gameObject.AddComponent<PlayerCarryController>();
            carryController.Initialize(cam != null ? cam.transform : transform);
            UpdateCarriedHud();
        }
    }

    public class PlayerCarryController : MonoBehaviour
    {
        [SerializeField] private Transform carryPoint;

        private IPotionCarryable carriedItem;
        private Transform carriedTransform;

        public bool IsCarrying => carriedItem != null;
        public IPotionCarryable CarriedItem => carriedItem;

        public void Initialize(Transform cameraTransform)
        {
            if (carryPoint != null)
                return;

            var carryPointObject = new GameObject("CarryPoint");
            carryPoint = carryPointObject.transform;
            carryPoint.SetParent(cameraTransform, false);
            carryPoint.localPosition = new Vector3(0.35f, -0.3f, 0.8f);
            carryPoint.localRotation = Quaternion.identity;
        }

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
            carriedTransform.localRotation = item.CarryRotation;
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

        public bool TryDeposit(IPotionDropTarget target)
        {
            if (!IsCarrying || target == null || !target.TryAccept(carriedItem))
                return false;

            carriedItem.ReturnToWorld();
            carriedItem = null;
            carriedTransform = null;
            return true;
        }
    }

    void LateUpdate()
    {
        if (!isOwner)
            return;
        if (PlayerInputBlocker.IsBlocked)
        {
            ClearCurrent();
            return;
        }

        if (carryController == null)
            carryController = GetComponent<PlayerCarryController>();

        // Buttons move through Transform, so queries must see their current collider poses.
        Physics.SyncTransforms();
        CheckInteractable();

        if (Keyboard.current == null) return;

        if (Keyboard.current.qKey.wasPressedThisFrame && carryController != null && carryController.IsCarrying)
        {
            carryController.Drop();
            UpdateCarriedHud();
        }
        else if (Keyboard.current.qKey.wasPressedThisFrame
            && current is PotionCookingStation cookingStation)
        {
            cookingStation.ReduceHeat();
        }
        else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (carryController != null && carryController.IsCarrying
                && current is IPotionDropTarget dropTarget)
            {
                if (carryController.TryDeposit(dropTarget))
                    UpdateCarriedHud();
            }
            else if (current is IPotionCarryable carryable && carryController != null)
            {
                if (carryController.TryPickUp(carryable))
                    UpdateCarriedHud();
            }
            else
                current?.Interact();
        }
    }

    void CheckInteractable()
    {
        if (cam != null && Physics.Raycast(cam.transform.position, cam.transform.forward, out RaycastHit hit, distance, interactableLayer))
        {
            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                if (interactable is IInteractableAvailability availability && !availability.CanInteract)
                {
                    ClearCurrent();
                    return;
                }
                if (interactable != current)
                {
                    ClearCurrent();

                    current = interactable;

                    // Outline
                    if (interactable is TheatreButton button)
                        button.Highlight(true);
                    else if (hit.collider.TryGetComponent(out Outline outline))
                    {
                        currentOutline = outline;
                        currentOutline.enabled = true;
                    }
                }
                // Symbols and button availability can change without moving the crosshair.
                string interactionPrompt = GetInteractionPrompt(interactable);
                if (interactionPrompt != currentPrompt)
                {
                    currentPrompt = interactionPrompt;
                    if (interactTextView != null) interactTextView.SetText(interactionPrompt);
                    if (gameViewManager != null) gameViewManager.ShowView<InteractTextView>(false);
                    UpdateCarriedHud(interactionPrompt);
                }
                return;
            }
        }

        ClearCurrent();
    }

    private void UpdateCarriedHud(string contextualPrompt = null)
    {
        if (!InstanceHandler.TryGetInstance(out MainGameView mainGameView))
            return;

        string itemName = string.Empty;
        if (carryController != null && carryController.IsCarrying)
            itemName = carryController.CarriedItem switch
            {
                PotionIngredientObject ingredient => ingredient.Ingredient.ToString(),
                _ => "Item"
            };

        string prompt = itemName.Length == 0
            ? string.Empty
            : string.IsNullOrEmpty(contextualPrompt) ? "Q: Drop" : contextualPrompt;
        mainGameView.PlayerHUD.SetCarriedItem(itemName, prompt);
    }

    private string GetInteractionPrompt(IInteractable interactable)
    {
        if (carryController != null && carryController.IsCarrying
            && interactable is IPotionDropTarget)
            return ((IPotionDropTarget)interactable).CarryPrompt;

        if (interactable is IInteractablePrompt prompt)
            return prompt.InteractionPrompt;

        return "E: Interact";
    }

    void ClearCurrent()
    {
        if (current != null)
        {
            if (current is TheatreButton button && button != null) button.Highlight(false);
            if (gameViewManager != null) gameViewManager.HideView<InteractTextView>();
            current = null;
            currentPrompt = null;
            UpdateCarriedHud();
        }

        if (currentOutline != null)
        {
            currentOutline.enabled = false;
            currentOutline = null;
        }
    }
}
