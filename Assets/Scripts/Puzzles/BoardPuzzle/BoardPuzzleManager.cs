using PurrNet;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BoardPuzzleManager : PuzzleBase
{
    public Tile[] tiles;

    List<int> sequence = new List<int>();

    int step = 0;

    float timer = 10f;

    int round = 1;
    int maxRounds = 3;

    bool collapsing = false;

    protected override void OnSpawned()
    {
        if (!isServer) return;

        GenerateBoard();
        StartRound();
    }

    void Update()
    {
        if (!isServer) return;
        if (collapsing) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            StartCoroutine(CollapseRoutine());
        }
    }

    void StartRound()
    {
        GenerateSequence();
        step = 0;

        ResetBoardClient();

        timer = 10f;
    }

    void GenerateSequence()
    {
        sequence.Clear();

        for (int i = 0; i < 5; i++)
        {
            sequence.Add(Random.Range(0, tiles.Length));
        }
    }

    void GenerateBoard()
    {
        List<(Tile.TileShape, Tile.TileColor)> combos = new List<(Tile.TileShape, Tile.TileColor)>();

        foreach (Tile.TileShape shape in System.Enum.GetValues(typeof(Tile.TileShape)))
        {
            foreach (Tile.TileColor color in System.Enum.GetValues(typeof(Tile.TileColor)))
            {
                combos.Add((shape, color));
            }
        }

        Shuffle(combos);

        for (int i = 0; i < tiles.Length; i++)
        {
            var combo = combos[i];
            tiles[i].Setup(combo.Item1, combo.Item2);
        }
    }

    IEnumerator CollapseRoutine()
    {
        collapsing = true;

        int safeIndex = sequence[step];

        WarningTilesClient();

        yield return new WaitForSeconds(1.5f);

        CollapseBoardClient(safeIndex);

        yield return new WaitForSeconds(3f);

        ResetBoardClient();

        step++;

        if (step >= sequence.Count)
        {
            RoundComplete();
        }

        timer = 10f;
        collapsing = false;
    }

    void RoundComplete()
    {
        round++;

        if (round > maxRounds)
        {
            SolvePuzzle();
            return;
        }

        StartRound();
    }

    void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int r = Random.Range(i, list.Count);
            (list[i], list[r]) = (list[r], list[i]);
        }
    }

    [ObserversRpc]
    void WarningTilesClient()
    {
        foreach (var t in tiles)
            t.Warning();
    }

    [ObserversRpc]
    void CollapseBoardClient(int safeIndex)
    {
        for (int i = 0; i < tiles.Length; i++)
        {
            if (i == safeIndex)
                tiles[i].Show();
            else
                tiles[i].Hide();
        }
    }

    [ObserversRpc]
    void ResetBoardClient()
    {
        foreach (Tile t in tiles)
            t.Show();
    }
}