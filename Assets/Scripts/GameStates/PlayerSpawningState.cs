using System;
using System.Collections.Generic;
using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;

public class PlayerSpawningState : StateNode
{
    [SerializeField] private PlayerController playerPrefab;
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        DespawnPlayers();

        SpawnPlayer();
        //machine.Next();
    }

    private void SpawnPlayer()
    {
        int currentSpawnIndex = 0;
        foreach(var player in networkManager.players)
        {
            var spawnPoint = spawnPoints[currentSpawnIndex];
            var newPlayer = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
            newPlayer.GiveOwnership(player);
            currentSpawnIndex++;

            if (currentSpawnIndex >= spawnPoints.Count)
                currentSpawnIndex = 0;
        }
    }

    private void DespawnPlayers()
    {
        var allPlayers = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var player in allPlayers) {
            Destroy(player.gameObject);
        }
    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
    }
}
