using System.Collections;
using PurrNet;
using UnityEngine;

public class ClockPuzzleManager : PuzzleBase
{
    public ControlClock[] controlClocks;
    public TargetClock[] targetClocks;
    [SerializeField] private AudioClip tickSound;

    int[] mapping = {2,4,0,1,3};

    [SerializeField] int[] correctHours = {3,7,11,2,9};

    protected override void OnSpawned()
    {
        if (!isServer)
            return;
    }

    [ServerRpc]
    public void RotateControlClock(int controlIndex)
    {
        if (controlIndex < 0 || controlIndex >= mapping.Length)
            return;

        int targetIndex = mapping[controlIndex];

        var control = controlClocks[controlIndex];
        var target = targetClocks[targetIndex];

        control.AddHour();
        target.AddHour();

        PlayTickSound(control.transform.position);
        PlayTickSound(target.transform.position);

        CheckSolved();
    }

    void CheckSolved()
    {
        for (int i = 0; i < targetClocks.Length; i++)
        {
            if (targetClocks[i].CurrentHour != correctHours[i])
                return;
        }

        SolvePuzzle();
        ShowSolvedUI();
        Debug.Log("Clock Puzzle Solved!");
    }

    [ObserversRpc]
    void PlayTickSound(Vector3 position)
    {
        if (tickSound)
        {
            float volume = SettingsManager.Instance != null
                ? SettingsManager.Instance.GetVolume(SettingsVolumeChannel.Sfx)
                : 1f;
            AudioSource.PlayClipAtPoint(tickSound, position, volume);
        }
    }

    [ObserversRpc]
    void ShowSolvedUI()
    {
        InstanceHandler.GetInstance<GameViewManager>()
            .ShowView<PuzzleSolvedView>();
    }
}