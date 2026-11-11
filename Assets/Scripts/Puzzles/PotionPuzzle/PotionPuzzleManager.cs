
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public enum PotionBatchState
{
    Empty,
    ReadyToCook,
    Cooking,
    Completed
}

public class PotionPuzzleManager : PuzzleBase
{
    [Header("Recipe Sequence")]
    [SerializeField] private PotionRecipe[] recipes;
    [SerializeField, Range(5, 6)] private int requiredSuccessfulRecipes = 6;

    [Header("Cooking")]
    [SerializeField] private SyncVar<int> currentHeat = new(0);
    [SerializeField] private SyncVar<Color> liquidColor = new(Color.clear);
    [SerializeField, Min(0.1f)] private float heatTolerance = 0.1f;
    [SerializeField] private PotionCookingStation cookingStation;
    [SerializeField] private LevelSelectionManager levelSelectionManager;
    [SerializeField, Min(0)] private int hubLevelIndex;

    [Header("Scene Particle Effects")]
    [SerializeField] private ParticleSystem explosionEffect;
    [SerializeField] private ParticleSystem successEffect;

    private readonly List<PotionIngredient> currentIngredients = new();
    private PotionBatchState batchState = PotionBatchState.Empty;
    private Color mixedColor;
    private int mixedIngredientCount;
    private int successfulRecipeCount;
    private int currentRecipeIndex;

    public PotionBatchState BatchState => batchState;
    public int CurrentHeat => currentHeat.value;
    public IReadOnlyList<PotionIngredient> CurrentIngredients => currentIngredients;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        currentHeat.onChanged += OnHeatChanged;
        liquidColor.onChanged += OnLiquidColorChanged;

        if (cookingStation == null)
            Debug.LogError(
                $"Potion puzzle {name} has no Cooking Station assigned.", this);

        if (levelSelectionManager == null)
            Debug.LogError(
                $"Potion puzzle {name} has no LevelSelectionManager assigned.", this);

        if (explosionEffect == null)
            Debug.LogWarning(
                $"Potion puzzle {name} has no Explosion Effect assigned.", this);
        else
            PrepareEffect(explosionEffect);

        if (successEffect == null)
            Debug.LogWarning(
                $"Potion puzzle {name} has no Success Effect assigned.", this);
        else
            PrepareEffect(successEffect);

        if (!isServer)
        {
            OnHeatChanged(currentHeat.value);
            OnLiquidColorChanged(liquidColor.value);
            return;
        }

        if (!HasValidRecipes())
            recipes = PotionRecipes.CreateDefault();

        requiredSuccessfulRecipes = Mathf.Clamp(
            requiredSuccessfulRecipes,
            5,
            Mathf.Min(6, recipes.Length)
        );

        successfulRecipeCount = 0;
        currentRecipeIndex = 0;
        ResetBatch();
    }

    protected override void OnDestroy()
    {
        currentHeat.onChanged -= OnHeatChanged;
        liquidColor.onChanged -= OnLiquidColorChanged;

        base.OnDestroy();
    }

    [ServerRpc(requireOwnership: false)]
    public void AddIngredient(PotionIngredient ingredient, Color ingredientColor)
    {
        if (!isServer
            || (batchState != PotionBatchState.Empty
                && batchState != PotionBatchState.ReadyToCook))
            return;

        currentIngredients.Add(ingredient);

        mixedColor = mixedIngredientCount == 0
            ? ingredientColor
            : Color.Lerp(
                mixedColor,
                ingredientColor,
                1f / (mixedIngredientCount + 1)
            );

        mixedIngredientCount++;
        liquidColor.value = mixedColor;

        batchState = PotionBatchState.ReadyToCook;
    }

    [ServerRpc(requireOwnership: false)]
    public void AdjustHeat(int amount)
    {
        if (!isServer || batchState == PotionBatchState.Completed)
            return;

        currentHeat.value = Mathf.Clamp(
            currentHeat.value + Mathf.Clamp(amount, -5, 5),
            0,
            5
        );

        Debug.Log($"Potion heat changed: {currentHeat.value}/5.", this);
    }

    [ServerRpc(requireOwnership: false)]
    public void StartCooking()
    {
        if (!isServer)
            return;

        if (batchState != PotionBatchState.ReadyToCook || currentIngredients.Count == 0)
        {
            Debug.LogWarning(
                $"Cannot start potion cooking. State={batchState}, " +
                $"recipe={currentRecipeIndex + 1}/{requiredSuccessfulRecipes}, " +
                $"ingredients={currentIngredients.Count}/{CurrentRecipe.ingredients.Length}, " +
                $"heat={currentHeat.value}.",
                this
            );
            return;
        }

        if (!HasCorrectIngredients())
        {
            FailBatch("Sai cong thuc hoac sai thu tu.");
            return;
        }

        if (Mathf.Abs(currentHeat.value - CurrentRecipe.requiredHeat) > heatTolerance)
        {
            FailBatch(
                $"Sai nhiet do. Can {CurrentRecipe.requiredHeat}, " +
                $"dang la {currentHeat.value}."
            );
            return;
        }

        successfulRecipeCount++;
        Debug.Log(
            $"Potion recipe {successfulRecipeCount}/{requiredSuccessfulRecipes} " +
            $"succeeded at heat {currentHeat.value}.",
            this
        );

        PlaySuccessEffectLocal();
        PlaySuccessEffect();

        if (successfulRecipeCount >= requiredSuccessfulRecipes)
        {
            batchState = PotionBatchState.Completed;
            SolvePuzzle();
            ShowSolvedUI();
            ReturnPlayersToHub();
            return;
        }

        currentRecipeIndex++;
        ResetBatch();
    }

    private bool HasCorrectIngredients()
    {
        PotionRecipe currentRecipe = CurrentRecipe;

        if (currentIngredients.Count != currentRecipe.ingredients.Length)
            return false;

        for (int i = 0; i < currentRecipe.ingredients.Length; i++)
        {
            if (currentIngredients[i] != currentRecipe.ingredients[i])
                return false;
        }

        return true;
    }

    private PotionRecipe CurrentRecipe =>
        recipes[Mathf.Clamp(currentRecipeIndex, 0, recipes.Length - 1)];

    private bool HasValidRecipes()
    {
        if (recipes == null || recipes.Length < 6)
            return false;

        for (int i = 0; i < recipes.Length; i++)
        {
            if (!recipes[i].IsValid())
                return false;
        }

        return true;
    }

    private void FailBatch(string reason)
    {
        if (!isServer)
            return;

        Debug.LogWarning($"Potion puzzle failed: {reason}", this);

        PlayExplosionLocal();
        PlayExplosion();

        ResetBatch();
    }

    [ObserversRpc(runLocally: true)]
    private void ShowSolvedUI()
    {
        GameViewManager viewManager =
            InstanceHandler.GetInstance<GameViewManager>();

        if (viewManager == null)
        {
            Debug.LogError(
                $"Potion puzzle {name} cannot show solved UI: " +
                "GameViewManager instance is missing.", this);
            return;
        }

        viewManager.ShowView<PuzzleSolvedView>();
    }

    private void ReturnPlayersToHub()
    {
        if (levelSelectionManager == null)
            return;

        levelSelectionManager.ReturnPlayersToHub(hubLevelIndex);
    }

    private void ResetBatch()
    {
        currentIngredients.Clear();

        mixedColor = Color.clear;
        mixedIngredientCount = 0;

        currentHeat.value = 0;
        liquidColor.value = mixedColor;

        batchState = PotionBatchState.Empty;

        cookingStation?.ResetLiquid();
    }

    private void OnHeatChanged(int heatLevel)
    {
        cookingStation?.SetHeatVisual(heatLevel);
    }

    private void OnLiquidColorChanged(Color color)
    {
        cookingStation?.SetLiquidColor(color);
    }

    // =========================
    // PARTICLE EFFECTS
    // =========================

    private void PrepareEffect(ParticleSystem effect)
    {
        if (effect == null)
            return;

        // Giữ nguyên vị trí và rotation trong Scene.
        // Chỉ dừng particle, không di chuyển object.
        effect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );
    }

    private void PlayEffect(ParticleSystem effect)
    {
        if (effect == null)
            return;

        if (!effect.gameObject.activeInHierarchy)
        {
            Debug.LogWarning(
                $"Particle {effect.name} is inactive. " +
                "Keep the scene Particle GameObject active.",
                effect
            );
            return;
        }

        effect.Stop(
            true,
            ParticleSystemStopBehavior.StopEmittingAndClear
        );

        effect.Play(true);
    }

    private void PlayExplosionLocal()
    {
        PlayEffect(explosionEffect);
    }

    [ObserversRpc(runLocally: false)]
    private void PlayExplosion()
    {
        PlayExplosionLocal();
    }

    private void PlaySuccessEffectLocal()
    {
        PlayEffect(successEffect);
        cookingStation?.PlaySuccess();
    }

    [ObserversRpc(runLocally: false)]
    private void PlaySuccessEffect()
    {
        PlaySuccessEffectLocal();
    }
}
