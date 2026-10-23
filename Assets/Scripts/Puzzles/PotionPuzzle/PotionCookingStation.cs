using UnityEngine;

public class PotionCookingStation : MonoBehaviour, IInteractable, IInteractablePrompt
{
    [SerializeField] private PotionPuzzleManager puzzleManager;
    [SerializeField, Range(0, 5)] private int heatStep = 1;
    [SerializeField] private ParticleSystem fireEffect;
    [SerializeField] private Light fireLight;
    [SerializeField] private float minLightIntensity = 0f;
    [SerializeField] private float maxLightIntensity = 2f;

    public string InteractionPrompt => "Q: Heat Up\nE: Cool Down";

    private void Awake()
    {
        if (puzzleManager == null)
            puzzleManager = GetComponentInParent<PotionPuzzleManager>();

        ApplyHeatVisuals(0);
    }

    public void Interact()
    {
        if (puzzleManager == null)
        {
            Debug.LogError($"Cooking station {name} has no PotionPuzzleManager reference.", this);
            return;
        }

        int nextHeat = Mathf.Clamp(puzzleManager.CurrentHeat + heatStep, 0, 5);
        puzzleManager.SetHeat(nextHeat);
        ApplyHeatVisuals(nextHeat);
    }

    public void ReduceHeat()
    {
        if (puzzleManager == null)
        {
            Debug.LogError($"Cooking station {name} has no PotionPuzzleManager reference.", this);
            return;
        }

        int nextHeat = Mathf.Clamp(puzzleManager.CurrentHeat - heatStep, 0, 5);
        puzzleManager.SetHeat(nextHeat);
        ApplyHeatVisuals(nextHeat);
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
    void SetCarried(bool carried);
}

public class PotionBaseBottle : MonoBehaviour, IInteractable, IPotionCarryable
{
    [SerializeField] private Transform carryTransform;
    [SerializeField] private PotionIngredient ingredient;

    private Collider[] colliders;

    public Transform CarryTransform => carryTransform != null ? carryTransform : transform;
    public PotionIngredient Ingredient => ingredient;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>(true);
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

    public void Highlight(bool state)
    {
    }
}
