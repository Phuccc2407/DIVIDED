using PurrNet;
using UnityEngine;

public class ControlClock : NetworkBehaviour, IInteractable
{
    public int index;
    public ClockPuzzleManager puzzle;

    [SerializeField] private Renderer rend;
    [SerializeField] private Transform hourHand;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tickSound;

    int hour = 12;

    public int CurrentHour => hour;

    public void Interact()
    {
        puzzle.RotateControlClock(index);
    }

    public void Highlight(bool state)
    {
        rend.material.color = state ? Color.yellow : Color.white;
    }

    public void AddHour()
    {
        if (!isServer)
            return;

        hour++;

        if (hour > 12)
            hour = 1;

        UpdateClock_Observers(hour);
    }

    [ObserversRpc]
    void UpdateClock_Observers(int newHour)
    {
        hour = newHour;

        float angle = 30f * hour;

        hourHand.localRotation =
            Quaternion.Euler(0, -angle, 0);
    }
}