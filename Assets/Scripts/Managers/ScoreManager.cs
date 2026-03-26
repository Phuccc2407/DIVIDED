using System;
using PurrNet;
using UnityEngine;

public class ScoreManager : NetworkBehaviour
{
    [SerializeField] private SyncDictionary<PlayerID, ScoreData> _scores = new SyncDictionary<PlayerID, ScoreData>();

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
        _scores.onChanged += OnScoresChanged;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        InstanceHandler.UnregisterInstance<ScoreManager>();
    }

    private void OnScoresChanged(SyncDictionaryChange<PlayerID, ScoreData> change)
    {
        if (InstanceHandler.TryGetInstance(out ScoreboardView scoreboardView))
        {
            scoreboardView.SetData(_scores.ToDictionary());
        }
    }

    public void AddKill(PlayerID playerID)
    {
        CheckForDictionaryEntry(playerID);

        var scoreData = _scores[playerID];
        scoreData.kills++;
        _scores[playerID] = scoreData;
    }

    public void AddDeath(PlayerID playerID)
    {
        CheckForDictionaryEntry(playerID);

        var scoreData = _scores[playerID];
        scoreData.deaths++;
        _scores[playerID] = scoreData;
    }

    public PlayerID GetWinner()
    {
        PlayerID winner = default(PlayerID);
        int highestKills = -1;

        foreach (var score in _scores)
        {
            if (score.Value.kills > highestKills)
            {
                highestKills = score.Value.kills;
                winner = score.Key;
            }
        }

        return winner;
    }

    private void CheckForDictionaryEntry(PlayerID playerID)
    {
        if (!_scores.ContainsKey(playerID))
            _scores.Add(playerID, new ScoreData());
    }

    public struct ScoreData
    {
        public int kills;
        public int deaths;

        public override string ToString()
        {
            return $"{kills}/{deaths}";
        }
    }
}
