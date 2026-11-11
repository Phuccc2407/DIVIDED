using UnityEngine;

public class PotionCookingStation : MonoBehaviour, IInteractable, IInteractablePrompt, IPotionDropTarget
{
    [SerializeField] private PotionPuzzleManager puzzleManager;
    [SerializeField, Range(0, 5)] private int heatStep = 1;
    [SerializeField] private ParticleSystem fireEffect;
    [SerializeField] private Light fireLight;
    [SerializeField] private float minLightIntensity = 0f;
    [SerializeField] private float maxLightIntensity = 2f;
    [SerializeField] private Renderer liquidRenderer;
    [SerializeField] private Color emptyLiquidColor = Color.clear;
    [SerializeField] private ParticleSystem successEffect;

    public string InteractionPrompt => "E: Tang nhiet\nQ: Giam nhiet";
    public string CarryPrompt => "E: Pour";

    public bool TryAccept(IPotionCarryable item)
    {
        if (item is PotionIngredientObject ingredient)
        {
            if (puzzleManager == null)
                return false;

            puzzleManager.AddIngredient(ingredient.Ingredient, ingredient.IngredientColor);
            return true;
        }

        return false;
    }

    private void Awake()
    {
        if (puzzleManager == null)
            puzzleManager = GetComponentInParent<PotionPuzzleManager>();

        if (liquidRenderer == null)
            Debug.LogError($"Cooking station {name} has no Liquid Renderer assigned.", this);
        if (successEffect == null)
            Debug.LogWarning($"Cooking station {name} has no Success Effect assigned.", this);

        ApplyHeatVisuals(0);
    }

    public void Interact()
    {
        if (puzzleManager == null)
        {
            Debug.LogError($"Cooking station {name} has no PotionPuzzleManager reference.", this);
            return;
        }

        puzzleManager.AdjustHeat(heatStep);
    }

    public void ReduceHeat()
    {
        if (puzzleManager == null)
        {
            Debug.LogError($"Cooking station {name} has no PotionPuzzleManager reference.", this);
            return;
        }

        puzzleManager.AdjustHeat(-heatStep);
    }

    public void StartCooking()
    {
        if (puzzleManager == null)
        {
            Debug.LogError($"Cooking station {name} has no PotionPuzzleManager reference.", this);
            return;
        }

        puzzleManager.StartCooking();
    }

    public void Highlight(bool state)
    {
    }

    public void SetHeatVisual(int heatLevel)
    {
        ApplyHeatVisuals(Mathf.Clamp(heatLevel, 0, 5));
    }

    public void ResetLiquid()
    {
        SetLiquidColor(emptyLiquidColor);
    }

    public void PlaySuccess()
    {
        if (successEffect != null)
        {
            successEffect.gameObject.SetActive(true);
            successEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            successEffect.Play();
        }
    }

    public void SetLiquidColor(Color color)
    {
        if (liquidRenderer == null)
        {
            Debug.LogError($"Cooking station {name} has no Liquid Renderer assigned.", this);
            return;
        }

        Material material = liquidRenderer.material;
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
    }

    private void ApplyHeatVisuals(int heatLevel)
    {
        float normalizedHeat = heatLevel / 5f;

        if (fireEffect != null)
        {
            var emission = fireEffect.emission;
            emission.rateOverTime = Mathf.Lerp(0f, 35f, normalizedHeat);

            var main = fireEffect.main;
            main.startSize = Mathf.Lerp(0.1f, 0.8f, normalizedHeat);
            fireEffect.gameObject.SetActive(heatLevel > 0);
        }

        if (fireLight != null)
        {
            fireLight.intensity = Mathf.Lerp(minLightIntensity, maxLightIntensity, normalizedHeat);
            fireLight.enabled = heatLevel > 0;
        }
    }
}

public interface IPotionCarryable
{
    Transform CarryTransform { get; }
    Quaternion CarryRotation { get; }
    void SetCarried(bool carried);
    void ReturnToWorld();
}

public interface IPotionDropTarget
{
    bool TryAccept(IPotionCarryable item);
    string CarryPrompt { get; }
}

public class PotionBaseBottle : MonoBehaviour, IInteractable, IPotionCarryable
{
    [SerializeField] private Transform carryTransform;
    [SerializeField] private Vector3 carryRotation;
    [SerializeField] private PotionIngredient ingredient;

    private Collider[] colliders;
    private Transform originalParent;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;

    public Transform CarryTransform => carryTransform != null ? carryTransform : transform;
    public Quaternion CarryRotation => Quaternion.Euler(carryRotation);
    public PotionIngredient Ingredient => ingredient;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>(true);
        originalParent = transform.parent;
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    public void Interact()
    {
    }

    public void SetCarried(bool carried)
    {
        if (colliders == null)
            colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider itemCollider in colliders)
            itemCollider.enabled = !carried;
    }

    public void ReturnToWorld()
    {
        transform.SetParent(originalParent, false);
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;
        SetCarried(false);
    }

    public void Highlight(bool state)
    {
    }
}
