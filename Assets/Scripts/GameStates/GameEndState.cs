using System.Collections.Generic;
using System.Linq;
using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;

public class GameEndState : StateNode
{
    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        if (!InstanceHandler.TryGetInstance(out ScoreManager scoreManager))
        {
            Debug.LogError("ScoreManager instance not found!");
            return;
        }

        var winner = scoreManager.GetWinner();
        if (winner == default(PlayerID))
        {
            Debug.Log("No winner could be determined.");
            return;
        }

        if (InstanceHandler.TryGetInstance(out EndGameView endGameView))
        {
            Debug.LogError("EndGameView instance not found!");
            return;
        }

        if(!InstanceHandler.TryGetInstance(out GameViewManager gameViewManager))
        {
            Debug.LogError("GameViewManager instance not found!");
            return;
        }

        endGameView.SetWinner(winner);
        gameViewManager.ShowView<EndGameView>();
        Debug.Log($"Player {winner.id} wins with the most kills!");
    }

    public override void Exit(bool asServer)
    {
        base.Exit(asServer);
    }
}
