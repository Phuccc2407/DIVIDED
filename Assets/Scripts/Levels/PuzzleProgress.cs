public sealed class PuzzleProgress
{
    public int CompletedMask { get; private set; }
    public bool AllCompleted => CompletedMask == 15;

    public bool TryComplete(int puzzleNumber)
    {
        if (puzzleNumber < 1 || puzzleNumber > 4) return false;
        int bit = 1 << (puzzleNumber - 1);
        if ((CompletedMask & bit) != 0) return false;
        CompletedMask |= bit;
        return true;
    }

    public void Reset() => CompletedMask = 0;
}
