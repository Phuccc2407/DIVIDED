using System;
using System.Collections.Generic;
using PurrNet;
using TMPro;
using UnityEngine;

[Serializable]
public class GlassBridgeRow
{
    public GlassBridgePanel left;
    public GlassBridgePanel right;
}

[Serializable]
public class GlassBridgeQuestion
{
    [TextArea] public string question;
    public string correctAnswer;
    public string wrongAnswer;
}

public enum GlassBridgePhase { Inactive, Waiting, Running, Falling, Completed }

public class GlassBridgePuzzleManager : PuzzleBase
{
    [Header("Hub")]
    [SerializeField] private LevelSelectionManager levelSelectionManager;
    [SerializeField] private int bridgeLevelIndex = 3;
    [Header("Safe first pair (no question)")]
    [SerializeField] private GlassBridgePanel startLeft;
    [SerializeField] private GlassBridgePanel startRight;
    [Header("Question pairs, ordered from start to finish")]
    [SerializeField] private GlassBridgeRow[] rows;
    [SerializeField] private BoxCollider finishZone;
    [SerializeField] private GlassBridgeQuestion[] questions;
    [Header("Display")]
    [SerializeField] private TMP_Text questionDisplay;
    [SerializeField] private TMP_Text timerDisplay;
    [Header("Rules")]
    [Tooltip("Set before entering the bridge: 1 for solo testing, 2 for co-op.")]
    [SerializeField, Range(1, 2)] private int requiredPlayers = 2;
    [SerializeField, Min(1f)] private float roundSeconds = 60f;
    [SerializeField, Min(0.5f)] private float fallSeconds = 3f;
    [SerializeField] private Transform fallBoundary;

    private GlassBridgePhase phase;
    private PlayerController[] participants;
    private int activePlayerCount = 2;
    private int[] questionIndices;
    private readonly List<int> questionDeck = new List<int>();
    private bool[] correctOnRight;
    private bool[] broken;
    private int currentRow;
    private float remaining;
    private float nextPublish;
    [Header("Bridge SFX (optional, plays on both clients)")]
    [SerializeField] private AudioClip roundStartSound;
    [SerializeField] private AudioClip safeRowSound;
    [SerializeField] private AudioClip countdownTickSound;
    [SerializeField] private AudioClip failureSound;
    [SerializeField] private AudioClip timeoutSound;
    [SerializeField] private AudioClip resetSound;
    [SerializeField, Range(0f, 1f)] private float bridgeSfxVolume = 0.7f;
    [SerializeField, Min(1)] private int warningSeconds = 10;
    private AudioSource bridgeSfx;
    private int lastWarningSecond;
    private bool hasSnapshot;

    // One-shots are deliberately not buffered: late joiners hear no stale cues.
    [ObserversRpc(runLocally: true)]
    private void PlayBridgeSound(int cue)
    {
        AudioClip clip = cue == 0 ? roundStartSound : cue == 1 ? safeRowSound
            : cue == 2 ? countdownTickSound : cue == 3 ? failureSound
            : cue == 4 ? timeoutSound : resetSound;
        if (clip == null) return;
        if (bridgeSfx == null)
        {
            bridgeSfx = gameObject.AddComponent<AudioSource>();
            bridgeSfx.playOnAwake = false;
            bridgeSfx.spatialBlend = 0f;
        }
        bridgeSfx.PlayOneShot(clip, bridgeSfxVolume * (SettingsManager.Instance != null ? SettingsManager.Instance.SfxVolume : 1f));
    }

    // Called only by the server when entering/leaving through the hub.
    public void OnLevelEntered(int levelIndex)
    {
        if (!isServer) return;
        if (levelIndex != bridgeLevelIndex)
        {
            phase = GlassBridgePhase.Inactive;
            Publish();
            return;
        }
        phase = GlassBridgePhase.Inactive;
        if (!ValidateSetup()) return;
        activePlayerCount = Mathf.Clamp(requiredPlayers, 1, 2);
        participants = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        if (participants.Length != activePlayerCount)
        {
            Debug.LogWarning($"Glass bridge requires {activePlayerCount} spawned player(s), found {participants.Length}. Set Required Players before entering the bridge.", this);
            return;
        }
        solved = false;
        ResetRound();
    }

    private bool ValidateSetup()
    {
        if (rows == null || rows.Length == 0 || startLeft == null || startRight == null
            || finishZone == null || fallBoundary == null || levelSelectionManager == null)
        {
            Debug.LogError("Glass bridge: assign rows, start pair, finish zone, fall boundary and hub manager.", this);
            return false;
        }
        foreach (var row in rows)
            if (row == null || row.left == null || row.right == null) return false;
        if (questions == null || questions.Length == 0)
            questions = GlassBridgeQuestionBank.CreateDefault();
        foreach (var q in questions)
            if (q == null || string.IsNullOrWhiteSpace(q.question)
                || string.IsNullOrWhiteSpace(q.correctAnswer) || string.IsNullOrWhiteSpace(q.wrongAnswer)
                || q.correctAnswer == q.wrongAnswer)
            {
                Debug.LogError("Glass bridge: every question needs two distinct, nonempty answers.", this);
                return false;
            }
        if (questions.Length < Mathf.Max(2, rows.Length))
        {
            Debug.LogError("Glass bridge: provide at least one unique question per row.", this);
            return false;
        }
        return true;
    }

    private void ResetRound()
    {
        int[] previous = questionIndices;
        questionIndices = new int[rows.Length];
        correctOnRight = new bool[rows.Length];
        broken = new bool[rows.Length * 2 + 2];
        // Draw without replacement across retries, not just within one attempt.
        // Refill when the remaining bank cannot supply a complete bridge.
        if (questionDeck.Count < rows.Length)
        {
            questionDeck.Clear();
            for (int i = 0; i < questions.Length; i++) questionDeck.Add(i);
            for (int i = questionDeck.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (questionDeck[i], questionDeck[j]) = (questionDeck[j], questionDeck[i]);
            }
            // With a large bank, avoid the entire previous attempt even at refill.
            if (previous != null && questions.Length >= rows.Length * 2)
            {
                for (int i = 0; i < rows.Length; i++)
                {
                    if (Array.IndexOf(previous, questionDeck[i]) < 0) continue;
                    for (int j = rows.Length; j < questionDeck.Count; j++)
                    {
                        if (Array.IndexOf(previous, questionDeck[j]) >= 0) continue;
                        (questionDeck[i], questionDeck[j]) = (questionDeck[j], questionDeck[i]);
                        break;
                    }
                }
            }
        }
        int[] pool = questionDeck.ToArray();
        // Avoid the same question at the same row on consecutive attempts.
        for (int i = 0; previous != null && i < rows.Length; i++)
        {
            if (pool[i] != previous[i]) continue;
            for (int j = 0; j < pool.Length; j++)
            {
                if (j == i || pool[j] == previous[i]) continue;
                if (j < rows.Length && pool[i] == previous[j]) continue;
                (pool[i], pool[j]) = (pool[j], pool[i]);
                break;
            }
        }
        for (int i = 0; i < rows.Length; i++)
        {
            questionIndices[i] = pool[i];
            correctOnRight[i] = UnityEngine.Random.value > 0.5f;
        }
        questionDeck.Clear();
        for (int i = rows.Length; i < pool.Length; i++) questionDeck.Add(pool[i]);
        currentRow = 0;
        remaining = roundSeconds;
        lastWarningSecond = int.MaxValue;
        phase = GlassBridgePhase.Waiting;
        Publish();
    }

    private void Update()
    {
        if (!isServer || phase == GlassBridgePhase.Inactive || phase == GlassBridgePhase.Completed) return;
        if (participants == null || participants.Length != activePlayerCount || participants[0] == null
            || (activePlayerCount == 2 && participants[1] == null))
        {
            phase = GlassBridgePhase.Inactive;
            Publish();
            return;
        }
        if (phase == GlassBridgePhase.Falling)
        {
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                PlayBridgeSound(5);
                levelSelectionManager.RestartLevel(bridgeLevelIndex);
            }
            return;
        }
        Vector3 a = Foot(participants[0]);
        // Reuse the solo player's position so all bridge checks follow the same rules.
        Vector3 b = activePlayerCount == 2 ? Foot(participants[1]) : a;
        if (a.y < fallBoundary.position.y || b.y < fallBoundary.position.y)
        {
            Fail();
            return;
        }
        if (phase == GlassBridgePhase.Waiting)
        {
            if (OnStart(a) && OnStart(b))
            {
                phase = GlassBridgePhase.Running;
                Publish();
                PlayBridgeSound(0);
            }
            return;
        }
        remaining -= Time.deltaTime;
        if (remaining <= 0f) { Fail(true); return; }
        int second = Mathf.CeilToInt(remaining);
        if (second <= warningSeconds && second < lastWarningSecond)
        {
            lastWarningSecond = second;
            PlayBridgeSound(2);
        }
        // All incorrect panes break, even if someone tries to skip a row.
        for (int i = 0; i < rows.Length; i++)
        {
            var wrong = correctOnRight[i] ? rows[i].left : rows[i].right;
            if (wrong.IsUnder(a) || wrong.IsUnder(b))
            {
                broken[2 + i * 2 + (correctOnRight[i] ? 0 : 1)] = true;
                Publish();
                return;
            }
        }
        if (currentRow < rows.Length)
        {
            var safe = correctOnRight[currentRow] ? rows[currentRow].right : rows[currentRow].left;
            if (safe.IsUnder(a) && safe.IsUnder(b)) { currentRow++; Publish(); PlayBridgeSound(1); }
        }
        else if (InFinish(a) && InFinish(b))
        {
            Complete();
            return;
        }
        if (Time.time >= nextPublish) { nextPublish = Time.time + 0.2f; Publish(); }
    }

    private static Vector3 Foot(PlayerController player)
    {
        var cc = player.GetComponent<CharacterController>();
        return cc != null ? player.transform.TransformPoint(cc.center - Vector3.up * cc.height * 0.5f)
            : player.transform.position;
    }

    private bool OnStart(Vector3 foot) => startLeft.IsUnder(foot) || startRight.IsUnder(foot);
    private bool InFinish(Vector3 foot) => (finishZone.ClosestPoint(foot + Vector3.up * 0.1f)
        - (foot + Vector3.up * 0.1f)).sqrMagnitude < 0.001f;

    private void Fail(bool timedOut = false)
    {
        PlayBridgeSound(timedOut ? 4 : 3);
        phase = GlassBridgePhase.Falling;
        remaining = fallSeconds;
        for (int i = 0; i < broken.Length; i++) broken[i] = true;
        Publish();
    }

    private void Complete()
    {
        phase = GlassBridgePhase.Completed;
        // Hub levels can be entered in any order; don't depend on the sequential puzzle index.
        solved = true;
        levelSelectionManager.NotifyPuzzleCompleted(this);
        Publish();
        levelSelectionManager.ReturnPlayersToHub();
    }

    [ObserversRpc(runLocally: true)]
    private void ShowCompletion()
    {
        if (InstanceHandler.TryGetInstance(out GameViewManager views)) views.ShowView<PuzzleSolvedView>();
    }

    private void Publish()
    {
        string[] left = new string[rows == null ? 0 : rows.Length];
        string[] right = new string[left.Length];
        string prompt = "";
        if (questionIndices != null && questionIndices.Length == left.Length)
        {
            for (int i = 0; i < left.Length; i++)
            {
                var q = questions[questionIndices[i]];
                left[i] = correctOnRight[i] ? q.wrongAnswer : q.correctAnswer;
                right[i] = correctOnRight[i] ? q.correctAnswer : q.wrongAnswer;
            }
            prompt = currentRow < rows.Length ? $"{currentRow + 1}/{rows.Length}\n{questions[questionIndices[currentRow]].question}"
                : activePlayerCount == 1 ? "Reach the finish!" : "Both players must reach the finish!";
        }
        ApplySnapshot((int)phase, activePlayerCount, remaining, prompt, left, right, broken ?? Array.Empty<bool>());
    }

    [ObserversRpc(runLocally: true, bufferLast: true)]
    private void ApplySnapshot(int state, int playerCount, float seconds, string prompt, string[] left, string[] right, bool[] shattered)
    {
        var displayedPhase = (GlassBridgePhase)state;
        bool playBreak = hasSnapshot && displayedPhase == GlassBridgePhase.Running;
        hasSnapshot = true;
        if (questionDisplay != null)
            questionDisplay.text = displayedPhase == GlassBridgePhase.Waiting
                ? (playerCount == 1 ? "Jump onto the first glass pair." : "Both players: jump onto the first glass pair.")
                : displayedPhase == GlassBridgePhase.Falling ? "BRIDGE COLLAPSED!\nResetting..."
                : displayedPhase == GlassBridgePhase.Completed ? "BRIDGE COMPLETE!" : prompt;
        if (timerDisplay != null)
            timerDisplay.text = displayedPhase == GlassBridgePhase.Running ? Mathf.CeilToInt(Mathf.Max(0f, seconds)).ToString()
                : displayedPhase == GlassBridgePhase.Waiting ? "READY" : "";
        if (shattered.Length != left.Length * 2 + 2) return;
        startLeft.SetBroken(shattered[0], playBreak);
        startRight.SetBroken(shattered[1], playBreak);
        for (int i = 0; i < left.Length; i++)
        {
            rows[i].left.ShowAnswer(left[i]);
            rows[i].right.ShowAnswer(right[i]);
            rows[i].left.SetBroken(shattered[2 + i * 2], playBreak);
            rows[i].right.SetBroken(shattered[3 + i * 2], playBreak);
        }
    }

}
