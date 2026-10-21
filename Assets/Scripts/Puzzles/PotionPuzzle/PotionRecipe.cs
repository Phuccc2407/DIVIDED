using System;
using UnityEngine;

public enum PotionIngredient
{
    RedHerb,
    BlueHerb,
    YellowHerb,
    PurpleHerb,
    WhiteHerb
}

[Serializable]
public struct PotionRecipe
{
    [Min(0)] public int potionId;
    public PotionIngredient[] ingredients;
    [Range(0, 5)] public int requiredHeat;
    [Min(1f)] public float requiredDuration;
    [Min(0)] public int resultBottleId;

    public bool IsValid()
    {
        return ingredients != null
            && ingredients.Length >= 2
            && ingredients.Length <= 3
            && requiredHeat > 0
            && requiredDuration > 0f
            && resultBottleId >= 0;
    }

    public bool MatchesIngredients(PotionIngredient[] submittedIngredients)
    {
        if (submittedIngredients == null || submittedIngredients.Length != ingredients.Length)
            return false;

        for (int i = 0; i < ingredients.Length; i++)
        {
            if (submittedIngredients[i] != ingredients[i])
                return false;
        }

        return true;
    }
}

public static class PotionRecipes
{
    public const int TotalPotions = 6;

    public static PotionRecipe[] CreateDefault()
    {
        return new[]
        {
            Create(0, PotionIngredient.RedHerb, PotionIngredient.BlueHerb, PotionIngredient.YellowHerb, 2, 8f, 0),
            Create(1, PotionIngredient.YellowHerb, PotionIngredient.PurpleHerb, 4, 12f, 1),
            Create(2, PotionIngredient.BlueHerb, PotionIngredient.WhiteHerb, PotionIngredient.RedHerb, 1, 6f, 2),
            Create(3, PotionIngredient.PurpleHerb, PotionIngredient.YellowHerb, 3, 10f, 3),
            Create(4, PotionIngredient.WhiteHerb, PotionIngredient.BlueHerb, PotionIngredient.PurpleHerb, 5, 15f, 4),
            Create(5, PotionIngredient.RedHerb, PotionIngredient.YellowHerb, 2, 9f, 5)
        };
    }

    private static PotionRecipe Create(
        int potionId,
        PotionIngredient first,
        PotionIngredient second,
        int requiredHeat,
        float requiredDuration,
        int resultBottleId)
    {
        return new PotionRecipe
        {
            potionId = potionId,
            ingredients = new[] { first, second },
            requiredHeat = requiredHeat,
            requiredDuration = requiredDuration,
            resultBottleId = resultBottleId
        };
    }

    private static PotionRecipe Create(
        int potionId,
        PotionIngredient first,
        PotionIngredient second,
        PotionIngredient third,
        int requiredHeat,
        float requiredDuration,
        int resultBottleId)
    {
        return new PotionRecipe
        {
            potionId = potionId,
            ingredients = new[] { first, second, third },
            requiredHeat = requiredHeat,
            requiredDuration = requiredDuration,
            resultBottleId = resultBottleId
        };
    }
}
