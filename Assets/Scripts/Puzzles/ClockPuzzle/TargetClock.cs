using PurrNet;
using UnityEngine;

public class TargetClock : NetworkBehaviour
{
    [SerializeField] private Transform hourHand;

    int hour = 12;

    public int CurrentHour => hour;

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