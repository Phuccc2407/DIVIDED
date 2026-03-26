using System.Collections;
using System.Collections.Generic;
using PurrNet;
using PurrNet.StateMachine;
using UnityEngine;

public class RoundEndState : StateNode  
{
    [SerializeField] private int amountOFRounds = 3;
    [SerializeField] private StateNode spawningState;

    private int _roundCounter = 0;
    [SerializeField] private WaitForSeconds _delay = new WaitForSeconds(3f);

    public override void Enter(bool asServer)
    {
        base.Enter(asServer);

        if (!asServer)
            return;

        CheckForGameEnd();
    }

    private void CheckForGameEnd()
    {
        _roundCounter++;

        if (_roundCounter >= amountOFRounds)
        {
            machine.Next();
            return;
        }

        StartCoroutine(DelayNextState());
    }

    private IEnumerator DelayNextState()
    {
        yield return _delay;
        machine.SetState(spawningState);
    }

    public override void Exit()
    {
        
    }
}
