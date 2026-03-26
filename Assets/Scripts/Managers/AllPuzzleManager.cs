using PurrNet;
using UnityEngine;
using System.Collections.Generic;

public class AllPuzzleManager : NetworkBehaviour
{
    public static AllPuzzleManager Instance;

    [SerializeField]
    List<PuzzleBase> puzzles = new List<PuzzleBase>();

    int currentIndex = 0;

    void Awake()
    {
        Instance = this;
    }

    protected override void OnSpawned()
    {
        if (!isServer)
            return;

        ActivateCurrentPuzzle();
    }

    [ServerRpc]
    void ActivateCurrentPuzzle()
    {
        if (currentIndex >= puzzles.Count)
            return;

        puzzles[currentIndex].ActivatePuzzle();
    }

    [ServerRpc]
    public void PuzzleSolved(PuzzleBase puzzle)
    {
        if (puzzle != puzzles[currentIndex])
            return;

        currentIndex++;

        ActivateCurrentPuzzle();
    }
}