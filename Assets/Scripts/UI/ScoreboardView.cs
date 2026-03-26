using System.Collections.Generic;
using PurrNet;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class ScoreboardView : View
{
    [SerializeField] private Transform scoreboardEntriesParent;
    [SerializeField] private ScoreboardEntry scoreboardEntryPrefab;

    private GameViewManager _gameViewManager;

    void Awake()
    {
        InstanceHandler.RegisterInstance(this);
    }

    private void Start() {
        _gameViewManager = InstanceHandler.GetInstance<GameViewManager>();
    }

    void OnDestroy()
    {
        InstanceHandler.UnregisterInstance<ScoreboardView>();
    }

    public void SetData(Dictionary<PlayerID, ScoreManager.ScoreData> data)
    {
        foreach (Transform child in scoreboardEntriesParent.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (var playerScore in data)
        {
            var entry = Instantiate(scoreboardEntryPrefab, scoreboardEntriesParent);
            entry.SetData(playerScore.Key.id.ToString(), playerScore.Value.kills, playerScore.Value.deaths);
        }
    }

    private void Update()
    {
        if(Keyboard.current.tabKey.wasPressedThisFrame)
        {
            _gameViewManager.ShowView<ScoreboardView>(false);
        }
        else if(Keyboard.current.tabKey.wasReleasedThisFrame)
        {
            _gameViewManager.HideView<ScoreboardView>();
        }
    }

    public override void OnShow()
    {
        
    }

    public override void OnHide()
    {
        
    }
}
