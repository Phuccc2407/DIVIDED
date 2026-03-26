using System;
using PurrNet;
using UnityEngine;

public class PlayerHealth : NetworkBehaviour
{
    [SerializeField] private SyncVar<int> health = new(100);
    [SerializeField] private int selfLayer, otherLayer;

    public event Action<PlayerID> OnDeath_Server;

    public int Health => health.value;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        var actualLayer = isOwner ? selfLayer : otherLayer;
        SetLayerRecursively(gameObject, actualLayer);

        if (isOwner)
        {
            //InstanceHandler.GetInstance<MainGameView>().UpdateHealth(health.value);
            health.onChanged += OnHealthChanged;
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        health.onChanged -= OnHealthChanged;
    }

    private void OnHealthChanged(int newHealth)
    {
        //InstanceHandler.GetInstance<MainGameView>().UpdateHealth(newHealth);
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void ChangeHealth(int amount, RPCInfo info = default)
    {
        health.value += amount;

        if (health.value <= 0)
        {
            if(InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
            {
                scoreManager.AddKill(info.sender);
                if(owner.HasValue)
                    scoreManager.AddDeath(owner.Value);
            }
            OnDeath_Server?.Invoke(owner.Value);
            Destroy(gameObject);
        }
    }
}
