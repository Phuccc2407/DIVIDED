using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TheatreButton : MonoBehaviour, IInteractable, IInteractablePrompt, IInteractableAvailability
{
    [Tooltip("0 = room A; 1 = room B")]
    public int roomIndex;
    [Tooltip("-1 = start; 0..3 = answer position")]
    public int answerIndex = -1;
    public TheatrePuzzleManager puzzle;
    [SerializeField] private TMP_Text symbolDisplay;
    [Header("Interaction highlight")]
    [SerializeField] private Color outlineColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField, Range(0f, 10f)] private float outlineWidth = 4f;
    public TheatreMotion lift = new TheatreMotion();
    private bool available;
    private int token;
    private readonly List<Outline> outlines = new List<Outline>();
    private Collider[] interactionColliders;

    public bool CanInteract => available && puzzle != null && isActiveAndEnabled;
    public string InteractionPrompt => !CanInteract ? string.Empty : answerIndex < 0 ? "E: Start performance" : "E: Choose symbol";
    public bool IsValid => lift.IsValid && (answerIndex < 0 || symbolDisplay != null);

    private void Awake()
    {
        interactionColliders = GetComponentsInChildren<Collider>(true);
        int layer = LayerMask.NameToLayer("Interactable");
        if (layer >= 0)
        {
            gameObject.layer = layer;
            foreach (var collider in interactionColliders)
                collider.gameObject.layer = layer;
        }

        // Put the outline on model meshes, excluding the TMP symbol above the button.
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;
            if (renderer.GetComponent<TMP_Text>() != null) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            if (renderer is MeshRenderer && (filter == null || filter.sharedMesh == null)) continue;
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh == null) continue;
            var outline = renderer.GetComponent<Outline>();
            if (outline == null) outline = renderer.gameObject.AddComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineVisible;
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
            outline.enabled = false;
            outlines.Add(outline);
        }
    }

    public void Configure(bool enabled, string symbol, int roundToken)
    {
        available = enabled;
        token = roundToken;
        if (symbolDisplay != null) symbolDisplay.text = symbol;
        if (!enabled) Highlight(false);
    }

    public void Interact()
    {
        if (CanInteract) puzzle.Press(roomIndex, answerIndex, token);
    }

    public bool IsWithinReach(Vector3 origin, float reach)
    {
        if (interactionColliders == null) interactionColliders = GetComponentsInChildren<Collider>(true);
        foreach (var collider in interactionColliders)
            if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy
                && (collider.ClosestPoint(origin) - origin).sqrMagnitude <= reach * reach) return true;
        return false;
    }

    public void Highlight(bool state)
    {
        foreach (var outline in outlines)
            if (outline != null) outline.enabled = state && CanInteract;
    }

    private void OnDisable() => Highlight(false);
}
