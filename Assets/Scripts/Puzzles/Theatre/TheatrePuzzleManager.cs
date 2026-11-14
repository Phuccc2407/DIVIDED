using System;
using System.Collections.Generic;
using PurrNet;
using UnityEngine;

public enum TheatrePhase { Inactive, Waiting, Opening, Running, Closing, Intermission, Completed }

public class TheatrePuzzleManager : PuzzleBase
{
    [Header("Hub")]
    [SerializeField] private LevelSelectionManager levelSelectionManager;
    [SerializeField] private int theatreLevelIndex = 4;
    [SerializeField, Min(1f)] private float maxButtonDistance = 4f;
    [Tooltip("Enable only when using AllPuzzleManager's sequential mode instead of hub selection.")]
    [SerializeField] private bool sequentialMode;
    [Header("Rooms: A then B")]
    [SerializeField] private TheatreRoom[] rooms = new TheatreRoom[2];
    [Header("Independent animation and symbol banks")]
    [SerializeField] private AnimationClip[] animations;
    [Tooltip("Use the 20 articulated prototype gestures instead of imported clips. Actors need TheatreGestureRig.")]
    [SerializeField] private bool useBuiltInGestures;
    [SerializeField] private string[] symbols = {
        "\u2600", "\u263D", "\u2605", "\u2604", "\u263F", "\u2640", "\u2641", "\u2642", "\u2643", "\u2644",
        "\u2645", "\u2646", "\u2647", "\u2660", "\u2663", "\u2665", "\u2666", "\u269C", "\u262F", "\u2625",
        "\u2693", "\u26A1", "\u2694", "\u2696", "\u2699", "\u231B", "\u221E", "\u2295", "\u2297", "\u25C8",
        "\u25B3", "\u25BD", "\u25A1", "\u25CE", "\u16A0", "\u16B1", "\u16A8", "\u16C9", "\u16DF", "\u16D7"
    };
    [Header("Timing")]
    [SerializeField, Min(1f)] private float answerSeconds = 10f;
    [SerializeField, Min(0.1f)] private float curtainSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float liftSeconds = 1f;
    [SerializeField, Min(0.1f)] private float intermissionSeconds = 1f;
    [SerializeField, Min(0f)] private float completionSeconds = 2f;

    private sealed class Round
    {
        public int[] symbols = new int[4];
        public int[] clips = new int[4];
        public int[] buttons;
        public int target;
    }

    private Round[] rounds;
    private TheatrePhase phase;
    private int round, token, result;
    private float phaseStarted, nextPublish;
    // Local presentation state stays separate from authoritative server state.
    private TheatrePhase shownPhase;
    private int shownRound, shownToken = -1, shownResult;
    private float shownElapsed, receivedAt;
    private bool configured;
    private float TransitionSeconds => Mathf.Max(curtainSeconds, liftSeconds);
    private int AnimationCount => useBuiltInGestures ? TheatreGestureRig.GestureCount : animations.Length;
    private int AnswerRoom(int index) => index < 5 ? 0 : 1;

    private void Awake()
    {
        BindButtons();
        Present(0f);
    }

    private void BindButtons()
    {
        for (int r = 0; r < rooms.Length; r++)
        {
            var room = rooms[r];
            if (room == null) continue;
            Bind(room.startButton, r, -1);
            for (int i = 0; i < room.answers.Length; i++) Bind(room.answers[i], r, i);
        }
    }

    private void Bind(TheatreButton button, int room, int index)
    {
        if (button == null) return;
        button.puzzle = this;
        button.roomIndex = room;
        button.answerIndex = index;
    }

    protected override void OnSpawned()
    {
        base.OnSpawned();
        if (isServer) Publish();
    }

    public override void ActivatePuzzle()
    {
        if (sequentialMode && isServer) Enter();
    }

    public void OnLevelEntered(int index)
    {
        if (!isServer || sequentialMode) return;
        if (index == theatreLevelIndex) Enter();
        else SetPhase(TheatrePhase.Inactive);
    }

    private void Enter()
    {
        if (!ValidateSetup()) { SetPhase(TheatrePhase.Inactive); return; }
        BindButtons();
        solved = false;
        round = result = 0;
        rounds = null;
        SetPhase(TheatrePhase.Waiting);
    }

    [ContextMenu("Validate Theatre Setup")]
    private void ValidateInEditor() => ValidateSetup();

    private bool ValidateSetup()
    {
        if (rooms == null || rooms.Length != 2 || rooms[0] == null || rooms[1] == null
            || rooms[0] == rooms[1] || !rooms[0].IsValid || !rooms[1].IsValid
            || (!sequentialMode && levelSelectionManager == null))
        {
            Debug.LogError("Theatre: assign two complete rooms, pose markers, Point Lights and hub manager. See TheatreSetup.md.", this);
            return false;
        }
        var unique = new HashSet<string>();
        if (useBuiltInGestures)
            foreach (var room in rooms)
            {
                bool validRigs = room.mystery.SupportsBuiltInGestures;
                foreach (var actor in room.references) validRigs &= actor.SupportsBuiltInGestures;
                if (!validRigs)
                {
                    Debug.LogError("Theatre: built-in gesture mode needs a complete TheatreGestureRig on all ten actors.", this);
                    return false;
                }
            }
        if (symbols != null)
            foreach (string symbol in symbols)
                if (string.IsNullOrWhiteSpace(symbol) || !unique.Add(symbol))
                { Debug.LogError("Theatre: symbols must be nonempty and unique.", this); return false; }
        var clips = new HashSet<AnimationClip>();
        if (!useBuiltInGestures && animations != null)
            foreach (var clip in animations)
                if (clip == null || clip.legacy || clip.length <= 0f || !clips.Add(clip))
                { Debug.LogError("Theatre: assign distinct, non-legacy animation clips with positive lengths.", this); return false; }
        if (unique.Count < 40 || (!useBuiltInGestures && clips.Count < 4))
        {
            Debug.LogError("Theatre needs at least 40 distinct symbols and 4 distinct animation clips (20 recommended).", this);
            return false;
        }
        Debug.Log("Theatre setup references are valid. Verify TMP glyphs and animation rigs in Play Mode.", this);
        return true;
    }

    private static int[] Deck(int count)
    {
        int[] values = new int[count];
        for (int i = 0; i < count; i++) values[i] = i;
        Shuffle(values);
        return values;
    }

    private static void Shuffle(int[] values)
    {
        for (int i = values.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (values[i], values[j]) = (values[j], values[i]);
        }
    }

    private void Generate()
    {
        int[] symbolDeck = Deck(symbols.Length), targets = Deck(AnimationCount);
        rounds = new Round[10];
        for (int n = 0; n < 10; n++)
        {
            var data = new Round { buttons = Deck(4) };
            int target = targets[n % targets.Length];
            var distractors = new List<int>();
            foreach (int clip in Deck(AnimationCount)) if (clip != target) distractors.Add(clip);
            // Prefer distractors not seen in the preceding round.
            if (n > 0) distractors.Sort((a, b) =>
                (Array.IndexOf(rounds[n - 1].clips, a) >= 0 ? 1 : 0).CompareTo(
                 Array.IndexOf(rounds[n - 1].clips, b) >= 0 ? 1 : 0));
            data.clips = new[] { target, distractors[0], distractors[1], distractors[2] };
            Shuffle(data.clips);
            data.target = Array.IndexOf(data.clips, target);
            for (int i = 0; i < 4; i++) data.symbols[i] = symbolDeck[n * 4 + i];
            rounds[n] = data;
        }
    }

    [ServerRpc(requireOwnership: false)]
    public void Press(int roomIndex, int answerIndex, int roundToken, RPCInfo info = default)
    {
        if (!isServer || roundToken != token) return;
        if (roomIndex < 0 || roomIndex >= rooms.Length || rooms[roomIndex] == null) return;
        var requestedRoom = rooms[roomIndex];
        TheatreButton button = answerIndex == -1 ? requestedRoom.startButton
            : answerIndex >= 0 && answerIndex < 4 ? requestedRoom.answers[answerIndex] : null;
        if (button == null) return;
        bool nearButton = false;
        Physics.SyncTransforms();
        foreach (var player in FindObjectsByType<PlayerController>())
        {
            if (player.owner != info.sender) continue;
            var interactor = player.GetComponent<PlayerInteractor>();
            Vector3 origin = interactor != null ? interactor.InteractionOrigin : player.transform.position;
            if (button.IsWithinReach(origin, maxButtonDistance)) { nearButton = true; break; }
        }
        if (!nearButton) return;
        if (phase == TheatrePhase.Waiting && roomIndex == 0 && answerIndex == -1)
        {
            Generate(); round = result = 0;
            SetPhase(TheatrePhase.Opening);
            return;
        }
        if (phase != TheatrePhase.Running || roomIndex != AnswerRoom(round)
            || answerIndex < 0 || answerIndex > 3) return;
        if (Time.time - phaseStarted >= answerSeconds) result = 3;
        else result = rounds[round].buttons[answerIndex] == rounds[round].target ? 1 : 2;
        SetPhase(TheatrePhase.Closing);
    }

    private void Update()
    {
        if (isServer)
        {
            float elapsed = Time.time - phaseStarted;
            if (phase == TheatrePhase.Opening && elapsed >= TransitionSeconds) SetPhase(TheatrePhase.Running);
            else if (phase == TheatrePhase.Running && elapsed >= answerSeconds)
            { result = 3; SetPhase(TheatrePhase.Closing); }
            else if (phase == TheatrePhase.Closing && elapsed >= TransitionSeconds)
            {
                if (result != 1) { round = 0; rounds = null; SetPhase(TheatrePhase.Waiting); }
                else if (round == 9) SetPhase(TheatrePhase.Completed);
                else SetPhase(TheatrePhase.Intermission);
            }
            else if (phase == TheatrePhase.Intermission && elapsed >= intermissionSeconds)
            { round++; result = 0; SetPhase(TheatrePhase.Opening); }
            else if (phase == TheatrePhase.Completed && !solved && elapsed >= completionSeconds) Complete();
            if (Time.time >= nextPublish) { nextPublish = Time.time + 0.2f; Publish(); }
        }
        Present(shownElapsed + Time.time - receivedAt);
    }

    private void OnDisable()
    {
        BackgroundMusic.SetPaused(this, false);
        shownPhase = TheatrePhase.Inactive;
        Present(0f);
        foreach (var room in rooms)
        {
            if (room == null) continue;
            room.StopMusic();
            if (room.mystery != null) room.mystery.StopAnimation();
            foreach (var actor in room.references) if (actor != null) actor.StopAnimation();
        }
    }

    private void SetPhase(TheatrePhase next)
    {
        phase = next;
        phaseStarted = Time.time;
        token++;
        Publish();
    }

    private void Complete()
    {
        if (sequentialMode) SolvePuzzle();
        else
        {
            solved = true;
            levelSelectionManager.NotifyPuzzleCompleted(this);
            levelSelectionManager.ReturnPlayersToHub();
        }
    }

    [ObserversRpc(runLocally: true)]
    private void ShowCompletion()
    {
        if (InstanceHandler.TryGetInstance(out GameViewManager views)) views.ShowView<PuzzleSolvedView>();
    }

    private void Publish()
    {
        Round data = rounds == null ? null : rounds[round];
        ApplySnapshot((int)phase, round, token, result, Time.time - phaseStarted,
            data?.symbols ?? Array.Empty<int>(), data?.clips ?? Array.Empty<int>(),
            data?.buttons ?? Array.Empty<int>(), data?.target ?? 0);
    }

    [ObserversRpc(runLocally: true, bufferLast: true)]
    private void ApplySnapshot(int state, int index, int version, int outcome, float elapsed,
        int[] symbolIds, int[] clipIds, int[] buttonOrder, int target)
    {
        shownPhase = (TheatrePhase)state;
        shownRound = index;
        shownResult = outcome;
        shownElapsed = elapsed;
        receivedAt = Time.time;
        if (shownToken == version) return;
        shownToken = version;
        bool pauseBackground = shownPhase == TheatrePhase.Opening || shownPhase == TheatrePhase.Running
            || shownPhase == TheatrePhase.Intermission || shownPhase == TheatrePhase.Completed
            || (shownPhase == TheatrePhase.Closing && outcome == 1);
        BackgroundMusic.SetPaused(this, pauseBackground);
        configured = symbolIds.Length == 4 && clipIds.Length == 4 && buttonOrder.Length == 4;
        if (configured)
        {
            configured = target >= 0 && target < 4 && symbols != null && (useBuiltInGestures || animations != null);
            for (int i = 0; configured && i < 4; i++)
                configured = symbolIds[i] >= 0 && symbolIds[i] < symbols.Length
                    && clipIds[i] >= 0 && clipIds[i] < AnimationCount && (useBuiltInGestures || animations[clipIds[i]] != null)
                    && buttonOrder[i] >= 0 && buttonOrder[i] < 4;
            if (!configured) Debug.LogError("Theatre: snapshot does not match local symbol/animation banks. Use identical builds on both peers.", this);
        }
        for (int r = 0; r < rooms.Length; r++)
        {
            var room = rooms[r];
            if (room == null) continue;
            room.StopMusic();
            if (room.startButton != null) room.startButton.Configure(shownPhase == TheatrePhase.Waiting && r == 0, "START", version);
            for (int i = 0; i < room.answers.Length; i++)
                if (room.answers[i] != null) room.answers[i].Configure(configured && shownPhase == TheatrePhase.Running && r == AnswerRoom(index),
                    configured ? symbols[symbolIds[buttonOrder[i]]] : "", version);
            if (shownPhase == TheatrePhase.Opening && configured)
            {
                ConfigureActor(room.mystery, "?", clipIds[target]);
                for (int i = 0; i < 4; i++) ConfigureActor(room.references[i], symbols[symbolIds[i]], clipIds[i]);
                room.Sound(room.openSound); room.Sound(room.liftSound);
                if (index == 0 && r == 0) room.Sound(room.pressSound);
            }
            else if (shownPhase == TheatrePhase.Running && configured)
            {
                // Also configure a peer that joins during a running round.
                ConfigureActor(room.mystery, "?", clipIds[target]);
                for (int i = 0; i < 4; i++) ConfigureActor(room.references[i], symbols[symbolIds[i]], clipIds[i]);
                room.StartMusic(elapsed);
            }
            else if (shownPhase == TheatrePhase.Closing)
            {
                room.Sound(room.closeSound); room.Sound(room.liftSound);
                if (outcome != 3) room.Sound(room.pressSound);
                room.Sound(outcome == 1 ? room.correctSound : outcome == 3 ? room.timeoutSound : room.wrongSound);
            }
            else if (shownPhase == TheatrePhase.Intermission && index == 4) room.Sound(room.swapSound);
            else if (shownPhase == TheatrePhase.Completed) room.Sound(room.completedSound);
            if (shownPhase == TheatrePhase.Inactive || shownPhase == TheatrePhase.Waiting
                || shownPhase == TheatrePhase.Completed)
            {
                if (room.mystery != null) room.mystery.StopAnimation();
                foreach (var actor in room.references) if (actor != null) actor.StopAnimation();
            }
        }
        Present(elapsed);
    }

    private void ConfigureActor(TheatreActor actor, string symbol, int index)
    {
        if (useBuiltInGestures) actor.ConfigureGesture(symbol, index);
        else actor.Configure(symbol, animations[index]);
    }

    private void Present(float elapsed)
    {
        bool opening = shownPhase == TheatrePhase.Opening;
        bool running = shownPhase == TheatrePhase.Running;
        bool closing = shownPhase == TheatrePhase.Closing;
        float curtain = running ? 1f : opening ? Mathf.Clamp01(elapsed / curtainSeconds)
            : closing ? 1f - Mathf.Clamp01(elapsed / curtainSeconds) : 0f;
        float lift = running ? 1f : opening ? Mathf.Clamp01(elapsed / liftSeconds)
            : closing ? 1f - Mathf.Clamp01(elapsed / liftSeconds) : 0f;
        for (int r = 0; r < rooms.Length; r++)
        {
            var room = rooms[r];
            if (room == null) continue;
            room.leftCurtain.Apply(curtain); room.rightCurtain.Apply(curtain);
            if (room.pointLight != null) room.pointLight.enabled = opening || running || (closing && elapsed < curtainSeconds);
            if (room.startButton != null) room.startButton.lift.Apply(
                shownPhase == TheatrePhase.Waiting && r == 0 ? Mathf.Clamp01(elapsed / liftSeconds)
                : opening && shownRound == 0 && r == 0 ? 1f - Mathf.Clamp01(elapsed / liftSeconds) : 0f);
            bool answerRoom = r == AnswerRoom(shownRound);
            for (int i = 0; i < room.answers.Length; i++)
                if (room.answers[i] != null) room.answers[i].lift.Apply(answerRoom ? lift : 0f);
            if (room.mystery != null)
            {
                room.mystery.lift.Apply(answerRoom ? lift : 0f);
                if (running) room.mystery.Sample(elapsed);
            }
            foreach (var actor in room.references)
                if (actor != null) { actor.lift.Apply(answerRoom ? 0f : lift); if (running) actor.Sample(elapsed); }
            if (room.statusDisplay != null)
                room.statusDisplay.text = running ? $"{Mathf.CeilToInt(Mathf.Max(0f, answerSeconds - elapsed))}s\nChoose the right symbol."
                    : shownPhase == TheatrePhase.Waiting ? (r == 0 ? "PRESS START" : "WAIT FOR ROOM A")
                    : shownPhase == TheatrePhase.Completed ? "PERFORMANCE COMPLETE"
                    : closing && shownResult != 1 ? (shownResult == 3 ? "TIME'S UP - RESETTING" : "WRONG - RESETTING")
                    : shownPhase == TheatrePhase.Intermission && shownRound == 4 ? "SWAP ROLES" : "";
        }
    }
}
