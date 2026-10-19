using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class LevelSelectionManager : NetworkBehaviour
{
    [Serializable]
    private struct LevelSpawn
    {
        public Transform firstPlayer;
        public Transform secondPlayer;
    }

    [SerializeField] private List<LevelSpawn> levelSpawns = new List<LevelSpawn>();
    [SerializeField] private List<PlayerController> players = new List<PlayerController>();

    [ServerRpc(requireOwnership: false)]
    public void RequestLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelSpawns.Count)
        {
            Debug.LogWarning($"Rejected level request {levelIndex}: no matching spawn points.", this);
            return;
        }

        RefreshPlayers();
        if (players.Count == 0)
        {
            Debug.LogWarning("Cannot enter a level because no players are spawned.", this);
            return;
        }

        var spawn = levelSpawns[levelIndex];
        TeleportPlayer(players[0], spawn.firstPlayer);

        if (players.Count > 1)
            TeleportPlayer(players[1], spawn.secondPlayer);

        PlayLevelMusic(levelIndex);
    }

    [ObserversRpc(runLocally: true)]
    private void PlayLevelMusic(int levelIndex)
    {
        BackgroundMusic.PlayLevelMusic(levelIndex);
    }

    private void RefreshPlayers()
    {
        players.Clear();
        players.AddRange(FindObjectsByType<PlayerController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None));
    }

    private static void TeleportPlayer(PlayerController player, Transform spawnPoint)
    {
        if (player == null || spawnPoint == null)
            return;

        var controller = player.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        player.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);

        if (controller != null)
            controller.enabled = true;
    }
}
