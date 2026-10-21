using System.Collections;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public enum PotionBatchState
{
    Empty,
    ReadyToCook,
    Cooking,
    ReadyToPour,
    Failed,
    Completed
}

public class PotionPuzzleManager : PuzzleBase
{
    [Header("Recipes")]
    [SerializeField] private PotionRecipe[] recipes;

    [Header("Cooking")]
    [SerializeField, Range(0, 5)] private int currentHeat;
    [SerializeField, Min(0.1f)] private float heatTolerance = 0f;
    [SerializeField, Min(0.1f)] private float heatCheckInterval = 0.1f;
    [SerializeField, Min(0.1f)] private float explosionDelay = 0.5f;
    [SerializeField] private PotionCookingStation cookingStation;
    [SerializeField] private PotionResultBottle[] resultBottles;
    [SerializeField] private ParticleSystem explosionEffect;

    private readonly List<PotionIngredient> currentIngredients = new List<PotionIngredient>();
    private readonly HashSet<int> completedBottles = new HashSet<int>();
    private PotionBatchState batchState = PotionBatchState.Empty;
    private int currentRecipeIndex;
    private float correctHeatTime;
    private Coroutine cookingRoutine;

    public PotionBatchState BatchState => batchState;
    public int CurrentRecipeIndex => currentRecipeIndex;
    public int CompletedBottleCount => completedBottles.Count;
    public int CurrentHeat => currentHeat;
    public IReadOnlyList<PotionIngredient> CurrentIngredients => currentIngredients;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        if (!isServer)
            return;

        if (recipes == null || recipes.Length == 0)
            recipes = PotionRecipes.CreateDefault();

        if (!ValidateRecipes())
        {
            Debug.LogError($"Potion puzzle {name} has invalid recipes.", this);
            enabled = false;
            return;
        }

        ResetCurrentBatch();
    }

    [ServerRpc(requireOwnership: false)]
    public void AddIngredient(PotionIngredient ingredient)
    {
        if (!isServer || batchState != PotionBatchState.Empty)
            return;

        PotionRecipe recipe = recipes[currentRecipeIndex];
        if (currentIngredients.Count >= recipe.ingredients.Length)
        {
            FailCurrentBatch("Too many ingredients were added.");
            return;
        }

        currentIngredients.Add(ingredient);

        int ingredientIndex = currentIngredients.Count - 1;
        if (currentIngredients[ingredientIndex] != recipe.ingredients[ingredientIndex])
        {
            FailCurrentBatch("Ingredients were added in the wrong order.");
            return;
        }

        if (currentIngredients.Count == recipe.ingredients.Length)
            batchState = PotionBatchState.ReadyToCook;
    }

    [ServerRpc(requireOwnership: false)]
    public void SetHeat(int heatLevel)
    {
        if (!isServer || batchState == PotionBatchState.Completed)
            return;

        currentHeat = Mathf.Clamp(heatLevel, 0, 5);
        UpdateHeatVisuals(currentHeat);
    }

    [ServerRpc(requireOwnership: false)]
    public void StartCooking()
    {
        if (!isServer || batchState != PotionBatchState.ReadyToCook)
            return;

        correctHeatTime = 0f;
        batchState = PotionBatchState.Cooking;
        cookingRoutine = StartCoroutine(CookCurrentBatch());
    }

    [ServerRpc(requireOwnership: false)]
    public void PourIntoBottle(int bottleId)
    {
        if (!isServer || batchState != PotionBatchState.ReadyToPour)
            return;

        PotionRecipe recipe = recipes[currentRecipeIndex];
        if (bottleId != recipe.resultBottleId)
        {
            FailCurrentBatch("The potion was poured into the wrong bottle.");
            return;
        }

        completedBottles.Add(bottleId);
        batchState = PotionBatchState.Completed;
        SetBottleFilled(bottleId);

        if (completedBottles.Count >= PotionRecipes.TotalPotions)
        {
            SolvePuzzle();
            return;
        }

        currentRecipeIndex++;
        ResetCurrentBatch();
    }

    private IEnumerator CookCurrentBatch()
    {
        PotionRecipe recipe = recipes[currentRecipeIndex];

        while (batchState == PotionBatchState.Cooking)
        {
            yield return new WaitForSeconds(heatCheckInterval);

            float heatDifference = Mathf.Abs(currentHeat - recipe.requiredHeat);
            if (heatDifference <= heatTolerance)
            {
                correctHeatTime += heatCheckInterval;
                if (correctHeatTime >= recipe.requiredDuration)
                    batchState = PotionBatchState.ReadyToPour;
            }
            else if (currentHeat > recipe.requiredHeat)
            {
                yield return new WaitForSeconds(explosionDelay);
                if (batchState == PotionBatchState.Cooking)
                    FailCurrentBatch("The heat was too high for too long.");
            }
        }

        cookingRoutine = null;
    }

    private void FailCurrentBatch(string reason)
    {
        if (batchState == PotionBatchState.Failed)
            return;

        if (cookingRoutine != null)
        {
            StopCoroutine(cookingRoutine);
            cookingRoutine = null;
        }

        Debug.LogWarning($"Potion batch {currentRecipeIndex} failed: {reason}", this);
        batchState = PotionBatchState.Failed;
        PlayExplosion();
        ResetEntirePuzzle();
    }

    private void ResetCurrentBatch()
    {
        currentIngredients.Clear();
        correctHeatTime = 0f;
        currentHeat = 0;
        batchState = PotionBatchState.Empty;
    }

    private void ResetEntirePuzzle()
    {
        currentRecipeIndex = 0;
        completedBottles.Clear();
        ResetCurrentBatch();
        ResetBottleVisuals();
    }

    private bool ValidateRecipes()
    {
        if (recipes.Length != PotionRecipes.TotalPotions)
            return false;

        bool[] foundRecipeIds = new bool[PotionRecipes.TotalPotions];
        bool[] foundBottleIds = new bool[PotionRecipes.TotalPotions];

        for (int i = 0; i < recipes.Length; i++)
        {
            PotionRecipe recipe = recipes[i];
            if (!recipe.IsValid()
                || recipe.potionId != i
                || recipe.potionId < 0
                || recipe.potionId >= PotionRecipes.TotalPotions
                || recipe.resultBottleId >= PotionRecipes.TotalPotions
                || recipe.resultBottleId < 0
                || foundRecipeIds[recipe.potionId]
                || foundBottleIds[recipe.resultBottleId])
            {
                return false;
            }

            foundRecipeIds[recipe.potionId] = true;
            foundBottleIds[recipe.resultBottleId] = true;
        }

        return true;
    }

    [ObserversRpc(runLocally: true)]
    private void UpdateHeatVisuals(int heatLevel)
    {
        if (cookingStation != null)
            cookingStation.SetHeatVisual(heatLevel);
    }

    [ObserversRpc(runLocally: true)]
    private void SetBottleFilled(int bottleId)
    {
        if (resultBottles == null || bottleId < 0 || bottleId >= resultBottles.Length)
            return;

        if (resultBottles[bottleId] != null)
            resultBottles[bottleId].SetFilledVisual(true);
    }

    [ObserversRpc(runLocally: true)]
    private void ResetBottleVisuals()
    {
        if (resultBottles == null)
            return;

        foreach (PotionResultBottle bottle in resultBottles)
        {
            if (bottle != null)
                bottle.SetFilledVisual(false);
        }
    }

    [ObserversRpc(runLocally: true)]
    private void PlayExplosion()
    {
        if (explosionEffect != null)
            explosionEffect.Play();
    }
}
