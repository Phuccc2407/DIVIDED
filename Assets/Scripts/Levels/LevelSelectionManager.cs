using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public class LevelSelectionManager : NetworkBehaviour
{
    public static LevelSelectionManager Instance { get; private set; }

    [Serializable]
    private class PuzzleChapter
    {
        public int number;
        public int levelIndex;
        public string title;
        public PuzzleBase puzzle;
    }
    [Serializable]
    private struct LevelSpawn
    {
        public Transform firstPlayer;
        public Transform secondPlayer;
    }

    [SerializeField] private List<LevelSpawn> levelSpawns = new List<LevelSpawn>();
    [SerializeField] private List<PlayerController> players = new List<PlayerController>();
    [Header("Hub return points (separate from puzzle indices)")]
    [SerializeField] private Transform hubFirstPlayer;
    [SerializeField] private Transform hubSecondPlayer;
    [SerializeField] private List<GlassBridgePuzzleManager> glassBridgePuzzles = new List<GlassBridgePuzzleManager>();
    [SerializeField] private List<TheatrePuzzleManager> theatrePuzzles = new List<TheatrePuzzleManager>();
    [Header("Puzzle announcements and ending")]
    [SerializeField] private PuzzlePresentation presentation;
    [SerializeField] private List<PuzzleChapter> chapters = new List<PuzzleChapter> {
        new PuzzleChapter { number = 1, levelIndex = 0, title = "THE THIRTEENTH HOUR" },
        new PuzzleChapter { number = 2, levelIndex = 1, title = "THE ALCHEMIST'S OATH" },
        new PuzzleChapter { number = 3, levelIndex = 2, title = "A STEP FROM OBLIVION" },
        new PuzzleChapter { number = 4, levelIndex = 3, title = "THE SILENT ENCORE" }
    };
    private readonly PuzzleProgress progress = new PuzzleProgress();
    private bool endingActive;
    public int CompletedPuzzleMask { get; private set; }

    public bool IsLevelCompleted(int levelIndex)
    {
        int mask = isServer ? progress.CompletedMask : CompletedPuzzleMask;
        foreach (var chapter in chapters)
            if (chapter.levelIndex == levelIndex && chapter.number >= 1 && chapter.number <= 4
                && (mask & (1 << (chapter.number - 1))) != 0) return true;
        return false;
    }

    private void Awake()
    {
        Instance = this;
        if (presentation == null) presentation = GetComponent<PuzzlePresentation>();
        if (presentation == null) presentation = gameObject.AddComponent<PuzzlePresentation>();
        presentation.SetReturnAction(RequestReturnFromEnding);
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (!isServer) return;
        progress.Reset();
        endingActive = false;
        PublishProgress(0, false);
    }

    protected override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    [ServerRpc(requireOwnership: false)]
    public void RequestLevel(int levelIndex)
    {
        if (endingActive) return;
        HandleLevelRequest(levelIndex);
    }

    private void HandleLevelRequest(int levelIndex, bool announce = true)
    {
        // Validate on the server too, including requests sent before the door updated.
        if (IsLevelCompleted(levelIndex)) return;
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

        TeleportPlayers(levelIndex);
        PlayLevelMusic(levelIndex);
        foreach (var bridge in glassBridgePuzzles)
            if (bridge != null) bridge.OnLevelEntered(levelIndex);
        foreach (var theatre in theatrePuzzles)
            if (theatre != null) theatre.OnLevelEntered(levelIndex);
        if (announce)
            foreach (var chapter in chapters)
                if (chapter.levelIndex == levelIndex)
                {
                    ShowChapter(chapter.number, chapter.title, false);
                    break;
                }
    }

    public void RestartLevel(int levelIndex)
    {
        if (isServer && !endingActive) HandleLevelRequest(levelIndex, false);
    }

    public void ReturnPlayersToHub()
    {
        if (!isServer)
        {
            Debug.LogWarning(
                $"Rejected Hub return from non-server {name}.", this);
            return;
        }

        if (hubFirstPlayer == null || hubSecondPlayer == null)
        {
            Debug.LogError("Assign Hub First Player and Hub Second Player on LevelSelectionManager. Hub return does not use puzzle level indices.", this);
            return;
        }
        TeleportPlayersToHub();
        PlayHubMusic();
        foreach (var bridge in glassBridgePuzzles)
            if (bridge != null) bridge.OnLevelEntered(-1);
        foreach (var theatre in theatrePuzzles)
            if (theatre != null) theatre.OnLevelEntered(-1);
    }

    [Obsolete("Hub uses its own spawn references. Call ReturnPlayersToHub() instead.")]
    public void ReturnPlayersToHub(int ignoredLevelIndex) => ReturnPlayersToHub();

    [ObserversRpc(runLocally: true)]
    private void TeleportPlayersToHub()
    {
        RefreshPlayers();
        if (players.Count > 0) TeleportPlayer(players[0], hubFirstPlayer);
        if (players.Count > 1) TeleportPlayer(players[1], hubSecondPlayer);
    }

    [ObserversRpc(runLocally: true)]
    private void PlayHubMusic() => BackgroundMusic.PlayHubMusic();

    // Called by puzzle managers only on the server. Duplicate wins never count twice.
    public bool NotifyPuzzleCompleted(PuzzleBase puzzle)
    {
        if (!isServer || puzzle == null) return false;
        int fallbackNumber = puzzle is ClockPuzzleManager ? 1 : puzzle is PotionPuzzleManager ? 2
            : puzzle is GlassBridgePuzzleManager ? 3 : puzzle is TheatrePuzzleManager ? 4 : 0;
        foreach (var chapter in chapters)
        {
            if (chapter.puzzle != puzzle && !(chapter.puzzle == null && chapter.number == fallbackNumber)) continue;
            bool firstCompletion = progress.TryComplete(chapter.number);
            ShowChapter(chapter.number, chapter.title, true);
            if (!firstCompletion) return true;
            endingActive = progress.AllCompleted;
            PublishProgress(progress.CompletedMask, endingActive);
            return true;
        }
        return false;
    }

    [ObserversRpc(runLocally: true)]
    private void ShowChapter(int number, string title, bool completed)
    {
        if (presentation != null) presentation.ShowChapter(number, title, completed);
    }

    [ObserversRpc(runLocally: true, bufferLast: true)]
    private void PublishProgress(int mask, bool showEnding)
    {
        CompletedPuzzleMask = mask;
        if (showEnding && presentation != null) presentation.ShowEnding();
    }

    [ServerRpc(requireOwnership: false)]
    public void RequestReturnFromEnding()
    {
        if (!endingActive) return;
        endingActive = false;
        DismissEnding();
        PublishProgress(progress.CompletedMask, false);
        ReturnPlayersToHub();
    }

    [ObserversRpc(runLocally: true)]
    private void DismissEnding()
    {
        if (presentation != null) presentation.HideEnding();
    }

    [ObserversRpc(runLocally: true)]
    private void TeleportPlayers(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelSpawns.Count)
        {
            Debug.LogWarning($"Rejected level teleport {levelIndex}: no matching spawn points.", this);
            return;
        }

        RefreshPlayers();
        if (players.Count == 0)
        {
            Debug.LogWarning("Cannot teleport players because no players are spawned.", this);
            return;
        }

        var spawn = levelSpawns[levelIndex];
        TeleportPlayer(players[0], spawn.firstPlayer);

        if (players.Count > 1)
            TeleportPlayer(players[1], spawn.secondPlayer);
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
        // Instance discovery order differs across peers; ownership IDs do not.
        players.Sort((a, b) => string.CompareOrdinal(a.owner.ToString(), b.owner.ToString()));
    }

    private static void TeleportPlayer(PlayerController player, Transform spawnPoint)
    {
        if (player == null || spawnPoint == null)
            return;

        player.TeleportTo(spawnPoint.position, spawnPoint.rotation);
    }
}
