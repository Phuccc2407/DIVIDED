using PurrNet;
using UnityEngine;

public abstract class PuzzleBase : NetworkBehaviour
{
    public bool solved = false;

    public virtual void ActivatePuzzle()
    {
        Debug.Log(name + " activated");
    }

    protected void SolvePuzzle()
    {
        if (!isServer)
            return;

        if (solved)
            return;

        solved = true;

        AllPuzzleManager.Instance.PuzzleSolved(this);
    }
}