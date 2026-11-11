using UnityEngine;

public class PotionIngredientObject : MonoBehaviour, IInteractable, IPotionCarryable
{
    [SerializeField] private PotionPuzzleManager puzzleManager;
    [SerializeField] private PotionIngredient ingredient;
    [SerializeField] private Color ingredientColor = Color.white;
    [SerializeField] private Transform carryTransform;
    [SerializeField] private Vector3 carryRotation;
    [SerializeField] private Rigidbody rigidBody;

    private Collider[] colliders;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    public PotionIngredient Ingredient => ingredient;
    public Color IngredientColor => ingredientColor;
    public Transform CarryTransform => carryTransform != null ? carryTransform : transform;
    public Quaternion CarryRotation => Quaternion.Euler(carryRotation);

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>(true);
        if (rigidBody == null)
            rigidBody = GetComponent<Rigidbody>();
        if (rigidBody == null)
            rigidBody = gameObject.AddComponent<Rigidbody>();

        if (ingredientColor == Color.white && ingredient != PotionIngredient.WhiteHerb)
            ingredientColor = GetDefaultColor(ingredient);

        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    public void Interact()
    {
        if (puzzleManager == null)
        {
            Debug.LogError($"Ingredient {name} has no PotionPuzzleManager reference.", this);
            return;
        }

        puzzleManager.AddIngredient(ingredient, ingredientColor);
    }

    public void Highlight(bool state)
    {
    }

    public void SetCarried(bool carried)
    {
        if (colliders == null)
            colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider itemCollider in colliders)
            itemCollider.enabled = !carried;

        if (rigidBody != null)
        {
            rigidBody.isKinematic = carried;
            rigidBody.useGravity = !carried;
        }
    }

    public void ReturnToWorld()
    {
        transform.SetParent(originalParent, false);
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        SetCarried(false);
    }

    private static Color GetDefaultColor(PotionIngredient value)
    {
        return value switch
        {
            PotionIngredient.RedHerb => Color.red,
            PotionIngredient.BlueHerb => Color.blue,
            PotionIngredient.YellowHerb => Color.yellow,
            PotionIngredient.PurpleHerb => new Color(0.65f, 0.1f, 0.9f),
            _ => Color.white
        };
    }
}
